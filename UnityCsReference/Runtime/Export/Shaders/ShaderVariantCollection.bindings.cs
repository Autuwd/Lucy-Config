// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 ShaderVariantCollection — Shader 变体预编译管理
//
// 📌 作用：
//   声明项目中使用到的所有 Shader 变体组合，
//   在构建或运行时预编译这些变体，避免卡顿。
//
// 💡 为什么需要 SVC：
//   #pragma multi_compile 会产生大量变体，
//   但实际运行时只用其中一小部分。
//   SVC 告诉 Unity"只需要这些变体"，其余在构建时剥离。
//
// 📌 关键操作：
//   - AddVariant(shader, passType, keywords) — 注册需要的变体
//   - WarmUp() — 运行时预编译所有已注册变体
//   - WarmUpProgressively(n) — 渐进式预编译（分散 CPU 开销）
//   - isWarmedUp — 检查是否已完成预编译
//
// ⚠️ 最佳实践：
//   URP/HDRP 自己管理 SVC，通常不需要手动操作。
//   自定义渲染管线中，建议在 Loading 界面调用 WarmUp。
// ==============================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    public sealed partial class ShaderVariantCollection : Object
    {
        public partial struct ShaderVariant
        {
            [FreeFunction][NativeConditional("UNITY_EDITOR")]
            extern private static string CheckShaderVariant(Shader shader, UnityEngine.Rendering.PassType passType, string[] keywords);
        }
    }

    public sealed partial class ShaderVariantCollection : Object
    {
        extern public int  shaderCount  { get; }
        extern public int  variantCount { get; }
        extern public int  warmedUpVariantCount { get; }
        extern public bool isWarmedUp   {[NativeName("IsWarmedUp")] get; }

        extern private bool AddVariant(Shader shader, UnityEngine.Rendering.PassType passType, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] string[] keywords);
        extern private bool RemoveVariant(Shader shader, UnityEngine.Rendering.PassType passType,[UnityMarshalAs(NativeType.ScriptingObjectPtr)] string[] keywords);
        extern private bool ContainsVariant(Shader shader, UnityEngine.Rendering.PassType passType, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] string[] keywords);

        [NativeName("ClearVariants")] extern public void Clear();
        [NativeName("WarmupShaders")] extern public void WarmUp();
        [NativeName("WarmupShadersProgressively")] extern public bool WarmUpProgressively(int variantCount);

        [NativeName("CreateFromScript")] extern private static void Internal_Create([Writable] ShaderVariantCollection svc);
    }
}
