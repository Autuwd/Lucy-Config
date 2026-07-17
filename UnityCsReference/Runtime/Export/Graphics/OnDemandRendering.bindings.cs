// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 OnDemandRendering — 按需渲染控制
//
// 📌 作用：
//   控制 Unity 的渲染帧率独立于逻辑帧率。
//   通过设置 renderFrameInterval，可以让 Unity 每 N 帧渲染一次，
//   而逻辑更新（Update）仍然每帧执行。
//
// 🎯 关键属性：
//   - renderFrameInterval：渲染间隔（默认=1，每帧渲染）
//     设为 2 = 每 2 帧渲染一次（有效帧率减半）
//     设为 3 = 每 3 帧渲染一次（以此类推）
//   - willCurrentFrameRender：当前帧是否会被渲染
//   - effectiveRenderFrameRate：实际渲染帧率（考虑间隔后的有效值）
//
// 💡 典型应用场景：
//   - UI 界面（画面变化少时降低渲染频率节省电量）
//   - 后台应用（应用不在前台但仍需保持逻辑运行）
//   - 策略/回合制游戏（画面不需要每帧更新）
//
// ⚡ 性能优化：
//   - 对于 UI 为主的界面，设为 2~4 可显著降低 GPU 功耗
//   - 配合 Application.targetFrameRate 使用效果更佳
//   - 移动平台省电利器
//
// 📌 注意：此 API 不影响 Time.frameCount 的递增
// ==============================================================

using UnityEngine.Scripting;
using UnityEngine.Bindings;
using System;

namespace UnityEngine.Rendering
{
    [RequiredByNativeCode]
    public class OnDemandRendering
    {
        // Default to 1. Render every frame.
        private static int m_RenderFrameInterval = 1;

        public static bool willCurrentFrameRender
        {
            get
            {
                return Time.frameCount % renderFrameInterval == 0;
            }
        }

        public static int renderFrameInterval
        {
            get { return m_RenderFrameInterval; }

            set { m_RenderFrameInterval = Math.Max(1, value); }
        }

        [RequiredByNativeCode]
        internal static void GetRenderFrameInterval(out int frameInterval) { frameInterval = renderFrameInterval; }

        [FreeFunction]
        internal static extern float GetEffectiveRenderFrameRate();

        public static int effectiveRenderFrameRate
        {
            get
            {
                float frameRate = GetEffectiveRenderFrameRate();
                if (frameRate <= 0.0)
                    return (int)frameRate;
                return (int)(frameRate + 0.5f);
            }
        }
    }
}
