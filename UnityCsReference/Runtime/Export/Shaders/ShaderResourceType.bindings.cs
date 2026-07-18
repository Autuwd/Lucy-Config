// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 ShaderResourceType / ShaderResourceOptions — GPU 资源类型枚举
//
// 📌 作用：
//   定义 Shader 中可以绑定的 GPU 资源类型。
//   ShaderData 反射 API 使用此枚举描述 Shader 的资源需求。
//
// 📌 资源类型：
//   ConstantBuffer      → 常量缓冲区（cbuffer）
//   Buffer              → StructuredBuffer / ByteAddressBuffer
//   TypedBuffer         → Buffer<float4> 等类型化缓冲区
//   Texture             → 纹理资源
//   CombinedTextureSampler → 纹理+采样器组合
//   Sampler             → 单独的采样器状态
//   RayTracingAccelerationStructure → 光线追踪加速结构
//   InputTarget         → Framebuffer 输入（Subpass Input）
//
// 📌 访问权限：
//   ShaderResourceOptions.Readable = 只读（SRV）
//   ShaderResourceOptions.Writable = 可写（UAV）
// ==============================================================

using System;

using UnityEngine.Bindings;

namespace UnityEngine
{
namespace Shaders
{
    public enum ShaderResourceType
    {
        // ConstantBuffer<>
        ConstantBuffer,
        // StructuredBuffer<> or ByteAddressBuffer
        Buffer,
        // Buffer<>
        TypedBuffer,
        // Texture<>
        Texture,
        // Texture<> + SamplerState
        CombinedTextureSampler,
        // SamplerState
        Sampler,
        // RaytracingAccelerationStructure
        RayTracingAccelerationStructure,
        // Framebuffer input
        InputTarget,
    }

    [Flags]
    public enum ShaderResourceOptions
    {
        None = 0,

        Readable = 1 << 0,
        Writable = 1 << 1,
    }
} // namespace Shaders
} // namespace UnityEngine
