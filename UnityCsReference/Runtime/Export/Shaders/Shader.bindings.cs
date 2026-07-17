// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 Shader — Unity 渲染管线的核心：GPU 着色器程序的 C# 句柄
// ================================================================
//
// 📌 本文件概述：
//   Shader.bindings.cs 是 Unity 着色器系统在 C# 层的绑定入口。
//   它桥接了 C# 脚本层和 C++ 引擎层的着色器编译/管理/执行系统。
//
// 【Shader 对象的本质】
//   Shader 是 GPU 上运行的顶点/片元着色器程序在 CPU 端的"句柄"（Handle）。
//   它本身不是纹理、不是材质，而是一套"渲染指令模板"。
//   GPU 根据 Shader 中定义的指令，将 3D 模型数据转换为屏幕上的像素。
//
// 【Shader vs Material 的关系（核心概念）】
//   Shader = 渲染蓝图（定义"怎么画"）
//   Material = 渲染实例（定义"用什么颜色/纹理来画"）
//   一个 Shader 可以被多个 Material 共享，每个 Material 持有不同的属性值。
//   类比：Shader 是菜谱，Material 是按照菜谱做出的具体菜品。
//
// 【Shader 在渲染管线中的位置】
//   Mesh 数据 → GPU → Vertex Shader（顶点变换）→ Rasterization（光栅化）
//   → Fragment Shader（像素着色）→ 帧缓冲区 → 屏幕输出
//
// 【Unity 渲染管线兼容】
//   - Built-in Render Pipeline：传统管线，Shader 使用 ShaderLab + Cg/HLSL
//   - URP (Universal Render Pipeline)：通用管线，使用 Shader Graph 或自定义 HLSL
//   - HDRP (High Definition Render Pipeline)：高清管线，面向 AAA 级画质
//   - globalRenderPipeline 属性用于查询/设置当前活跃的渲染管线
//
// 【Shader 的内部结构】
//   一个 Shader 文件包含：
//   - SubShader：一个或多个 SubShader，按优先级从高到低选择（兼容不同 GPU）
//   - Pass：每个 SubShader 包含多个 Pass，每个 Pass 是一次完整的渲染通道
//   - Tag：SubShader 和 Pass 级别的键值对标签，用于渲染管线决策
//   - Properties：声明式属性列表（纹理、颜色、浮点数等），供 Material 设置
//
// 【ShaderKeyword 系统】
//   关键字（Keyword）是 Shader 的条件编译开关，运行时动态启用/禁用。
//   例如：#pragma shader_feature _NORMALMAP 控制法线贴图是否生效。
//   Unity 2021.2+ 引入了 GlobalKeyword 和 LocalKeyword 类型化关键字 API。
//   全局关键字影响所有 Material，局部关键字仅影响单个 Material。
//
// 【性能提示】
//   - Shader.Find() 通过名称查找，开销较大，应缓存结果
//   - PropertyToID() 将字符串转为整数 ID，避免运行时重复解析
//   - WarmupAllShaders() 预热所有 Shader，避免首次使用时的编译卡顿
//   - ShaderVariantCollection 用于精确控制需要预热的变体子集
//
// 📍 对应 C++ 头文件：
//   Runtime/Shaders/Shader.h
//   Runtime/Shaders/ComputeShader.h
//   Runtime/Shaders/ShaderNameRegistry.h
//   Runtime/Shaders/GpuPrograms/ShaderVariantCollection.h
//   Runtime/Shaders/Keywords/KeywordSpaceScriptBindings.h
// ================================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("UnityEditor.ShaderUtil.Tests")]
[assembly: InternalsVisibleTo("Unity.Shader.Tests")]

namespace UnityEngine
{
    // ================================================================
    // DisableBatchingType — 禁用合批类型枚举
    // ================================================================
    // 控制 Shader 是否参与 GPU 合批渲染（Batching）。
    //
    // 🎯 合批的意义：
    //   GPU 合批将多个小物件合并为一次 Draw Call，减少 CPU→GPU 的调用次数。
    //   但某些 Shader（如带动画骨骼的）不能被合批。
    //
    // ⚡ 值说明：
    //   False          — 不禁用合批（默认，允许合批）
    //   True           — 禁用合批（强制每个物件单独 Draw Call）
    //   WhenLODFading  — 仅在 LOD 淡入淡出时禁用合批
    // ================================================================
    internal enum DisableBatchingType
    {
        False,
        True,
        WhenLODFading
    }

