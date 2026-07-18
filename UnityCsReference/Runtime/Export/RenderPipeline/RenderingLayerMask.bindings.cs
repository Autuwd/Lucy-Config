// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 RenderingLayerMask — 渲染层遮罩（32 位位掩码）
//
// 📌 作用：
//   在 LayerMask（GameObject 层级）之外的第二个筛选层，
//   专门用于渲染管线的细粒度控制。
//
// 📌 LayerMask vs RenderingLayerMask：
//   LayerMask：决定 GameObject 是否被摄像机看到（物理/渲染公共层）
//   RenderingLayerMask：在渲染管线中进一步筛选（仅渲染层）
//
// 💡 典型应用：
//   1. 延迟渲染中不同物体的 Decal 接收控制
//   2. 特定光源只照亮某些 RenderingLayer
//   3. 后处理效果只应用到特定层级的物体
//   4. SRP 中通过 FilteringSettings.renderingLayerMask 控制
//
// 📌 操作方式：
//   - RenderingLayerToName / NameToRenderingLayer — 层名转换
//   - GetMask("Layer1", "Layer2") — 从名称构造遮罩
//   - GetDefinedRenderingLayerCount / GetDefinedRenderingLayerNames — 枚举
//   - 默认值 = 1（仅最低位）
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [NativeHeader("Runtime/Graphics/RenderingLayerMask.h")]
    [NativeHeader("Runtime/BaseClasses/TagManager.h")]
    [NativeClass("RenderingLayerMask", "struct RenderingLayerMask;")]
    [Serializable]
    public struct RenderingLayerMask
    {
        [NativeName("m_Bits")] uint m_Bits;

        //TODO: Replace this with a proper default value transferring.
        //Using a Internal_GetDefaultRenderingLayerValue() raise an error on MonoBehaviours. It can't be used for field initialization.
        //It must match TagManager::kDefaultRenderingLayerMask.
        public static RenderingLayerMask defaultRenderingLayerMask { get; } = new() {m_Bits = 1u};

        internal const int maxRenderingLayerSize = 32;

        public static implicit operator uint(RenderingLayerMask mask)
        {
            return mask.m_Bits;
        }

        // implicitly converts an integer to a LayerMask
        public static implicit operator RenderingLayerMask(uint intVal)
        {
            RenderingLayerMask mask;
            mask.m_Bits = intVal;
            return mask;
        }

        public static implicit operator int(RenderingLayerMask mask)
        {
            return unchecked((int)mask.m_Bits);
        }

        // implicitly converts an integer to a LayerMask
        public static implicit operator RenderingLayerMask(int intVal)
        {
            RenderingLayerMask mask;
            mask.m_Bits = unchecked((uint)intVal);
            return mask;
        }

        // Converts a layer mask value to an integer value.
        public uint value
        {
            get => m_Bits;
            set => m_Bits = value;
        }

        [NativeMethod("GetDefaultRenderingLayerValue")]
        static extern uint Internal_GetDefaultRenderingLayerValue();

        // Given a layer number, returns the name of the layer as defined in either a Builtin or a User Layer in the [[wiki:class-TagManager|Tag Manager]]
        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        [NativeMethod("RenderingLayerToString")]
        public static extern string RenderingLayerToName(int layer);

        // Given a layer name, returns the layer index as defined by either a Builtin or a User Layer in the [[wiki:class-TagManager|Tag Manager]]
        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        [NativeMethod("StringToRenderingLayer")]
        public static extern int NameToRenderingLayer(string layerName);

        // Given a set of layer names, returns the equivalent layer mask for all of them.
        public static uint GetMask(params string[] renderingLayerNames)
        {
            if (renderingLayerNames == null)
                throw new ArgumentNullException(nameof(renderingLayerNames));

            uint mask = 0;
            for (var i = 0; i < renderingLayerNames.Length; i++)
            {
                var layer = NameToRenderingLayer(renderingLayerNames[i]);
                if (layer != -1)
                    mask |= 1u << layer;
            }

            return mask;
        }

        // Given a span of layer names, returns the equivalent layer mask for all of them.
        public static uint GetMask(ReadOnlySpan<string> renderingLayerNames)
        {
            if (renderingLayerNames == null)
                throw new ArgumentNullException(nameof(renderingLayerNames));

            uint mask = 0;
            foreach (var name in renderingLayerNames)
            {
                var layer = NameToRenderingLayer(name);
                if (layer != -1)
                    mask |= 1u << layer;
            }

            return mask;
        }

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        public static extern int GetDefinedRenderingLayerCount();

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        public static extern int GetLastDefinedRenderingLayerIndex();

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        public static extern uint GetDefinedRenderingLayersCombinedMaskValue();

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        public static extern string[] GetDefinedRenderingLayerNames();

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        public static extern int[] GetDefinedRenderingLayerValues();

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        public static extern int GetRenderingLayerCount();
    }
}
