// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 Cursor — 鼠标光标控制 API
//
// 📌 作用：
//   控制鼠标光标的显示、锁定状态和自定义图标
//
// 💡 lockState 三种模式：
//   - CursorLockMode.None:     正常模式，光标自由移动
//   - CursorLockMode.Locked:   锁定到窗口中心（FPS 游戏常用）
//   - CursorLockMode.Confined: 限制在窗口区域内
//
// 🎯 关键属性/方法：
//   - visible: 光标显隐
//   - lockState: 锁定模式
//   - SetCursor(): 设置自定义光标纹理 + 热点偏移 + 模式
//
// 💡 CursorMode.Auto = 硬件光标（性能好），ForceSoftware = 软件绘制
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    // How should the custom cursor be rendered
    public enum CursorMode
    {
        // Use hardware cursors on supported platforms.
        Auto = 0,

        // Force the use of software cursors.
        ForceSoftware = 1,
    }

    // How should the cursor behave?
    public enum CursorLockMode
    {
        // Normal
        None = 0,

        // Locked to the center of the game window
        Locked = 1,

        // Confined to the game window
        Confined = 2
    }

    // Cursor API for setting the cursor that is used for rendering.
    [NativeHeader("Runtime/Export/Input/Cursor.bindings.h")]
    public class Cursor
    {
        private static void SetCursor(Texture2D texture, CursorMode cursorMode)
        {
            SetCursor(texture, Vector2.zero, cursorMode);
        }

        public static extern void SetCursor(Texture2D texture, Vector2 hotspot, CursorMode cursorMode);

        // Should the cursor be visible?
        public static extern bool visible { get; set; }

        // Is the cursor normal/locked/confined?
        public static extern CursorLockMode lockState { get; set; }
    }
}