    // ================================================================
    // Shader - GPU 着色器程序的 C# 端句柄
    // ================================================================
    //
    // 🎯 Shader 是 Unity 渲染管线的核心对象，它封装了 GPU 上运行的
    //   顶点着色器（Vertex Shader）和片元着色器（Fragment Shader）程序。
    //
    // 【继承链】
    //   Shader -> Object（UnityEngine.Object，有 hideFlags、name 等基础属性）
    //   sealed class — 不可继承
    //
    // 【Shader 与 GPU 程序的关系】
    //   - 一个 .shader 文件经过编译后，生成适配不同 GPU（DX9/DX11/DX12/Metal/Vulkan）
    //     的着色器字节码（GpuProgram）
    //   - Shader 对象是这些字节码的管理句柄，引擎根据 GPU 能力自动选择最佳变体
    //   - ShaderVariantCollection 管理需要预编译的变体子集
    //
    // 【Shader 文件结构（ShaderLab 语法）】
    //   Shader "MyShader" {
    //       Properties { ... }          ← 属性声明（Material 可设置）
    //       SubShader {                 ← 子着色器（按优先级选择）
    //           Tags { ... }            ← 渲染队列、渲染类型等标签
    //           Pass {                  ← 渲染通道
    //               Tags { ... }        ← Pass 级标签
    //               HLSLPROGRAM ... ENDHLSL  ← GPU 代码
    //           }
    //       }
    //       Fallback "OtherShader"     ← 所有 SubShader 都不支持时的后备
    //   }
    //
    // 【全局属性 vs 材质属性】
    //   - 全局属性（Shader.SetGlobal*）：影响所有使用该属性的 Shader
    //   - 材质属性（Material.SetFloat 等）：仅影响单个 Material
    //   - 全局属性优先级低于材质属性
    //
    // 【ShaderKeyword 系统详解】
    //   关键字是 Shader 内部的 #if/#ifdef 编译开关：
    //   - GlobalKeyword：全局关键字，EnableKeyword/DisableKeyword 控制
    //   - LocalKeyword / LocalKeywordSpace：局部关键字，仅影响单个 Material
    //   - keywordSpace：此 Shader 声明的所有局部关键字空间
    //   - enabledGlobalKeywords：当前全局启用的所有关键字
    //
    // 【PropertyID 系统】
    //   Shader.PropertyToID("_Color") → int ID
    //   使用 ID 比字符串快得多，因为避免了每帧的字符串哈希查找
    //   这是 Unity 渲染性能优化的基础模式
    //
    // ⚠️ 注意事项：
    //   - Shader 是从 Asset 加载的，不应 new 创建
    //   - Shader.Find() 在打包后只包含被引用的 Shader
    //   - Destroy() 一个正在使用的 Shader 会导致粉色材质（fallback）
    // ================================================================
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/Shader.h")]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    [NativeHeader("Runtime/Shaders/ShaderNameRegistry.h")]
    [NativeHeader("Runtime/Shaders/GpuPrograms/ShaderVariantCollection.h")]
    [NativeHeader("Runtime/Shaders/Keywords/KeywordSpaceScriptBindings.h")]
    [NativeHeader("Runtime/Misc/ResourceManager.h")]
    public sealed partial class Shader : Object
    {
        // ================================================================
        // Shader 查找与创建
        // ================================================================
        // 🎯 从已加载资源中按名称查找 Shader。
        //   Find() 在运行时通过 ShaderNameRegistry 查询已编译的 Shader 列表。
        //   FindBuiltin() 专门查找内置 Shader（如 Standard、Unlit/Color 等）。
        //   CreateFromCompiledData() 从预编译的字节码数据重建 Shader 对象。
        //
        // ⚠️ 性能提示：Shader.Find() 有哈希查找开销，频繁调用应缓存结果。
        // ================================================================
        public static Shader Find(string name) => ResourcesAPI.ActiveAPI.FindShaderByName(name);
        [FreeFunction("GetBuiltinResource<Shader>")] extern internal static Shader FindBuiltin(string name);

        [FreeFunction("ShaderScripting::CreateFromCompiledData")] extern internal static Shader CreateFromCompiledData(
            byte[] compiledData, Shader[] dependencies);

