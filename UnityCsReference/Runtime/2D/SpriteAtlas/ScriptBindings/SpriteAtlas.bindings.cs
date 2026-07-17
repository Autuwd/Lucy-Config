// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// SpriteAtlas — Unity 2D 精灵图集系统
//
// 🎯 核心作用：
//   SpriteAtlas 将多个 Sprite 打包到一张或多张大纹理中，
//   减少 Draw Call 和纹理切换，提升 2D 渲染性能。
//
// 📌 图集打包流程：
//   1. 在编辑器中创建 SpriteAtlas 资源
//   2. 将 Sprite/纹理文件夹拖入 Atlas 的 Objects to Pack
//   3. 打包后，所有 Sprite 共享同一张图集纹理
//   4. 运行时通过 tag 或直接引用获取 Sprite
//
// 💡 关键概念：
//   - variant（变体图集）：继承自另一个 Atlas，可独立缩放
//   - SpriteAtlasManager：管理图集的请求和注册
//     - atlasRequested 事件：按 tag 按需加载图集
//     - atlasRegistered 事件：图集注册完成通知
//   - CanBindTo()：检查某 Sprite 是否属于该图集
//   - GetSprite(name)：从图集中按名称获取 Sprite
//
// ⚠️ 性能提示：
//   - 图集在首次使用时加载，建议使用 atlasRequested 实现懒加载
//   - 大图集占用更多内存，需要在 Draw Call 和内存间平衡
//   - 运行时可通过 SpriteAtlasManager.CreateSpriteAtlas 动态创建图集
//
// 📌 数据结构：
//   - ObjectData：打包对象信息（EntityId + 包装位置）
//   - TextureData：纹理信息（EntityId + 材质贴图名）
//   - AtlasPage：图集页面（包含打包对象数组和纹理数组）
//   - SpriteAtlasRuntimeConfig：运行时配置（缩放系数）
//
// 📍 对应 C++ 头文件：
//   Runtime/2D/SpriteAtlas/SpriteAtlasManager.h
//   Runtime/2D/SpriteAtlas/SpriteAtlas.h
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.U2D
{

    /// <summary>
    /// ObjectData holds information about a packed Object such as Sprite in the sprite atlas.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlasManager.h")]
    public struct ObjectData
    {
        /// <summary>
        /// Entity ID of the sprite.
        /// </summary>
        public EntityId     asset;

        /// <summary>
        /// Packing information for this object. X, Y coordinates of the packed position.
        /// </summary>
        public Vector4      packInfo;
    };

    /// <summary>
    /// TextureData holds information about a texture in the sprite atlas.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlasManager.h")]
    public struct TextureData
    {
        /// <summary>
        /// Entity ID of the packed texture
        /// </summary>
        public EntityId     texture;

        /// <summary>
        /// Name of the material map associated with this texture.
        /// </summary>
        public string       mapName;
    };

    /// <summary>
    /// AtlasPage holds information about a page in the sprite atlas. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlasManager.h")]
    public struct AtlasPage
    {
        /// <summary>
        /// Array of object data (packables) for this atlas page.
        /// </summary>
        public ObjectData[]     assets;

        /// <summary>
        /// Array of texture (packed textures) data for this atlas page.
        /// </summary>
        public TextureData[]    packedTextures;
    };

    /// <summary>
    /// Configuration settings for the sprite atlas at runtime.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]    
    public struct SpriteAtlasRuntimeConfig
    {
        /// <summary>
        /// Scale multiplier to be applied to the sprite.
        /// </summary>
        public float scaleMultiplier;
    }

    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlasManager.h")]
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlas.h")]
    [StaticAccessor("GetSpriteAtlasManager()", StaticAccessorType.Dot)]
    public class SpriteAtlasManager
    {
        public static event Action<string, Action<SpriteAtlas>> atlasRequested = null;

        [RequiredByNativeCode]
        private static bool RequestAtlas(string tag)
        {
            if (atlasRequested != null)
            {
                atlasRequested(tag, Register);
                return true;
            }
            return false;
        }

        public static event Action<SpriteAtlas> atlasRegistered = null;

        [RequiredByNativeCode]
        private static void PostRegisteredAtlas(SpriteAtlas spriteAtlas)
        {
            atlasRegistered?.Invoke(spriteAtlas);
        }

        extern internal static void Register(SpriteAtlas spriteAtlas);

        [FreeFunction("SpriteAtlasManager::CreateSpriteAtlas", ThrowsException = true)]
        extern private static SpriteAtlas CreateSpriteAtlas_Internal(string name, SpriteAtlasRuntimeConfig config, AtlasPage[] pages);
        public static SpriteAtlas CreateSpriteAtlas(string name, SpriteAtlasRuntimeConfig config, AtlasPage[] pages)
        {
            if(pages == null)
                throw new ArgumentException("No packing data has been provided.");
            return CreateSpriteAtlas_Internal(name, config, pages);
        }
    }

    [NativeHeader("Runtime/Graphics/SpriteFrame.h")]
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlas.h")]
    public class SpriteAtlas : UnityEngine.Object
    {
        public SpriteAtlas() { Internal_Create(this); }
        extern private static void Internal_Create([Writable] SpriteAtlas self);

        extern public bool isVariant {[NativeMethod("IsVariant")] get; }
        extern public string tag { get; }
        extern public int spriteCount { get; }

        extern public bool CanBindTo([NotNull] Sprite sprite);

        extern public Sprite GetSprite(string name);
        public int GetSprites(Sprite[] sprites) { return GetSpritesScripting(sprites); }
        public int GetSprites(Sprite[] sprites, string name) {  return GetSpritesWithNameScripting(sprites, name); }

        extern private int GetSpritesScripting([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Sprite[] sprites);
        extern private int GetSpritesWithNameScripting([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Sprite[] sprites, string name);
    }
}
