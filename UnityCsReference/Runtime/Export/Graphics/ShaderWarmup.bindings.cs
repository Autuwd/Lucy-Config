// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ====================================================================
// 🎯 着色器预热 — 预编译Shader变体以减少运行时卡顿
// 💡 WarmupShader直接预热指定Shader的变体
// 💡 WarmupShaderFromCollection从ShaderVariantCollection中预热
// 💡 ShaderWarmupSetup包含顶点声明(VertexAttributeDescriptor[])配置
// ⚡ 在加载阶段调用，将编译开销提前到Loading Screen期间
// ====================================================================

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;


namespace UnityEngine.Experimental.Rendering
{
    public struct ShaderWarmupSetup
    {
        public VertexAttributeDescriptor[] vdecl;
    }

    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    public static class ShaderWarmup
    {
        [FreeFunction(Name = "ShaderWarmupScripting::WarmupShader")]
        static public extern void WarmupShader(Shader shader, ShaderWarmupSetup setup);
        [FreeFunction(Name = "ShaderWarmupScripting::WarmupShaderFromCollection")]
        static public extern void WarmupShaderFromCollection(ShaderVariantCollection collection, Shader shader, ShaderWarmupSetup setup);
    }
}