        // ================================================================
        // LOD 控制 — Shader 多级细节层次
        // ================================================================
        // 🎯 maximumLOD / globalMaximumLOD 控制 Shader 的 LOD 级别。
        //   Shader 可以声明多个 SubShader，按质量从高到低排列。
        //   maximumLOD 值越小，使用越高质量的 SubShader。
        //
        //   maximumChunksOverride：覆盖 Shader 的 chunk 分块数量（内存控制）
        //   globalMaximumLOD：全局 LOD 上限，影响所有 Shader
        //   maximumLOD（实例）：单个 Shader 的 LOD 上限
        // ================================================================
        [NativeProperty("MaxChunksRuntimeOverride")] extern public static int maximumChunksOverride { get; set; }

        [NativeProperty("MaximumShaderLOD")] extern public int maximumLOD { get; set; }
        [NativeProperty("GlobalMaximumShaderLOD")] extern public static int globalMaximumLOD { get; set; }
        extern public bool isSupported {[NativeMethod("IsSupported")] get; }
        extern public static string globalRenderPipeline { get; set; }

        // ================================================================
        // ShaderKeyword 系统 — 运行时条件编译开关
        // ================================================================
        // 🎯 Shader 关键字控制着色器代码的条件分支，类似 C 的 #ifdef。
        //   启用一个关键字，GPU 就执行对应的代码路径；禁用则跳过。
        //
        // 【全局关键字 vs 局部关键字】
        //   - GlobalKeyword（全局）：EnableKeyword/DisableKeyword/SetKeyword
        //     影响所有使用该关键字的 Shader，适用于全局效果开关
        //   - LocalKeyword / LocalKeywordSpace（局部）：仅影响单个 Shader/Material
        //     通过 keywordSpace 获取 Shader 声明的局部关键字
        //
        // 【使用模式】
        //   Shader.EnableKeyword("_NORMALMAP");     // 全局启用法线贴图
        //   Shader.DisableKeyword("_NORMALMAP");    // 全局禁用
        //   Shader.SetKeyword(kw, true/false);      // 统一设置
        //   Shader.IsKeywordEnabled(kw);            // 查询状态
        //
        // 【enabledGlobalKeywords / globalKeywords】
        //   - enabledGlobalKeywords：当前实际启用的全局关键字（只读快照）
        //   - globalKeywords：所有已注册的全局关键字列表
        // ================================================================
        public static GlobalKeyword[] enabledGlobalKeywords { get { return GetEnabledGlobalKeywords(); } }
        public static GlobalKeyword[] globalKeywords { get { return GetAllGlobalKeywords(); } }
        extern public LocalKeywordSpace keywordSpace { get; }

        [FreeFunction("keywords::GetEnabledGlobalKeywords")] extern internal static GlobalKeyword[] GetEnabledGlobalKeywords();
        [FreeFunction("keywords::GetAllGlobalKeywords")] extern internal static GlobalKeyword[] GetAllGlobalKeywords();

        [FreeFunction("ShaderScripting::EnableKeyword")]    extern public static void EnableKeyword(string keyword);
        [FreeFunction("ShaderScripting::DisableKeyword")]   extern public static void DisableKeyword(string keyword);
        [FreeFunction("ShaderScripting::IsKeywordEnabled")] extern public static bool IsKeywordEnabled(string keyword);

        // ⚡ 内部快速路径 — 通过 GlobalKeyword 对象直接操作，跳过字符串解析
        [FreeFunction("ShaderScripting::EnableKeyword")]    extern internal static void EnableKeywordFast(GlobalKeyword keyword);
        [FreeFunction("ShaderScripting::DisableKeyword")]   extern internal static void DisableKeywordFast(GlobalKeyword keyword);
        [FreeFunction("ShaderScripting::SetKeyword")]       extern internal static void SetKeywordFast(GlobalKeyword keyword, bool value);
        [FreeFunction("ShaderScripting::IsKeywordEnabled")] extern internal static bool IsKeywordEnabledFast(GlobalKeyword keyword);

        // 🎯 公开 API：使用 in 关键字传递，避免 struct 拷贝
        public static void EnableKeyword(in GlobalKeyword keyword)          { EnableKeywordFast(keyword); }
        public static void DisableKeyword(in GlobalKeyword keyword)         { DisableKeywordFast(keyword); }
        public static void SetKeyword(in GlobalKeyword keyword, bool value) { SetKeywordFast(keyword, value); }
        public static bool IsKeywordEnabled(in GlobalKeyword keyword)       { return IsKeywordEnabledFast(keyword); }

