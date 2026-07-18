// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 SortingLayer — 2D 渲染排序层
//
// 📌 作用：
//   控制 SpriteRenderer / ParticleSystem 等 2D 渲染器的绘制顺序
//
// 💡 关键概念：
//   - SortingLayer 是 struct，通过 int m_Id 标识
//   - 层之间有明确的绘制先后顺序（低值先绘，高值后绘）
//   - SortingLayer.layers: 获取项目中定义的所有层
//   - IDToName / NameToID: ID ↔ 名称互转
//   - GetLayerValueFromID / GetLayerValueFromName: 获取排序值
//   - IsValid(): 检查 ID 是否为有效层
//
// 🎯 与 Renderer.sortingOrder 配合：
//   SortingLayer 决定层间顺序，sortingOrder 决定层内顺序
// ==============================================================

using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine
{
    [NativeHeader("Runtime/BaseClasses/TagManager.h")]
    public struct SortingLayer
    {
        private int m_Id;

        public int id { get { return m_Id; } }

        public string name { get { return SortingLayer.IDToName(m_Id); } }

        public int value { get { return SortingLayer.GetLayerValueFromID(m_Id); } }

        public static SortingLayer[] layers
        {
            get
            {
                int[] ids = GetSortingLayerIDsInternal();
                SortingLayer[] layers = new SortingLayer[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    layers[i].m_Id = ids[i];
                }
                return layers;
            }
        }

        // Delegate for layer add/remove/changed events
        public delegate void LayerCallback(SortingLayer layer);
        internal delegate void LayerChangedCallback();

        public static LayerCallback onLayerAdded;
        public static LayerCallback onLayerRemoved;
        internal static LayerChangedCallback onLayerChanged;

        [FreeFunction("GetTagManager().GetSortingLayerIDs")]
        extern private static int[] GetSortingLayerIDsInternal();

        // Returns the final sorting value for the layer.
        [FreeFunction("GetTagManager().GetSortingLayerValueFromUniqueID")]
        public extern static int GetLayerValueFromID(int id);

        // Returns the final sorting value for the layer.
        [FreeFunction("GetTagManager().GetSortingLayerValueFromName")]
        public extern static int GetLayerValueFromName(string name);

        // Returns the unique id of the layer with name.
        [FreeFunction("GetTagManager().GetSortingLayerUniqueIDFromName")]
        public extern static int NameToID(string name);

        // Returns the name given the layer's id.
        [FreeFunction("GetTagManager().GetSortingLayerNameFromUniqueID")]
        public extern static string IDToName(int id);

        // Returns true if an id is valid layer id.
        [FreeFunction("GetTagManager().IsSortingLayerUniqueIDValid")]
        public extern static bool IsValid(int id);
    }
}
