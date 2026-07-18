// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 ScreenCapture — 屏幕截图功能
//
// 📌 作用：
//   截取当前游戏画面并保存为 PNG 文件或返回 Texture2D
//
// 💡 关键方法：
//   - CaptureScreenshot(filename): 保存截图到文件
//   - CaptureScreenshotAsTexture(): 返回截图 Texture2D（不写磁盘）
//   - CaptureScreenshotIntoRenderTexture(): 截图到 RenderTexture
//
// 🎯 superSize 参数：
//   1 = 当前分辨率，2 = 2 倍分辨率（抗锯齿效果）
//
// 💡 StereoScreenCaptureMode: 支持 VR 立体截图（左眼/右眼/双眼/运动向量）
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/ScreenCapture/Public/CaptureScreenshot.h")]
    public static class ScreenCapture
    {
        public static void CaptureScreenshot(string filename)
        {
            CaptureScreenshot(filename, 1, StereoScreenCaptureMode.LeftEye);
        }

        public static void CaptureScreenshot(string filename, int superSize)
        {
            CaptureScreenshot(filename, superSize, StereoScreenCaptureMode.LeftEye);
        }

        public static void CaptureScreenshot(string filename, StereoScreenCaptureMode stereoCaptureMode)
        {
            CaptureScreenshot(filename, 1, stereoCaptureMode);
        }

        public static Texture2D CaptureScreenshotAsTexture()
        {
            return CaptureScreenshotAsTexture(1, StereoScreenCaptureMode.LeftEye);
        }

        public static Texture2D CaptureScreenshotAsTexture(int superSize)
        {
            return CaptureScreenshotAsTexture(superSize, StereoScreenCaptureMode.LeftEye);
        }

        public static Texture2D CaptureScreenshotAsTexture(StereoScreenCaptureMode stereoCaptureMode)
        {
            return CaptureScreenshotAsTexture(1, stereoCaptureMode);
        }

        public static extern void CaptureScreenshotIntoRenderTexture(RenderTexture renderTexture);

        private static extern void CaptureScreenshot(string filename, [UnityEngine.Internal.DefaultValue("1")] int superSize, [UnityEngine.Internal.DefaultValue("1")]  StereoScreenCaptureMode CaptureMode);
        private static extern Texture2D CaptureScreenshotAsTexture(int superSize, StereoScreenCaptureMode stereoScreenCaptureMode);


        // Offsets must match UnityVRBlitMode in IUnityVR.h
        public enum StereoScreenCaptureMode
        {
            LeftEye = 1,
            RightEye = 2,
            BothEyes = 3,
            MotionVectors = 4,
        }
    }
}