        // ================================================================
        // 全局 Shader 属性查询
        // ================================================================
        // 🎯 获取全局 Shader 属性的数量和名称列表。
        //   propertyType 对应 ShaderPropertyType 枚举（Float/Int/Vector/Texture 等）。
        //   用于编辑器工具和运行时属性枚举场景。
        // ================================================================
        [FreeFunction("ShaderScripting::GetGlobalPropertyCount")] extern internal static int GetGlobalPropertyCount();
        [FreeFunction("ShaderScripting::GetGlobalPropertyCount")] extern private static int GetGlobalPropertyCountImpl(int propertyType);
        [FreeFunction("ShaderScripting::ExtractGlobalPropertyNames")] extern private static void ExtractGlobalPropertyNamesImpl(int propertyType, [Out] string[] names);

        // ================================================================
        // 渲染队列与合批控制
        // ================================================================
        // 🎯 renderQueue：Shader 的渲染队列值，决定渲染顺序。
        //   - Queue Background (1000)：背景
        //   - Queue Geometry (2000)：不透明几何体（默认）
        //   - Queue AlphaTest (2450)：透明测试
        //   - Queue Transparent (3000)：透明物体（从后往前渲染）
        //   - Queue Overlay (4000)：覆盖层（最后渲染）
        //
        // ⚡ disableBatching：控制此 Shader 是否参与 Dynamic/Static Batching
        //   合批可以减少 Draw Call，提升渲染性能
        // ================================================================
        extern public int renderQueue {[FreeFunction("ShaderScripting::GetRenderQueue", HasExplicitThis = true)] get; }
        extern internal DisableBatchingType disableBatching {[FreeFunction("ShaderScripting::GetDisableBatchingType", HasExplicitThis = true)] get; }

        // ⚡ 预热所有已编译的 Shader，避免首次使用时的编译卡顿
        //   通常在加载屏幕或场景切换时调用
        [FreeFunction] extern public static void WarmupAllShaders();

        // ================================================================
        // Tag 和 PropertyID 转换
        // ================================================================
        // 🎯 TagToID / IDToTag：Shader 标签的字符串 ↔ 整数 ID 转换
        //   用于 SubShader/Pass 级别的标签查询（如 "RenderType"、"LightMode"）
        //
        // ⚡ PropertyToID：将属性名字符串（如 "_MainTex"）转为整数 ID
        //   这是 Unity 渲染性能优化的基础：
        //   - 首次调用时解析字符串并缓存 ID
        //   - 后续使用整数 ID 直接索引，O(1) 时间复杂度
        //   - TryConvertPropertyIDToName 反向转换，用于调试
        // ================================================================
        [FreeFunction("ShaderScripting::TagToID")] extern internal static int TagToID(string name);
        [FreeFunction("ShaderScripting::IDToTag")] extern internal static string IDToTag(int name);

        [FreeFunction(Name = "ShaderScripting::PropertyToID", IsThreadSafe = true)] extern public static int PropertyToID(string name);
        [FreeFunction(Name = "ShaderScripting::PropertyIDToName", IsThreadSafe = true)] extern public static bool TryConvertPropertyIDToName(int propertyID, out string name);

        public static string PropertyIDToName(int id)
        {
            if (TryConvertPropertyIDToName(id, out string value))
                return value;
            return null;
        }

        // ================================================================
        // Shader 结构查询 — SubShader / Pass / Tag
        // ================================================================
        // 🎯 查询 Shader 的内部层次结构：
        //   - subshaderCount：SubShader 的数量（按兼容性从高到低排列）
        //   - passCount：第一个 SubShader 中的 Pass 数量
        //   - GetPassCountInSubshader(i)：指定 SubShader 的 Pass 数量
        //   - GetDependency(name)：获取依赖的其他 Shader
        //
        // 【FindPassTagValue】
        //   查询指定 Pass 上的 Tag 值。
        //   例如查找 LightMode = "UniversalForward" 的 Pass。
        //   在 SRP（Scriptable Render Pipeline）中用于定位特定渲染通道。
        //
        // 【FindSubshaderTagValue】
        //   查询 SubShader 级别的 Tag 值。
        //   例如 "RenderPipeline" = "UniversalPipeline" 用于 URP 识别。
        // ================================================================
        extern public Shader GetDependency(string name);

        extern public int passCount { [FreeFunction(Name = "ShaderScripting::GetPassCount", HasExplicitThis = true)] get; }
        extern public int subshaderCount { [FreeFunction(Name = "ShaderScripting::GetSubshaderCount", HasExplicitThis = true)] get; }

        [FreeFunction(Name = "ShaderScripting::GetPassCountInSubshader", HasExplicitThis = true)] extern public int GetPassCountInSubshader(int subshaderIndex);

