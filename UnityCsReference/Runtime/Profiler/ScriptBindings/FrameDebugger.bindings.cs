// ====================================================================================================
// 🎯 FrameDebugger —— GPU 渲染帧调试器
//
// 【功能定位】
//   提供 Editor 下逐帧检查 GPU 绘制调用的能力，查看每个 DrawCall 的
//   渲染状态（Shader、贴图、混合模式、顶点数等），定位过度绘制和渲染性能瓶颈。
//
// 【GPU 调试核心价值】
//   📌 显示每帧所有 DrawCall 列表及其耗时
//   📌 查看每个 DrawCall 的完整 GPU 管线状态（VB/IB/Shaders/Textures/BlendState 等）
//   📌 支持 Overdraw 可视化着色模式，直观定位像素过度绘制区域
//   📌 对比不同 GPU 模块（Rasterizer/Output Merger/Shader）的配置差异
//
// 【设计要点】
//   💡 静态类，[StaticAccessor("FrameDebugger", DoubleColon)] 直接对接 C++ side。
//   ⚠️ enabled 属性 = IsLocalEnabled() || IsRemoteEnabled()，本地和远程调试都算启用。
//   ⚠️ 此文件仅为托管暴露入口，实际 DrawCall 列表查看在 FrameDebugger 窗口（Editor Window）中。
//   ⚡ Frame Debugger 开启会增加 GPU 开销，不应在发布版本中启用。
// ====================================================================================================
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Profiler/PerformanceTools/FrameDebugger.h")]
    [StaticAccessor("FrameDebugger", StaticAccessorType.DoubleColon)]
    public static class FrameDebugger
    {
        public static bool enabled
        {
            get => IsLocalEnabled() || IsRemoteEnabled();
        }

        internal static extern bool IsLocalEnabled();
        internal static extern bool IsRemoteEnabled();
    }
}
