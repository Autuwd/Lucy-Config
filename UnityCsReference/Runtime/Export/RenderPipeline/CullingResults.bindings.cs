// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 CullingResults — 视锥体裁剪结果
//
// 📌 作用：
//   存储摄像机视锥体裁剪后的可见物体、光照和反射探针信息。
//   是 ScriptableRenderContext 中 DrawRenderers 的输入数据。
//
// 📌 核心数据：
//   ComputeVisibility() → 执行裁剪，填充 CullingResults
//   GetLightIndexCount / GetReflectionProbeIndexCount → 可见光源/探针数量
//   FillLightIndexMap / FillReflectionProbeIndexMap → 索引映射表
//
// 📌 间接绘制支持：
//   FillLightAndReflectionProbeIndices → 将光照索引写入 ComputeBuffer
//   可用于 Compute Shader 中的间接采样（如 Cluster Shading）。
//
// 📌 阴影计算：
//   GetShadowCasterBounds → 获取阴影投射体的包围盒
//   ComputeSpot/Point/DirectionalShadowMatricesAndCullingPrimitives →
//     计算阴影相机的投影/视图矩阵和裁剪参数
//
// 💡 visibleReflectionProbes — 可见反射探针列表
//   每个可见反射探针包含探针位置、影响范围、重要性等信息，
//   用于基于图像的光照（IBL）计算。
// ==============================================================

using System;
using Unity.Collections;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Export/RenderPipeline/ScriptableRenderPipeline.bindings.h")]
    [NativeHeader("Runtime/Graphics/ScriptableRenderLoop/ScriptableCulling.h")]
    [NativeHeader("Runtime/Scripting/ScriptingCommonStructDefinitions.h")]
    public partial struct CullingResults
    {
        [FreeFunction("ScriptableRenderPipeline_Bindings::GetLightIndexCount")]
        static extern int GetLightIndexCount(IntPtr cullingResultsPtr);

        [FreeFunction("ScriptableRenderPipeline_Bindings::GetReflectionProbeIndexCount")]
        static extern int GetReflectionProbeIndexCount(IntPtr cullingResultsPtr);

        [FreeFunction("FillLightAndReflectionProbeIndices")]
        static extern void FillLightAndReflectionProbeIndices(IntPtr cullingResultsPtr, ComputeBuffer computeBuffer);
        [FreeFunction("FillLightAndReflectionProbeIndices")]
        static extern void FillLightAndReflectionProbeIndicesGraphicsBuffer(IntPtr cullingResultsPtr, GraphicsBuffer buffer);

        [FreeFunction("GetLightIndexMapSize")]
        static extern int GetLightIndexMapSize(IntPtr cullingResultsPtr);

        [FreeFunction("GetReflectionProbeIndexMapSize")]
        static extern int GetReflectionProbeIndexMapSize(IntPtr cullingResultsPtr);

        [FreeFunction("FillLightIndexMapScriptable")]
        static extern void FillLightIndexMap(IntPtr cullingResultsPtr, IntPtr indexMapPtr, int indexMapSize);

        [FreeFunction("FillReflectionProbeIndexMapScriptable")]
        static extern void FillReflectionProbeIndexMap(IntPtr cullingResultsPtr, IntPtr indexMapPtr, int indexMapSize);

        [FreeFunction("SetLightIndexMapScriptable")]
        static extern void SetLightIndexMap(IntPtr cullingResultsPtr, IntPtr indexMapPtr, int indexMapSize);

        [FreeFunction("SetReflectionProbeIndexMapScriptable")]
        static extern void SetReflectionProbeIndexMap(IntPtr cullingResultsPtr, IntPtr indexMapPtr, int indexMapSize);

        [FreeFunction("ScriptableRenderPipeline_Bindings::GetShadowCasterBounds")]
        static extern bool GetShadowCasterBounds(IntPtr cullingResultsPtr, int lightIndex, out Bounds bounds);

        [FreeFunction("ScriptableRenderPipeline_Bindings::ComputeSpotShadowMatricesAndCullingPrimitives")]
        static extern bool ComputeSpotShadowMatricesAndCullingPrimitives(IntPtr cullingResultsPtr, int activeLightIndex,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);

        [FreeFunction("ScriptableRenderPipeline_Bindings::ComputePointShadowMatricesAndCullingPrimitives")]
        static extern bool ComputePointShadowMatricesAndCullingPrimitives(IntPtr cullingResultsPtr, int activeLightIndex,
            CubemapFace cubemapFace, float fovBias,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);

        [FreeFunction("ScriptableRenderPipeline_Bindings::ComputeDirectionalShadowMatricesAndCullingPrimitives")]
        static extern bool ComputeDirectionalShadowMatricesAndCullingPrimitives(IntPtr cullingResultsPtr, int activeLightIndex,
            int splitIndex, int splitCount, Vector3 splitRatio, int shadowResolution, float shadowNearPlaneOffset,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
    }
}