        public Rendering.ShaderTagId FindPassTagValue(int passIndex, Rendering.ShaderTagId tagName)
        {
            if (passIndex < 0 || passIndex >= passCount)
                throw new ArgumentOutOfRangeException("passIndex");
            var id = Internal_FindPassTagValue(passIndex, tagName.id);
            return new Rendering.ShaderTagId { id = id };
        }

        public Rendering.ShaderTagId FindPassTagValue(int subshaderIndex, int passIndex, Rendering.ShaderTagId tagName)
        {
            if (subshaderIndex < 0 || subshaderIndex >= subshaderCount)
                throw new ArgumentOutOfRangeException("subshaderIndex");
            if (passIndex < 0 || passIndex >= GetPassCountInSubshader(subshaderIndex))
                throw new ArgumentOutOfRangeException("passIndex");
            var id = Internal_FindPassTagValueInSubShader(subshaderIndex, passIndex, tagName.id);
            return new Rendering.ShaderTagId { id = id };
        }

        public Rendering.ShaderTagId FindSubshaderTagValue(int subshaderIndex, Rendering.ShaderTagId tagName)
        {
            if (subshaderIndex < 0 || subshaderIndex >= subshaderCount)
                throw new ArgumentOutOfRangeException($"Invalid subshaderIndex {subshaderIndex}. Value must be in the range [0, {subshaderCount})");
            var id = Internal_FindSubshaderTagValue(subshaderIndex, tagName.id);
            return new Rendering.ShaderTagId { id = id };
        }

        [FreeFunction(Name = "ShaderScripting::FindPassTagValue", HasExplicitThis = true)] extern private int Internal_FindPassTagValue(int passIndex, int tagName);
        [FreeFunction(Name = "ShaderScripting::FindPassTagValue", HasExplicitThis = true)] extern private int Internal_FindPassTagValueInSubShader(int subShaderIndex, int passIndex, int tagName);
        [FreeFunction(Name = "ShaderScripting::FindSubshaderTagValue", HasExplicitThis = true)] extern private int Internal_FindSubshaderTagValue(int subShaderIndex, int tagName);

        // ================================================================
        // 自定义编辑器（Custom Editor）
        // ================================================================
        // 🎯 customEditor：此 Shader 在 Inspector 中使用的自定义编辑器类名。
        //   Internal_GetCustomEditorForRenderPipeline：在指定渲染管线下查找
        //   适用的自定义编辑器（不同管线可能有不同的材质编辑器）。
        // ================================================================
        [NativeProperty("CustomEditorName")] extern internal string customEditor { get; }
        [FreeFunction(Name = "ShaderScripting::GetCustomEditorForRenderPipeline", HasExplicitThis = true)] extern internal void Internal_GetCustomEditorForRenderPipeline(string renderPipelineType, out string customEditor);
        // TODO: get buffer is missing

