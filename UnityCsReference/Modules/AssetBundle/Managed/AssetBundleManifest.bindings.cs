// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ============================================================
// AssetBundleManifest — AssetBundle 依赖清单
//
// AssetBundle 构建完成后生成的依赖信息对象。
// 用于查询所有 AssetBundle 的名称、Hash 值和依赖关系。
//
// 通过以下方式获取：
//   AssetBundle manifestBundle = AssetBundle.LoadFromFile(manifestPath);
//   AssetBundleManifest manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
//
// 核心功能：
//   - GetAllAssetBundles()        — 获取所有 AssetBundle 名称
//   - GetDirectDependencies()     — 获取直接依赖
//   - GetAllDependencies()        — 获取所有依赖（递归）
//   - GetAssetBundleHash()        — 获取包 Hash（用于增量更新检查）
//
// 依赖加载流程：
//   加载一个 AssetBundle 前，应递归加载其所有依赖包，
//   否则资源可能无法正确加载（依赖的 Unity 对象缺失引用）。
// ============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    /// <summary>
    /// AssetBundle 清单文件。
    /// 记录了所有 AssetBundle 的元数据，包括名称、Hash 和依赖关系。
    /// 用于运行时决定需要加载哪些依赖 AssetBundle。
    /// </summary>
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleManifest.h")]
    public class AssetBundleManifest : Object
    {
        private AssetBundleManifest() {}

        /// <summary>获取清单中记录的所有 AssetBundle 名称。</summary>
        [NativeMethod("GetAllAssetBundles")]
        public extern string[] GetAllAssetBundles();

        /// <summary>获取包含变体（Variant）的所有 AssetBundle 名称。</summary>
        [NativeMethod("GetAllAssetBundlesWithVariant")]
        public extern string[] GetAllAssetBundlesWithVariant();

        /// <summary>获取指定 AssetBundle 的 Hash128 值。用于增量更新时判断是否有变更。</summary>
        [NativeMethod("GetAssetBundleHash")]
        public extern Hash128 GetAssetBundleHash(string assetBundleName);

        /// <summary>获取指定 AssetBundle 的直接依赖列表（仅第一层依赖）。</summary>
        [NativeMethod("GetDirectDependencies")]
        public extern string[] GetDirectDependencies(string assetBundleName);

        /// <summary>获取指定 AssetBundle 的所有依赖列表（递归展开所有层级）。</summary>
        [NativeMethod("GetAllDependencies")]
        public extern string[] GetAllDependencies(string assetBundleName);
    }
}
