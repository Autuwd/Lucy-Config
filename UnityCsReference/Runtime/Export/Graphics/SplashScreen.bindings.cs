// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 SplashScreen — Unity 启动画面（Splash Screen）管理
//
// 📌 作用：
//   管理 Unity 引擎启动时显示的 Logo/Splash 画面。
//   这是 Unity Personal 版本强制显示的画面，Pro 版本可自定义或禁用。
//
// 🎯 关键 API：
//   - isFinished：Splash 画面是否播放完毕
//   - Begin()：开始显示 Splash 画面
//   - Stop()：停止 Splash 画面（支持立即停止或淡出）
//   - Draw()：渲染 Splash 画面（由引擎在启动时自动调用）
//
// 📌 StopBehavior：
//   - StopImmediate：立即取消 Splash 画面
//   - FadeOut：渐隐退出（使用 BeginSplashScreenFade）
//
// 💡 应用场景：
//   - 编辑器中自定义 Splash 画面的显示逻辑
//   - 加载场景时控制 Splash 画面的生命周期
//   - 配合 LoadSceneAsync 实现 Splash -> 游戏的平滑过渡
//
// 📍 对应 C++ 头文件：Runtime/Graphics/DrawSplashScreenAndWatermarks.h
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/DrawSplashScreenAndWatermarks.h")]
    public class SplashScreen
    {
        public enum StopBehavior
        {
            StopImmediate = 0,
            FadeOut = 1
        }

        extern public static bool isFinished {[FreeFunction("IsSplashScreenFinished")] get; }

        [FreeFunction]
        extern static void CancelSplashScreen();

        [FreeFunction]
        extern static void BeginSplashScreenFade();

        [FreeFunction("BeginSplashScreen_Binding")]
        extern public static void Begin();

        public static void Stop(StopBehavior stopBehavior)
        {
            if (stopBehavior == StopBehavior.FadeOut)
                BeginSplashScreenFade();
            else
                CancelSplashScreen();
        }

        [FreeFunction("DrawSplashScreen_Binding")]
        extern public static void Draw();

        [FreeFunction("SetSplashScreenTime")]
        extern internal static void SetTime(float time);
    }
} // namespace UnityEngine.Rendering