        // ================================================================
        // 全局 Shader 属性设置/获取 — 所有 Material 共享的全局值
        // ================================================================
        // 🎯 全局属性影响所有使用对应属性名的 Shader。
        //   这些值存储在引擎的全局常量缓冲区中。
        //
        // 【使用场景】
        //   - 设置全局光源方向：Shader.SetGlobalVector("_WorldSpaceLightPos0", ...)
        //   - 设置全局雾效参数：Shader.SetGlobalFloat("_FogDensity", ...)
        //   - 设置全局环境贴图：Shader.SetGlobalTexture("_CubeMap", ...)
        //
        // 【优先级规则】
        //   全局属性 < 材质属性（Material.SetFloat 覆盖全局值）
        //   适用于所有使用该属性名的 Shader，不限于特定材质
        //
        // ⚡ 参数 name 必须先通过 Shader.PropertyToID() 转换为整数 ID。
        //
        // 【支持的类型】
        //   SetGlobalInt / SetGlobalFloat     — 标量值
        //   SetGlobalVector                   — Vector4 向量
        //   SetGlobalMatrix                   — Matrix4x4 矩阵
        //   SetGlobalTexture                  — 纹理（含 RenderTexture 子元素）
        //   SetGlobalBuffer                   — ComputeBuffer / GraphicsBuffer
        //   SetGlobalConstantBuffer           — 常量缓冲区（指定 offset/size）
        //   SetGlobalRayTracingAccelerationStructure — 光线追踪加速结构（DXR）
        // ================================================================
        [FreeFunction("ShaderScripting::SetGlobalInt")]     extern private static void SetGlobalIntImpl(int name, int value);
        [FreeFunction("ShaderScripting::SetGlobalFloat")]   extern private static void SetGlobalFloatImpl(int name, float value);
        [FreeFunction("ShaderScripting::SetGlobalVector")]  extern private static void SetGlobalVectorImpl(int name, Vector4 value);
        [FreeFunction("ShaderScripting::SetGlobalMatrix")]  extern private static void SetGlobalMatrixImpl(int name, Matrix4x4 value);
        [FreeFunction("ShaderScripting::SetGlobalTexture")] extern private static void SetGlobalTextureImpl(int name, Texture value);
        [FreeFunction("ShaderScripting::SetGlobalRenderTexture")] extern private static void SetGlobalRenderTextureImpl(int name, RenderTexture value, Rendering.RenderTextureSubElement element);
        [FreeFunction("ShaderScripting::SetGlobalBuffer")]  extern private static void SetGlobalBufferImpl(int name, ComputeBuffer value);
        [FreeFunction("ShaderScripting::SetGlobalBuffer")]  extern private static void SetGlobalGraphicsBufferImpl(int name, GraphicsBuffer value);
        [FreeFunction("ShaderScripting::SetGlobalConstantBuffer")] extern private static void SetGlobalConstantBufferImpl(int name, ComputeBuffer value, int offset, int size);
        [FreeFunction("ShaderScripting::SetGlobalConstantBuffer")] extern private static void SetGlobalConstantGraphicsBufferImpl(int name, GraphicsBuffer value, int offset, int size);
        [FreeFunction("ShaderScripting::SetGlobalRayTracingAccelerationStructure")] extern private static void SetGlobalRayTracingAccelerationStructureImpl(int name, RayTracingAccelerationStructure accelerationStructure);

        // ⚡ 全局属性的 Get 对应版本
        [FreeFunction("ShaderScripting::GetGlobalInt")]     extern private static int       GetGlobalIntImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalFloat")]   extern private static float     GetGlobalFloatImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalVector")]  extern private static Vector4   GetGlobalVectorImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalMatrix")]  extern private static Matrix4x4 GetGlobalMatrixImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalTexture")] extern private static Texture   GetGlobalTextureImpl(int name);

        // ================================================================
        // 全局数组属性 — 批量传递数据到 GPU
        // ================================================================
        // 🎯 数组版本的全局属性，用于传递多个值（如骨骼矩阵数组、光源位置数组）。
        //   Set 系列：传入 C# 数组和元素数量
        //   Get 系列：获取当前全局数组值
        //   Extract 系列：将内部数据复制到预分配的数组中（避免 GC 分配）
        //   Count 系列：查询数组属性的当前元素数量
        // ================================================================
        [FreeFunction("ShaderScripting::SetGlobalFloatArray")]  extern private static void SetGlobalFloatArrayImpl(int name, float[] values, int count);
        [FreeFunction("ShaderScripting::SetGlobalVectorArray")] extern private static void SetGlobalVectorArrayImpl(int name, Vector4[] values, int count);
        [FreeFunction("ShaderScripting::SetGlobalMatrixArray")] extern private static void SetGlobalMatrixArrayImpl(int name, Matrix4x4[] values, int count);

        [FreeFunction("ShaderScripting::GetGlobalFloatArray")]  extern private static float[]     GetGlobalFloatArrayImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalVectorArray")] extern private static Vector4[]   GetGlobalVectorArrayImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalMatrixArray")] extern private static Matrix4x4[] GetGlobalMatrixArrayImpl(int name);

        [FreeFunction("ShaderScripting::GetGlobalFloatArrayCount")]  extern private static int GetGlobalFloatArrayCountImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalVectorArrayCount")] extern private static int GetGlobalVectorArrayCountImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalMatrixArrayCount")] extern private static int GetGlobalMatrixArrayCountImpl(int name);

        [FreeFunction("ShaderScripting::ExtractGlobalFloatArray")]  extern private static void ExtractGlobalFloatArrayImpl(int name, [Out] float[] val);
        [FreeFunction("ShaderScripting::ExtractGlobalVectorArray")] extern private static void ExtractGlobalVectorArrayImpl(int name, [Out] Vector4[] val);
        [FreeFunction("ShaderScripting::ExtractGlobalMatrixArray")] extern private static void ExtractGlobalMatrixArrayImpl(int name, [Out] Matrix4x4[] val);
    }
}
