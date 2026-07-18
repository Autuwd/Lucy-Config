// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    //=============================================================================
    // ⚠️ FlareLayer —— 镜头光晕组件（已废弃）
    //
    // 设计说明:
    //   内置渲染管线的镜头光晕组件，必须挂在 Camera 上。
    //
    // 📌 废弃原因:
    //   随着 Built-In Render Pipeline 被标记为 deprecated，
    //   此组件不再有效。替代方案请参考文档或使用 URP/HDRP 的光晕系统。
    //
    // 🎯 当前行为:
    //   组件仍存在于代码中以保持向后兼容，但不再产生任何视觉效果。
    //   所有新项目不应再使用此组件。
    //=============================================================================

    [RequireComponent(typeof(Camera))]
    [Obsolete("The Flare Layer component is deprecated now that the Built-In Render Pipeline is deprecated. To use an alternative, refer to the documentation in the component help icon. #from(6000.5)", false)]
    [HelpURL("create-lens-flare")]
    public class FlareLayer : Behaviour
    {
        internal FlareLayer() {}
    }
}
