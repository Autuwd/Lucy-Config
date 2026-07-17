// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// 🎯 Material — Shader 属性容器：定义"用什么参数来渲染"
// ================================================================
//
// 📌 本文件概述：
//   Material.bindings.cs 是 Unity 材质系统在 C# 层的绑定入口。
//   Material 是 Shader 的实例化容器，持有具体的渲染属性值。
//
// 【Shader vs Material 核心关系（最重要）】
//   Shader = 渲染蓝图 / 配方（定义"怎么画"的 GPU 指令）
//   Material = 渲染实例 / 实际配置（定义"用什么颜色/纹理/参数来画"）
//
//   一个 Shader 可以被成百上千个 Material 共享：
//   - Material A 使用 Standard Shader，红色 + 木纹
//   - Material B 使用 Standard Shader，蓝色 + 金属纹理
//   - 它们共享同一个 Shader，但属性值不同
//
//   类比：Shader 是菜谱（宫保鸡丁的做法），Material 是具体的菜品
//   （一份微辣不放花生的宫保鸡丁）。
//
// 【Material 在渲染管线中的角色】
//   1. Renderer 组件持有 Material 引用
//   2. 渲染时，引擎将 Material 的属性值上传到 GPU 常量缓冲区
//   3. GPU 执行 Shader 指令，使用这些属性值计算每个像素的颜色
//   4. Material 关键字控制 Shader 内部的条件分支
//
// 【Material 属性类型】
//   - Float / Int：标量值（如金属度 _Metallic、光滑度 _Glossiness）
//   - Color / Vector：颜色和向量（如 _Color、_EmissionColor）
//   - Texture：纹理贴图（如 _MainTex、_BumpMap）
//   - Matrix：矩阵（如变换矩阵）
//   - ComputeBuffer / GraphicsBuffer：计算缓冲区（Compute Shader 数据）
//   - ConstantBuffer：常量缓冲区（指定 offset/size 的数据段）
//
// 【Material 关键字系统】
//   Material 可以独立于 Shader 全局关键字启用/禁用局部关键字。
//   - LocalKeyword：仅影响当前 Material
//   - GlobalKeyword：通过 Shader.EnableKeyword 全局启用
//   - 关键字影响 Shader 内部的 #if 分支，决定执行哪段 GPU 代码
//
// 【Material 继承体系（材质变体）】
//   Material 支持 parent/child 继承链：
//   - 父材质（parent）定义基础属性
//   - 子材质（variant）覆盖部分属性
//   - isVariant 标记是否为变体材质
//   - 属性覆盖系统（ApplyPropertyOverride/RevertPropertyOverride）
//
// 【MaterialPropertyBlock vs Material】
//   - Material：修改影响所有使用该 Material 的 Renderer
//   - MaterialPropertyBlock：每 Renderer 独立的属性覆盖，不改变共享 Material
//   - MaterialPropertyBlock 适用于 GPU Instancing 场景下的个性化渲染
//
// 【性能提示】
//   - Material 实例化（new Material / Instantiate）有 GC 开销
//   - 共享材质（SharedMaterial）不产生副本，修改会影响所有引用者
//   - 属性设置使用 int ID（PropertyToID）比字符串快
//   - 渲染完成后及时清理不再需要的动态 Material
//
// 📍 对应 C++ 头文件：
//   Runtime/Shaders/Material.h
//   Runtime/Graphics/ShaderScriptBindings.h
// ================================================================

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    // ================================================================
    // Material - Shader 属性实例容器
    // ================================================================
    //
    // 🎯 Material 是 Unity 渲染系统中最常用的对象之一。
    //   每个 Renderer（MeshRenderer、SkinnedMeshRenderer 等）引用一个或多个 Material。
    //   Material 将 Shader 的属性声明实例化为具体的值。
    //
    // 【继承链】
    //   Material -> Object（UnityEngine.Object）
    //   不是 sealed class — 可以被继承（如自定义 Material 扩展）
    //
    // 【Material 的内部数据结构】
    //   - Shader 引用：指向关联的 Shader 对象
    //   - 属性表（Property Map）：存储所有已设置的属性值（key = propertyID）
    //   - 关键字状态：启用/禁用的 LocalKeyword 列表
    //   - 渲染队列覆盖：可选地覆盖 Shader 默认的 renderQueue
    //   - 父材质引用：用于属性继承体系
    //
    // 【Material 与 GPU 的数据流】
    //   CPU 端（Material 属性值） → SetPass/Draw Call → GPU 常量缓冲区 → Shader 执行
    //
    // 【Default Material】
    //   - GetDefaultMaterial()：引擎默认材质（洋红色，用于丢失材质时的 fallback）
    //   - GetDefaultParticleMaterial()：默认粒子材质
    //   - GetDefaultLineMaterial()：默认线条材质
    // ================================================================
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/Material.h")]
    public partial class Material : Object
    {
        // ================================================================
        // 构造函数 — Material 的创建方式
        // ================================================================
        // 🎯 两种合法的构造方式：
        //
        //   Material(Shader shader)：
        //     从 Shader 创建空材质，所有属性使用 Shader 的默认值。
        //     这是最常见的创建方式。
        //     例如：new Material(Shader.Find("Standard"))
        //
        //   Material(Material source)：
        //     复制构造，从已有 Material 创建副本。
        //     副本继承源 Material 的所有属性值和关键字状态。
        //     ⚠️ 这是深拷贝，修改副本不会影响源 Material。
        //     [RequiredByNativeCode] 标记保证引擎内部可调用此构造函数。
        //
        //   Material(string contents)：
        //     ❌ 已废弃。不能从 Shader 源代码字符串创建 Material。
        //     必须使用 .shader 资源文件。
        // ================================================================
        [FreeFunction("MaterialScripting::CreateWithShader")]   extern private static void CreateWithShader([Writable] Material self, [NotNull] Shader shader);
        [FreeFunction("MaterialScripting::CreateWithMaterial")] extern private static void CreateWithMaterial([Writable] Material self, [NotNull] Material source);

        public Material(Shader shader)   { CreateWithShader(this, shader); }
        // will otherwise be stripped if scene only uses default materials not explicitly referenced
        // (ie some components will get a default material if a material reference is null)
        [RequiredByNativeCode]
        public Material(Material source) { CreateWithMaterial(this, source); }

        // TODO: is it time to make it deprecated with error?
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Creating materials from shader source string is no longer supported. Use Shader assets instead.", true)]
        public Material(string contents) {}

        // ⚡ 引擎内部默认材质获取（编辑器/运行时 fallback 用）
        static extern internal Material GetDefaultMaterial();
        static extern internal Material GetDefaultParticleMaterial();
        static extern internal Material GetDefaultLineMaterial();

        // ================================================================
        // shader 属性 — 关联的 Shader 对象
        // ================================================================
        // 🎯 获取或设置此 Material 使用的 Shader。
        //   运行时切换 Shader 会保留可匹配的属性值，丢弃不兼容的属性。
        //   例如：从 Standard 切换到 Unlit/Color 会保留 _Color 但丢弃 _Metallic。
        // ================================================================
        extern public Shader shader { get; set; }

        // ================================================================
        // 便捷属性 — color / mainTexture / mainTextureOffset / mainTextureScale
        // ================================================================
        // 🎯 这是最常用的材质属性快捷方式，等效于：
        //   - color         ↔ GetColor("_Color") / SetColor("_Color", value)
        //   - mainTexture   ↔ GetTexture("_MainTex") / SetTexture("_MainTex", value)
        //   - mainTextureOffset ↔ GetTextureOffset("_MainTex")
        //   - mainTextureScale  ↔ GetTextureScale("_MainTex")
        //
        // 💡 内部机制：
        //   先通过 GetFirstPropertyNameIdByAttribute 查找标记了
        //   [MainColor] / [MainTexture] 属性特性的 Shader 属性。
        //   如果找到就使用该属性，否则回退到硬编码的 "_Color" / "_MainTex"。
        //   这样支持自定义属性名，同时保持向后兼容。
        //
        // ⚠️ 注意：修改 color/mainTexture 影响所有引用此 Material 的 Renderer。
        // ================================================================
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly int k_MainTexId = Shader.PropertyToID("_MainTex");

        public Color color
        {
            get
            {
                // Try to find property with [MainColor] attribute and use that, otherwise fallback to old hardcoded one.
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainColor);
                if (nameId >= 0)
                    return GetColor(nameId);
                else
                    return GetColor(k_ColorId);
            }
            set
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainColor);
                if (nameId >= 0)
                    SetColor(nameId, value);
                else
                    SetColor(k_ColorId, value);
            }
        }
        public Texture mainTexture
        {
            get
            {
                // Try to find property with [MainTexture] attribute and use that, otherwise fallback to old hardcoded one.
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    return GetTexture(nameId);
                else
                    return GetTexture(k_MainTexId);
            }
            set
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    SetTexture(nameId, value);
                else
                    SetTexture(k_MainTexId, value);
            }
        }
        public Vector2 mainTextureOffset
        {
            get
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    return GetTextureOffset(nameId);
                else
                    return GetTextureOffset(k_MainTexId);
            }
            set
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    SetTextureOffset(nameId, value);
                else
                    SetTextureOffset(k_MainTexId, value);
            }
        }
        public Vector2 mainTextureScale
        {
            get
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    return GetTextureScale(nameId);
                else
                    return GetTextureScale(k_MainTexId);
            }
            set
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    SetTextureScale(nameId, value);
                else
                    SetTextureScale(k_MainTexId, value);
            }
        }
        [NativeName("GetFirstPropertyNameIdByAttributeFromScript")] extern private int GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags attributeFlag);

        // ================================================================
        // 属性存在性检查 — HasProperty / HasFloat / HasTexture 等
        // ================================================================
        // 🎯 在运行时检查 Shader 是否声明了指定名称/ID 的属性。
        //   用于安全地设置属性前的防御性检查：
        //     if (mat.HasProperty("_BumpMap"))
        //         mat.SetTexture("_BumpMap", normalMap);
        //
        // 💡 类型对应关系：
        //   HasProperty  — 通用检查（任意类型属性）
        //   HasFloat     — Float / Range 类型
        //   HasInt       — Float 属性（向后兼容，旧版整数存储为 Float）
        //   HasInteger   — 整数类型（Unity 2021.2+ 新增）
        //   HasTexture   — Texture2D / Texture3D / CubeMap 等
        //   HasMatrix    — Matrix4x4 类型
        //   HasVector    — Vector4 / Color 类型（Color 底层是 Vector4）
        //   HasColor     — 与 HasVector 相同（语义别名）
        //   HasBuffer    — ComputeBuffer / GraphicsBuffer 类型
        //   HasConstantBuffer — ConstantBuffer 类型（常量缓冲区）
        // ================================================================
        [NativeName("HasPropertyFromScript")] extern public bool HasProperty(int nameID);
        public bool HasProperty(string name) { return HasProperty(Shader.PropertyToID(name)); }

        [NativeName("HasFloatFromScript")] extern private bool HasFloatImpl(int name);
        public bool HasFloat(string name) { return HasFloatImpl(Shader.PropertyToID(name)); }
        public bool HasFloat(int nameID) { return HasFloatImpl(nameID); }

        public bool HasInt(string name) { return HasFloatImpl(Shader.PropertyToID(name)); }
        public bool HasInt(int nameID) { return HasFloatImpl(nameID); }

        [NativeName("HasIntegerFromScript")] extern private bool HasIntImpl(int name);
        public bool HasInteger(string name) { return HasIntImpl(Shader.PropertyToID(name)); }
        public bool HasInteger(int nameID) { return HasIntImpl(nameID); }
        [NativeName("HasTextureFromScript")] extern private bool HasTextureImpl(int name);
        public bool HasTexture(string name) { return HasTextureImpl(Shader.PropertyToID(name)); }
        public bool HasTexture(int nameID) { return HasTextureImpl(nameID); }
        [NativeName("HasMatrixFromScript")] extern private bool HasMatrixImpl(int name);
        public bool HasMatrix(string name) { return HasMatrixImpl(Shader.PropertyToID(name)); }
        public bool HasMatrix(int nameID) { return HasMatrixImpl(nameID); }
        [NativeName("HasVectorFromScript")] extern private bool HasVectorImpl(int name);
        public bool HasVector(string name) { return HasVectorImpl(Shader.PropertyToID(name)); }
        public bool HasVector(int nameID) { return HasVectorImpl(nameID); }
        public bool HasColor(string name) { return HasVectorImpl(Shader.PropertyToID(name)); }
        public bool HasColor(int nameID) { return HasVectorImpl(nameID); }
        [NativeName("HasBufferFromScript")] extern private bool HasBufferImpl(int name);
        public bool HasBuffer(string name) { return HasBufferImpl(Shader.PropertyToID(name)); }
        public bool HasBuffer(int nameID) { return HasBufferImpl(nameID); }
        [NativeName("HasConstantBufferFromScript")] extern private bool HasConstantBufferImpl(int name);
        public bool HasConstantBuffer(string name) { return HasConstantBufferImpl(Shader.PropertyToID(name)); }
        public bool HasConstantBuffer(int nameID) { return HasConstantBufferImpl(nameID); }

        // ================================================================
        // 渲染队列 — renderQueue / rawRenderQueue
        // ================================================================
        // 🎯 renderQueue：控制此 Material 的渲染排序队列。
        //   - get（实际渲染队列）：如果未设置覆盖值，返回 Shader 的默认队列
        //   - set（自定义队列）：覆盖 Shader 默认的渲染队列
        //   - rawRenderQueue：返回原始的自定义队列值（未设置则返回 -1）
        //
        // 💡 渲染队列决定渲染顺序：
        //   值越小越先渲染。不透明物体（2000）先渲染，透明物体（3000）后渲染。
        //   手动调整 renderQueue 可以控制同一队列内的渲染先后。
        // ================================================================
        extern public int renderQueue {[NativeName("GetActualRenderQueue")] get; [NativeName("SetCustomRenderQueue")] set; }
        extern public int rawRenderQueue {[NativeName("GetCustomRenderQueue")] get; }

        // ================================================================
        // Material 关键字系统 — 条件编译开关
        // ================================================================
        // 🎯 Material 级别的关键字控制，独立于全局 Shader 关键字。
        //   启用/禁用关键字会影响 Shader 内部的 #if 分支执行路径。
        //
        // 【字符串版本（旧 API）】
        //   EnableKeyword("NORMALMAP")     — 启用名为 NORMALMAP 的关键字
        //   DisableKeyword("NORMALMAP")    — 禁用
        //   IsKeywordEnabled("NORMALMAP")  — 查询状态
        //
        // 【LocalKeyword 版本（新 API，推荐）】
        //   使用类型化的 LocalKeyword 对象，避免字符串查找开销：
        //   var kw = new LocalKeyword(shader, "_NORMALMAP");
        //   mat.EnableKeyword(kw);
        //   mat.SetKeyword(kw, true/false);   — 统一设置
        //   mat.IsKeywordEnabled(kw);          — 查询
        //
        // 【enabledKeywords 属性】
        //   获取/设置当前 Material 启用的所有 LocalKeyword 列表。
        //   可以一次性替换整个关键字集合。
        //
        // ⚡ 注意区分：
        //   - Shader.EnableKeyword() → 全局，影响所有 Material
        //   - Material.EnableKeyword() → 局部，仅影响此 Material
        // ================================================================
        extern public void EnableKeyword(string keyword);
        extern public void DisableKeyword(string keyword);
        extern public bool IsKeywordEnabled(string keyword);

        [FreeFunction("MaterialScripting::EnableKeyword", HasExplicitThis = true)] extern private void EnableLocalKeyword(LocalKeyword keyword);
        [FreeFunction("MaterialScripting::DisableKeyword", HasExplicitThis = true)] extern private void DisableLocalKeyword(LocalKeyword keyword);
        [FreeFunction("MaterialScripting::SetKeyword", HasExplicitThis = true)] extern private void SetLocalKeyword(LocalKeyword keyword, bool value);
        [FreeFunction("MaterialScripting::IsKeywordEnabled", HasExplicitThis = true)] extern private bool IsLocalKeywordEnabled(LocalKeyword keyword);

        public void EnableKeyword(in LocalKeyword keyword) { EnableLocalKeyword(keyword); }
        public void DisableKeyword(in LocalKeyword keyword) { DisableLocalKeyword(keyword); }
        public void SetKeyword(in LocalKeyword keyword, bool value) { SetLocalKeyword(keyword, value); }
        public bool IsKeywordEnabled(in LocalKeyword keyword) { return IsLocalKeywordEnabled(keyword); }

        [FreeFunction("MaterialScripting::GetEnabledKeywords", HasExplicitThis = true)] extern private LocalKeyword[] GetEnabledKeywords();
        [FreeFunction("MaterialScripting::SetEnabledKeywords", HasExplicitThis = true)] extern private void SetEnabledKeywords(LocalKeyword[] keywords);
        public LocalKeyword[] enabledKeywords { get { return GetEnabledKeywords(); } set { SetEnabledKeywords(value); } }

        // ================================================================
        // 全局照明（GI）与 GPU Instancing
        // ================================================================
        // 🎯 globalIlluminationFlags：控制此 Material 对全局照明系统的贡献。
        //   - None：完全参与 GI（动态 + 静态）
        //   - EmissiveIsBlack：发射光为黑色时不贡献 GI
        //   - NotEmissive：不贡献 GI（但仍可接收 GI）
        //   - RealtimeEmissive / BakedEmissive：实时/烘焙 GI 控制
        //
        // ⚡ doubleSidedGI：双面 GI 标记，让薄物体（如纸张）两面都参与 GI。
        //
        // ⚡ enableInstancing：启用 GPU Instancing 支持。
        //   开启后，相同 Mesh + 相同 Shader 的多个对象可以合并为一次 Draw Call。
        //   适用于大量相同模型的场景（如草地、粒子、建筑群）。
        // ================================================================
        extern public MaterialGlobalIlluminationFlags globalIlluminationFlags { get; set; }
        extern public bool doubleSidedGI { get; set; }
        [NativeProperty("EnableInstancingVariants")] extern public bool enableInstancing { get; set; }

        // ================================================================
        // Pass 管理 — 渲染通道控制
        // ================================================================
        // 🎯 Shader 由多个 Pass 组成，每个 Pass 是一次完整的渲染通道。
        //   Material 可以控制每个 Pass 的启用/禁用状态。
        //
        // 【Pass 的典型用途】
        //   - Forward 渲染的 Base Pass（主光源 + 环境光）
        //   - Forward 渲染的 Additional Pass（逐像素附加光源）
        //   - Shadow Caster Pass（阴影投射）
        //   - Depth Only Pass（深度预渲染）
        //   - Post Processing Pass（后处理）
        //
        // 【passCount】
        //   返回关联 Shader 的 Pass 数量（只读）。
        //
        // 【SetShaderPassEnabled / GetShaderPassEnabled】
        //   通过 Pass 名称（如 "ForwardBase"、"ShadowCaster"）控制启用/禁用。
        //   禁用某个 Pass 意味着渲染时不执行该通道。
        //
        // 【FindPass / GetPassName】
        //   FindPass：通过名称查找 Pass 索引
        //   GetPassName：通过索引获取 Pass 名称
        // ================================================================
        extern public int passCount { [NativeName("GetShader()->GetPassCount")] get; }
        [FreeFunction("MaterialScripting::SetShaderPassEnabled", HasExplicitThis = true)] extern public void SetShaderPassEnabled(string passName, bool enabled);
        [FreeFunction("MaterialScripting::GetShaderPassEnabled", HasExplicitThis = true)] extern public bool GetShaderPassEnabled(string passName);
        extern public string GetPassName(int pass);
        extern public int FindPass(string passName);

        // ================================================================
        // Shader Tag 覆盖与查询
        // ================================================================
        // 🎯 SetOverrideTag：覆盖 Material 级别的 Shader Tag 值。
        //   例如：mat.SetOverrideTag("RenderType", "TransparentCutout")
        //   覆盖仅影响此 Material，不改变 Shader 原始 Tag。
        //
        // ⚡ GetTag：查询 Shader Tag 的值。
        //   searchFallbacks=true 时会搜索 SubShader 的 fallback 链。
        //   defaultValue 为未找到时的返回值。
        // ================================================================
        extern public void SetOverrideTag(string tag, string val);
        [NativeName("GetTag")] extern private string GetTagImpl(string tag, bool currentSubShaderOnly, string defaultValue);
        public string GetTag(string tag, bool searchFallbacks, string defaultValue) { return GetTagImpl(tag, !searchFallbacks, defaultValue); }
        public string GetTag(string tag, bool searchFallbacks) { return GetTagImpl(tag, !searchFallbacks, ""); }

        // ================================================================
        // 高级操作 — Lerp / SetPass / CopyProperties
        // ================================================================
        // 🎯 Lerp：在两个 Material 之间线性插值属性值。
        //   mat.Lerp(matA, matB, t)：将 mat 的属性设为 matA 和 matB 的线性插值。
        //   t=0 时完全等于 matA，t=1 时完全等于 matB。
        //   仅对同类型属性进行插值（Float、Color、Vector）。
        //
        // ⚡ SetPass：激活指定索引的 Pass，准备进行渲染。
        //   返回 true 表示 Pass 激活成功。通常由引擎自动调用。
        //   手动调用场景：自定义 GL 调用或 CommandBuffer 操作。
        //
        // 📌 CopyPropertiesFromMaterial：从另一个 Material 复制所有属性值。
        //   与构造函数的复制不同，这是在已有 Material 上执行覆盖。
        //   CopyMatchingPropertiesFromMaterial：仅复制同名属性。
        // ================================================================
        [FreeFunction("MaterialScripting::Lerp", HasExplicitThis = true, ThrowsException = true)] extern public void Lerp(Material start, Material end, float t);
        [FreeFunction("MaterialScripting::SetPass", HasExplicitThis = true)] extern public bool SetPass(int pass);
        [FreeFunction("MaterialScripting::CopyPropertiesFrom", HasExplicitThis = true)] extern public void CopyPropertiesFromMaterial(Material mat);
        [FreeFunction("MaterialScripting::CopyMatchingPropertiesFrom", HasExplicitThis = true)] extern public void CopyMatchingPropertiesFromMaterial(Material mat);

        // ================================================================
        // shaderKeywords 属性 — 字符串数组版本的关键字管理
        // ================================================================
        // 🎯 以字符串数组形式获取/设置所有启用的关键字。
        //   这是旧版 API，推荐使用 LocalKeyword 版本。
        //   适用于序列化/反序列化场景。
        // ================================================================
        [FreeFunction("MaterialScripting::GetShaderKeywords", HasExplicitThis = true)] extern private string[] GetShaderKeywords();
        [FreeFunction("MaterialScripting::SetShaderKeywords", HasExplicitThis = true)] extern private void SetShaderKeywords(string[] names);
        public string[] shaderKeywords { get { return GetShaderKeywords(); } set { SetShaderKeywords(value); } }

        // ================================================================
        // 属性枚举 — 获取 Material 的属性列表
        // ================================================================
        // 🎯 获取 Material 声明的所有属性名称。
        //   GetPropertyNames：按属性类型（Float/Int/Vector/Texture/Matrix）过滤
        //   GetTexturePropertyNames：仅获取纹理属性名称
        //   GetTexturePropertyNameIDs：仅获取纹理属性的整数 ID
        //   GetPropertyCount：属性总数
        //
        // ⚡ 优化版本（List<T> 重载）：直接填充已有列表，避免数组分配。
        // ================================================================
        [FreeFunction("MaterialScripting::GetPropertyNames", HasExplicitThis = true)]
        extern private string[] GetPropertyNamesImpl(int propertyType);

        [FreeFunction("MaterialScripting::GetPropertyCount", HasExplicitThis = true)]
        extern internal int GetPropertyCount();

        extern public int ComputeCRC();

        [FreeFunction("MaterialScripting::GetTexturePropertyNames", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public String[] GetTexturePropertyNames();

        [FreeFunction("MaterialScripting::GetTexturePropertyNameIDs", HasExplicitThis = true)]
        extern public int[] GetTexturePropertyNameIDs();

        [FreeFunction("MaterialScripting::GetTexturePropertyNamesInternal", HasExplicitThis = true)]
        extern private void GetTexturePropertyNamesInternal([Out,NotNull] List<string> outNames);

        [FreeFunction("MaterialScripting::GetTexturePropertyNameIDsInternal", HasExplicitThis = true)]
        extern private void GetTexturePropertyNameIDsInternal([Out,NotNull] List<int> outNames);

        public void GetTexturePropertyNames(List<string> outNames)
        {
            if (outNames == null)
            {
                throw new ArgumentNullException(nameof(outNames));
            }

            GetTexturePropertyNamesInternal(outNames);
        }

        public void GetTexturePropertyNameIDs(List<int> outNames)
        {
            if (outNames == null)
            {
                throw new ArgumentNullException(nameof(outNames));
            }

            GetTexturePropertyNameIDsInternal(outNames);
        }


        // ================================================================
        // 属性设置器/获取器 — SetXxx / GetXxx
        // ================================================================
        // 🎯 Material 属性的读写 API，是 Material 最核心的操作。
        //   每种类型都有 Set（写入）和 Get（读取）方法。
        //
        // 【基础类型】
        //   SetInt / GetInt       — 整数值（渲染管线参数、开关）
        //   SetFloat / GetFloat   — 浮点值（金属度、光滑度、时间变量）
        //   SetColor / GetColor   — 颜色值（主色调、自发光颜色）
        //   SetMatrix / GetMatrix — 4x4 矩阵（UV 变换、自定义变换）
        //
        // 【纹理类型】
        //   SetTexture / GetTexture              — 纹理对象（Texture2D/Cube 等）
        //   SetRenderTexture / GetRenderTexture  — RenderTexture（可写渲染纹理）
        //   SetTextureOffset / SetTextureScale   — 纹理 UV 偏移和缩放
        //   GetTextureScaleAndOffset             — 一次性获取 UV 变换
        //
        // 【缓冲区类型】
        //   SetBuffer / GetBuffer           — ComputeBuffer / GraphicsBuffer
        //   SetConstantBuffer               — 常量缓冲区（指定 offset/size 段）
        //
        // ⚡ 所有方法的 name 参数必须是 int ID（通过 Shader.PropertyToID 转换）。
        //   字符串版本的重载内部会自动调用 PropertyToID。
        //   但如果需要频繁设置同一属性，建议预缓存 ID：
        //     static readonly int ColorID = Shader.PropertyToID("_Color");
        //     mat.SetColor(ColorID, Color.red);
        // ================================================================
        [NativeName("SetIntFromScript")]     extern private void SetIntImpl(int name, int value);
        [NativeName("SetFloatFromScript")]   extern private void SetFloatImpl(int name, float value);
        [NativeName("SetColorFromScript")]   extern private void SetColorImpl(int name, Color value);
        [NativeName("SetMatrixFromScript")]  extern private void SetMatrixImpl(int name, Matrix4x4 value);
        [NativeName("SetTextureFromScript")] extern private void SetTextureImpl(int name, Texture value);
        [NativeName("SetRenderTextureFromScript")] extern private void SetRenderTextureImpl(int name, RenderTexture value, Rendering.RenderTextureSubElement element);
        [NativeName("SetBufferFromScript")] extern private void SetBufferImpl(int name, ComputeBuffer value);
        [NativeName("SetBufferFromScript")] extern private void SetGraphicsBufferImpl(int name, GraphicsBuffer value);
        [NativeName("SetConstantBufferFromScript")] extern private void SetConstantBufferImpl(int name, ComputeBuffer value, int offset, int size);
        [NativeName("SetConstantBufferFromScript")] extern private void SetConstantGraphicsBufferImpl(int name, GraphicsBuffer value, int offset, int size);

        [NativeName("GetIntFromScript")]     extern private int       GetIntImpl(int name);
        [NativeName("GetFloatFromScript")]   extern private float     GetFloatImpl(int name);
        [NativeName("GetColorFromScript")]   extern private Color     GetColorImpl(int name);
        [NativeName("GetMatrixFromScript")]  extern private Matrix4x4 GetMatrixImpl(int name);
        [NativeName("GetTextureFromScript")] extern private Texture   GetTextureImpl(int name);
        [NativeName("GetBufferFromScript")] extern private GraphicsBufferHandle GetBufferImpl(int name);
        [NativeName("GetConstantBufferFromScript")] extern private GraphicsBufferHandle GetConstantBufferImpl(int name);

        // ================================================================
        // 数组属性 — 批量数据传递到 GPU
        // ================================================================
        // 🎯 数组版本的属性设置/获取，用于传递批量数据：
        //   - SetFloatArray / GetFloatArray     — 浮点数组（权重值等）
        //   - SetVectorArray / GetVectorArray   — 向量数组（骨骼偏移等）
        //   - SetColorArray / GetColorArray     — 颜色数组（渐变色等）
        //   - SetMatrixArray / GetMatrixArray   — 矩阵数组（骨骼变换矩阵）
        //
        // ⚡ Extract 系列：将内部数据复制到预分配的数组中，避免 GC 分配。
        //   适用于频繁读取的性能敏感场景。
        //
        // 💡 使用场景：
        //   - SkinnedMeshRenderer 的骨骼矩阵数组
        //   - 自定义渲染的光源位置/颜色数组
        //   - 粒子系统的颜色渐变数组
        // ================================================================
        [FreeFunction(Name = "MaterialScripting::SetFloatArray", HasExplicitThis = true)]  extern private void SetFloatArrayImpl(int name, float[] values, int count);
        [FreeFunction(Name = "MaterialScripting::SetVectorArray", HasExplicitThis = true)] extern private void SetVectorArrayImpl(int name, Vector4[] values, int count);
        [FreeFunction(Name = "MaterialScripting::SetColorArray", HasExplicitThis = true)]  extern private void SetColorArrayImpl(int name, Color[] values, int count);
        [FreeFunction(Name = "MaterialScripting::SetMatrixArray", HasExplicitThis = true)] extern private void SetMatrixArrayImpl(int name, Matrix4x4[] values, int count);

        [FreeFunction(Name = "MaterialScripting::GetFloatArray", HasExplicitThis = true)]  extern private float[]     GetFloatArrayImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetVectorArray", HasExplicitThis = true)] extern private Vector4[]   GetVectorArrayImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetColorArray", HasExplicitThis = true)]  extern private Color[]     GetColorArrayImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetMatrixArray", HasExplicitThis = true)] extern private Matrix4x4[] GetMatrixArrayImpl(int name);

        [FreeFunction(Name = "MaterialScripting::GetFloatArrayCount", HasExplicitThis = true)]  extern private int GetFloatArrayCountImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetVectorArrayCount", HasExplicitThis = true)] extern private int GetVectorArrayCountImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetColorArrayCount", HasExplicitThis = true)]  extern private int GetColorArrayCountImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetMatrixArrayCount", HasExplicitThis = true)] extern private int GetMatrixArrayCountImpl(int name);

        [FreeFunction(Name = "MaterialScripting::ExtractFloatArray", HasExplicitThis = true)]  extern private void ExtractFloatArrayImpl(int name, [Out] float[] val);
        [FreeFunction(Name = "MaterialScripting::ExtractVectorArray", HasExplicitThis = true)] extern private void ExtractVectorArrayImpl(int name, [Out] Vector4[] val);
        [FreeFunction(Name = "MaterialScripting::ExtractColorArray", HasExplicitThis = true)]  extern private void ExtractColorArrayImpl(int name, [Out] Color[] val);
        [FreeFunction(Name = "MaterialScripting::ExtractMatrixArray", HasExplicitThis = true)] extern private void ExtractMatrixArrayImpl(int name, [Out] Matrix4x4[] val);

        // ================================================================
        // 纹理 UV 变换
        // ================================================================
        // 🎯 获取/设置指定纹理属性的 UV 偏移和缩放。
        //   GetTextureScaleAndOffset：一次性获取 Scale 和 Offset（返回 Vector4）
        //   SetTextureOffset：设置 UV 偏移（纹理滚动效果）
        //   SetTextureScale：设置 UV 缩放（纹理重复/平铺）
        //
        // 💡 UV 变换的数学：
        //   finalUV = rawUV * scale + offset
        //   scale=(2,2) 让纹理在两个方向上重复 2 次
        //   offset=(0.5,0) 让纹理水平偏移半个周期
        // ================================================================
        [NativeName("GetTextureScaleAndOffsetFromScript")] extern internal Vector4 GetTextureScaleAndOffsetImpl(int name);
        [NativeName("SetTextureOffsetFromScript")] extern private void SetTextureOffsetImpl(int name, Vector2 offset);
        [NativeName("SetTextureScaleFromScript")]  extern private void SetTextureScaleImpl(int name, Vector2 scale);

        // ================================================================
        // Material 继承体系 — parent / variant / 属性覆盖
        // ================================================================
        // 🎯 Material 支持父子继承链，实现属性的分层覆盖：
        //
        // 【parent 属性】
        //   获取/设置父材质。子材质继承父材质的所有属性，
        //   仅在覆盖（Override）的属性上使用自己的值。
        //
        // 【isVariant 属性】
        //   标记当前 Material 是否为材质变体（Variant）。
        //   变体质材是从父材质创建的子 Material。
        //
        // 【属性覆盖系统（Property Override）】
        //   - IsPropertyOverriden(nameID)：检查属性是否被覆盖
        //   - IsPropertyLocked(nameID)：检查属性是否被锁定（子级不可修改）
        //   - IsPropertyLockedByAncestor(nameID)：检查属性是否被祖先锁定
        //   - ApplyPropertyOverride(destination, nameID)：将属性覆盖应用到目标 Material
        //   - RevertPropertyOverride(nameID)：撤销覆盖，恢复父材质的值
        //   - SetPropertyLock(nameID, value)：锁定/解锁属性
        //   - RevertAllPropertyOverrides()：撤销所有覆盖
        //
        // 【IsChildOf】
        //   检查当前 Material 是否是指定祖先的子材质。
        //
        // 💡 典型使用场景：
        //   美术创建一个"基础金属"母材质，然后为不同变体创建子材质，
        //   仅覆盖颜色/纹理，其他属性（法线强度、UV 设置等）继承自母材质。
        //   这减少了材质管理的复杂度，修改母材质自动传播到所有变体。
        //
        // ⚠️ overrideCount / lockCount：只读属性，统计当前覆盖/锁定的属性数量。
        // ⚠️ allowLocking：控制此 Material 是否允许属性锁定功能。
        // ================================================================
        extern public Material parent { get; set; }
        extern public bool isVariant { [NativeName("IsVariant")] get; }

        extern internal int overrideCount { get; }
        extern internal int lockCount { get; }
        extern internal bool allowLocking { get; set; }

        [NativeName("IsChildOf")]          extern public bool IsChildOf([NotNull] Material ancestor);
        [NativeName("RevertAllOverrides")] extern public void RevertAllPropertyOverrides();

        public bool IsPropertyOverriden(int nameID)
        {
            GetPropertyState(nameID, out bool isOverriden, out _, out _);
            return isOverriden;
        }
        public bool IsPropertyLocked(int nameID)
        {
            GetPropertyState(nameID, out _, out bool isLockedInChildren, out _);
            return isLockedInChildren;
        }
        public bool IsPropertyLockedByAncestor(int nameID)
        {
            GetPropertyState(nameID, out _, out _, out bool isLockedByAncestor);
            return isLockedByAncestor;
        }

        public bool IsPropertyOverriden(string name)        => IsPropertyOverriden(Shader.PropertyToID(name));
        public bool IsPropertyLocked(string name)           => IsPropertyLocked(Shader.PropertyToID(name));
        public bool IsPropertyLockedByAncestor(string name) => IsPropertyLockedByAncestor(Shader.PropertyToID(name));

        // For MaterialProperty
        [NativeName("SetPropertyLock")]  extern public void SetPropertyLock(int nameID, bool value);
        [NativeName("ApplyOverride")]    extern public void ApplyPropertyOverride([NotNull] Material destination, int nameID, bool recordUndo = true);
        [NativeName("RevertOverride")]   extern public void RevertPropertyOverride(int nameID);
        [NativeName("GetPropertyState")] extern internal void GetPropertyState(int nameID, out bool isOverriden, out bool isLockedInChildren, out bool isLockedByAncestor);

        public void SetPropertyLock(string name, bool value)                                         => SetPropertyLock(Shader.PropertyToID(name), value);
        public void ApplyPropertyOverride(Material destination, string name, bool recordUndo = true) => ApplyPropertyOverride(destination, Shader.PropertyToID(name), recordUndo);
        public void RevertPropertyOverride(string name)                                              => RevertPropertyOverride(Shader.PropertyToID(name));

        // For MaterialSerializedProperty - bindings don't support overloads, so rename and use intermediate functions
        [NativeName("SetPropertyLock")]  extern private void SetPropertyLock_Serialized(MaterialSerializedProperty property, bool value);
        [NativeName("ApplyOverride")]    extern private void ApplyPropertyOverride_Serialized([NotNull] Material destination, MaterialSerializedProperty property, bool recordUndo = true);
        [NativeName("RevertOverride")]   extern private void RevertPropertyOverride_Serialized(MaterialSerializedProperty property);
        [NativeName("GetPropertyState")] extern private void GetPropertyState_Serialized(MaterialSerializedProperty property, out bool isOverriden, out bool isLockedInChildren, out bool isLockedByAncestor);

        internal void SetPropertyLock(MaterialSerializedProperty property, bool value) => SetPropertyLock_Serialized(property, value);
        internal void ApplyPropertyOverride(Material destination, MaterialSerializedProperty property, bool recordUndo = true) => ApplyPropertyOverride_Serialized(destination, property, recordUndo);
        internal void RevertPropertyOverride(MaterialSerializedProperty property) => RevertPropertyOverride_Serialized(property);
        internal void GetPropertyState(MaterialSerializedProperty propertyName, out bool isOverriden, out bool isLockedInChildren, out bool isLockedByAncestor)
            => GetPropertyState_Serialized(propertyName, out isOverriden, out isLockedInChildren, out isLockedByAncestor);

        // ================================================================
        // 内部工具方法
        // ================================================================
        // 🎯 RemoveUnusedProperties：清理不再使用的属性引用，优化内存。
        //   MarkChildrenNeedValidation：标记子材质需要重新验证（属性变更后）。
        //   这些是引擎内部方法，不应在用户代码中直接调用。
        // ================================================================
        // Clear stale references
        [NativeName("RemoveUnusedProperties")] extern internal void RemoveUnusedProperties();
        extern internal void MarkChildrenNeedValidation(string changedProperty);
    }
}
