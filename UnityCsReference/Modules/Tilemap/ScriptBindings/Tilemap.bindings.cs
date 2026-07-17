// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// Tilemap — Unity 2D 瓦片地图系统
//
// 🎯 核心作用：
//   Tilemap 是 Unity 2D 游戏中基于网格（Grid）的地图系统。
//   它将世界空间划分为规则的单元格（Cell），每个单元格可以放置
//   不同的 Tile（瓦片），用于快速构建 2D 关卡、地形和场景。
//
// 📌 核心概念：
//   - Grid（网格）：定义单元格的大小和形状（方形/六边形/等距）
//   - Tilemap：存储瓦片数据的组件，挂载在 Grid 子级上
//   - Tile（瓦片）：每个单元格的内容，包含 Sprite、颜色、变换等
//   - TilemapRenderer：实际执行瓦片渲染的组件
//   - TilemapCollider2D：为瓦片生成 2D 碰撞体
//
// 💡 瓦片操作：
//   - GetTile/SetTile：单个瓦片的读写
//   - GetTilesBlock/SetTilesBlock：按区域批量读写
//   - FloodFill：从指定位置开始的洪水填充
//   - BoxFill：矩形区域填充
//   - SwapTile：替换所有使用某瓦片的单元格
//
// ⚡ 高性能 API：
//   - TileArray / SpriteArray / PositionArray：非托管内存的数组结构
//   - 使用 NativeArray 进行批量设置，避免 GC 分配
//   - GetUsedTiles(Allocator)：获取所有已使用的瓦片（支持 Temp/Persistent）
//
// 🔑 Tile 属性系统：
//   - TileFlags：锁定颜色/变换、运行时实例化 GameObject 等标志
//   - TileAnimationFlags：动画循环、暂停、物理更新等标志
//   - TileChangeData：完整的瓦片变更数据（位置+瓦片+颜色+变换）
//   - TileAnimationData：瓦片动画配置（动画帧序列+速度+起始时间）
//
// 🏗 继承链：
//   Tilemap -> GridLayout -> Transform -> Component -> Object
//   TilemapRenderer -> Renderer -> Component -> Object
//   TilemapCollider2D -> Collider2D -> Collider -> Component -> Object
//
// 📍 对应 C++ 头文件：
//   Modules/Tilemap/Public/Tilemap.h
//   Modules/Tilemap/Public/TilemapRenderer.h
//   Modules/Grid/Public/Grid.h
// ==============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.U2D;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.Tilemaps
{
    // ==============================================================
    // TileFlags — 瓦片标志位枚举
    // ==============================================================
    // 控制瓦片在编辑器和运行时的行为：
    //   - LockColor：锁定瓦片颜色，编辑器中不允许修改
    //   - LockTransform：锁定瓦片变换矩阵
    //   - InstantiateGameObjectRuntimeOnly：运行时实例化 GameObject（编辑器中不显示）
    //   - KeepGameObjectRuntimeOnly：运行时保留已实例化的 GameObject
    //   - LockAll = LockColor | LockTransform：同时锁定颜色和变换
    // ==============================================================
    [Flags]
    {
        None = 0,
        LockColor = 1 << 0,
        LockTransform = 1 << 1,
        InstantiateGameObjectRuntimeOnly = 1 << 2,
        KeepGameObjectRuntimeOnly = 1 << 3,
        LockAll = LockColor | LockTransform,
    }

    // ==============================================================
    // TileAnimationFlags — 瓦片动画标志位枚举
    // ==============================================================
    // 控制瓦片动画的播放行为：
    //   - LoopOnce：播放一次后停止
    //   - PauseAnimation：暂停动画播放
    //   - UpdatePhysics：动画过程中更新物理碰撞体
    //   - UnscaledTime：使用不受 TimeScale 影响的时间
    //   - SyncAnimation：同步动画帧到同一 Tilemap 的其他瓦片
    // ==============================================================
    [Flags]
    {
        None = 0,
        LoopOnce = 1 << 0,
        PauseAnimation = 1 << 1,
        UpdatePhysics = 1 << 2,
        UnscaledTime = 1 << 3,
        SyncAnimation = 1 << 4,
    }

    // ==============================================================
    // Tilemap — 瓦片地图核心组件
    // ==============================================================
    // 🎯 基于网格的 2D 地图系统，继承自 GridLayout
    //
    // 📌 核心属性：
    //   origin：瓦片地图的原点（网格坐标）
    //   size：瓦片地图的尺寸（网格单位数）
    //   cellBounds：完整的单元格边界（由 origin 和 size 计算）
    //   localBounds：本地空间中的渲染边界
    //   orientation：瓦片放置方向（XY/XZ/YX/YZ/ZX/ZY/Custom）
    //   orientationMatrix：自定义方向变换矩阵
    //   tileAnchor：瓦片锚点比例（0~1，控制瓦片在单元格中的偏移）
    //   color：全局颜色叠加
    //   animationFrameRate：瓦片动画帧率
    //
    // 📌 瓦片存取：
    //   GetTile<T>(pos)：获取指定位置的瓦片（类型安全）
    //   SetTile(pos, tile)：设置单个瓦片
    //   GetTilesBlock(bounds)：获取区域内的所有瓦片
    //   SetTilesBlock(pos, tiles)：批量设置区域瓦片
    //   HasTile(pos)：检查是否有瓦片
    //
    // 📌 瓦片属性操作：
    //   GetColor/SetColor：瓦片颜色
    //   GetTransformMatrix/SetTransformMatrix：瓦片变换
    //   GetTileFlags/SetTileFlags：瓦片标志位
    //   GetColliderType/SetColliderType：瓦片碰撞类型
    //   GetSprite(pos)：获取瓦片对应的 Sprite
    //
    // 📌 编辑操作：
    //   FloodFill(pos, tile)：洪水填充
    //   BoxFill(pos, tile, ...)：矩形填充
    //   InsertCells/DeleteCells：插入/删除行列
    //   CompressBounds()：压缩边界到实际使用区域
    //   ClearAllTiles()：清除所有瓦片
    //
    // 📌 瓦片动画：
    //   GetAnimationFrame/SetAnimationFrame：动画帧控制
    //   GetAnimationTime/SetAnimationTime：动画时间控制
    //   GetTileAnimationFlags/SetTileAnimationFlags：动画标志
    // ==============================================================
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeHeader("Modules/Grid/Public/Grid.h")]
    [NativeHeader("Runtime/Graphics/SpriteFrame.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapTile.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapMarshalling.h")]
    [NativeHeader("Modules/Tilemap/Public/Tilemap.h")]
    public sealed partial class Tilemap : GridLayout
    {
        // ==============================================================
        // Orientation — 瓦片放置方向枚举
        // ==============================================================
        // 定义瓦片在 3D 空间中的放置平面：
        //   - XY：标准 2D 平面（最常用）
        //   - XZ：等距 3D 风格
        //   - YX/YZ/ZX/ZY：其他平面组合
        //   - Custom：使用 orientationMatrix 自定义
        // ==============================================================
        public enum Orientation
        {
            XY = 0,
            XZ = 1,
            YX = 2,
            YZ = 3,
            ZX = 4,
            ZY = 5,
            Custom = 6,
        }

        public extern Grid layoutGrid
        {
            [NativeMethod(Name = "GetAttachedGrid")]
            get;
        }

        public Vector3 GetCellCenterLocal(Vector3Int position) { return CellToLocalInterpolated(position) + CellToLocalInterpolated(tileAnchorRatio); }
        public Vector3 GetCellCenterWorld(Vector3Int position) { return LocalToWorld(GetCellCenterLocal(position)); }

        public BoundsInt cellBounds
        {
            get
            {
                return new BoundsInt(origin, size);
            }
        }

        [NativeProperty("TilemapBoundsScripting")]
        public extern Bounds localBounds
        {
            get;
        }

        [NativeProperty("TilemapFrameBoundsScripting")]
        internal extern Bounds localFrameBounds
        {
            get;
        }

        public extern float animationFrameRate
        {
            get;
            set;
        }

        public extern Color color
        {
            get;
            set;
        }
        public extern Vector3Int origin
        {
            get;
            set;
        }

        public extern Vector3Int size
        {
            get;
            set;
        }

        [NativeProperty(Name = "TileAnchorScripting")]
        public extern Vector3 tileAnchor
        {
            get;
            set;
        }

        [NativeProperty(Name = "TileAnchorRatioScripting")]
        internal extern Vector3 tileAnchorRatio
        {
            get;
        }

        public extern Orientation orientation
        {
            get;
            set;
        }

        public extern Matrix4x4 orientationMatrix
        {
            [NativeMethod(Name = "GetTileOrientationMatrix")]
            get;
            [NativeMethod(Name = "SetOrientationMatrix")]
            set;
        }

        internal extern Object GetTileAsset(Vector3Int position);
        public TileBase GetTile(Vector3Int position) { return GetTileAsset(position) as TileBase; }
        public T GetTile<T>(Vector3Int position) where T : TileBase { return GetTileAsset(position) as T; }

        [NativeMethod(Name = "GetTileAssetEntityId", IsThreadSafe = true)]
        public extern EntityId GetTileEntityId(Vector3Int position);

        internal extern IntPtr GetTilemapHandle();

        [NativeMethod(Name = "GetTileEntityIdFromHandle", IsThreadSafe = true)]
        internal static extern EntityId GetTileEntityIdFromHandle(IntPtr tilemapHandle, Vector3Int position);

        [NativeMethod(Name = "GetTileEntityIdsFromOffsets", IsThreadSafe = true)]
        private extern void GetTileEntityIdsFromOffsets(Vector3Int position, IntPtr offsetsIntrPtr, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetTileEntityIdsFromOffsetsAndHandle", IsThreadSafe = true)]
        private static extern void GetTileEntityIdsFromOffsetsAndHandle(IntPtr tilemapHandle, Vector3Int position, IntPtr offsetsIntrPtr, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetTileEntityIdsFromBlockOffset", IsThreadSafe = true)]
        private extern void GetTileEntityIdsFromBlockOffset(Vector3Int position, BoundsInt blockOffset, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetTileEntityIdsFromBlockOffsetAndHandle", IsThreadSafe = true)]
        private static extern void GetTileEntityIdsFromBlockOffsetAndHandle(IntPtr tilemapHandle, Vector3Int position, BoundsInt blockOffset, IntPtr tilesIntPtr, int count);

        internal extern Object[] GetTileAssetsBlock(Vector3Int position, Vector3Int blockDimensions);

        public TileBase[] GetTilesBlock(BoundsInt bounds)
        {
            var array = GetTileAssetsBlock(bounds.min, bounds.size);
            var tiles = new TileBase[array.Length];
            for (int i = 0; i < array.Length; ++i)
            {
                tiles[i] = (TileBase)array[i];
            }
            return tiles;
        }

        [FreeFunction(Name = "TilemapBindings::GetTileAssetsBlockNonAlloc", HasExplicitThis = true)]
        internal extern int GetTileAssetsBlockNonAlloc(Vector3Int startPosition, Vector3Int endPosition, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] tiles);

        public int GetTilesBlockNonAlloc(BoundsInt bounds, TileBase[] tiles)
        {
            return GetTileAssetsBlockNonAlloc(bounds.min, bounds.size, tiles);
        }

        public extern int GetTilesRangeCount(Vector3Int startPosition, Vector3Int endPosition);

        [FreeFunction(Name = "TilemapBindings::GetTileAssetsRangeNonAlloc", HasExplicitThis = true)]
        internal extern int GetTileAssetsRangeNonAlloc(Vector3Int startPosition, Vector3Int endPosition, Vector3Int[] positions, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] tiles);

        public int GetTilesRangeNonAlloc(Vector3Int startPosition, Vector3Int endPosition, Vector3Int[] positions, TileBase[] tiles)
        {
            return GetTileAssetsRangeNonAlloc(startPosition, endPosition, positions, tiles);
        }

        internal extern void SetTileAsset(Vector3Int position, Object tile);

        public void SetTile(Vector3Int position, TileBase tile) { SetTileAsset(position, tile); }

        internal extern void SetTileAssets(Vector3Int[] positionArray, Object[] tileArray);

        public void SetTiles(Vector3Int[] positionArray, TileBase[] tileArray) { SetTileAssets(positionArray, tileArray); }

        public void SetTiles(NativeArray<Vector3Int> positionArray, TileArray tileArray)
        {
            if (!positionArray.IsCreated
                || positionArray.Length != tileArray.Length)
                throw new ArgumentException("All NativeArrays must be created and have the same length as tileArray.");
            if (tileArray.Length == 0)
                return;
            unsafe
            {
                Internal_SetTileAssets(positionArray.m_Buffer, tileArray.buffer);
            }
        }

        [NativeMethod(Name = "SetTileAssetsBlock")]
        private extern void INTERNAL_CALL_SetTileAssetsBlock(Vector3Int position, Vector3Int blockDimensions, Object[] tileArray);
        public void SetTilesBlock(BoundsInt position, TileBase[] tileArray) { INTERNAL_CALL_SetTileAssetsBlock(position.min, position.size, tileArray); }

        public void SetTilesBlock(BoundsInt position, TileArray tileArray)
        {
            if (position.size.x * position.size.y * position.size.z != tileArray.Length)
                throw new ArgumentException("tileArray length must match the size of the bounds.");
            if (tileArray.Length == 0)
                return;
            Internal_SetTileAssetsBlock(position.min, position.size, tileArray.buffer);
        }

        [NativeMethod(Name = "SetTileChangeData")]
        public extern void SetTile(TileChangeData tileChangeData, bool ignoreLockFlags);
        [NativeMethod(Name = "SetTileChangeDataArray")]
        public extern void SetTiles(TileChangeData[] tileChangeDataArray, bool ignoreLockFlags);

        public void SetTiles(NativeArray<Vector3Int> positionArray, TileArray tileArray, NativeArray<Color> colorArray, NativeArray<Matrix4x4> transformArray, bool ignoreLockFlags)
        {
            if (!positionArray.IsCreated
                || !colorArray.IsCreated
                || !transformArray.IsCreated
                || positionArray.Length != tileArray.Length
                || tileArray.Length != colorArray.Length
                || colorArray.Length != transformArray.Length)
            {
                throw new ArgumentException("All NativeArrays must be created and have the same length as tileArray.");
            }
            if (tileArray.Length == 0)
                return;

            unsafe
            {
                Internal_SetTileChangeDataArray(positionArray.m_Buffer
                , tileArray.buffer
                , colorArray.m_Buffer
                , transformArray.m_Buffer
                , ignoreLockFlags);
            }
        }

        public bool HasTile(Vector3Int position)
        {
            return GetTileAsset(position) != null;
        }

        [NativeMethod(Name = "RefreshTileAsset")]
        public extern void RefreshTile(Vector3Int position);

        [FreeFunction(Name = "TilemapBindings::RefreshTileAssetsNative", HasExplicitThis = true)]
        internal extern unsafe void RefreshTilesNative(void* positions, int count, bool needSortRemoveDup);

        [NativeMethod(Name = "RefreshAllTileAssets")]
        public extern void RefreshAllTiles();

        internal extern void SwapTileAsset(Object changeTile, Object newTile);
        public void SwapTile(TileBase changeTile, TileBase newTile) { SwapTileAsset(changeTile, newTile); }

        internal extern bool ContainsTileAsset(Object tileAsset);
        public bool ContainsTile(TileBase tileAsset) { return ContainsTileAsset(tileAsset); }

        public extern int GetUsedTilesCount();

        public extern int GetUsedSpritesCount();

        public int GetUsedTilesNonAlloc(TileBase[] usedTiles)
        {
            return Internal_GetUsedTilesNonAlloc(usedTiles);
        }

        public int GetUsedSpritesNonAlloc(Sprite[] usedSprites)
        {
            return Internal_GetUsedSpritesNonAlloc(usedSprites);
        }

        [FreeFunction(Name = "TilemapBindings::GetUsedTilesNonAlloc", HasExplicitThis = true)]
        internal extern int Internal_GetUsedTilesNonAlloc([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] usedTiles);

        [FreeFunction(Name = "TilemapBindings::GetUsedSpritesNonAlloc", HasExplicitThis = true)]
        internal extern int Internal_GetUsedSpritesNonAlloc([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] usedSprites);

        public extern Sprite GetSprite(Vector3Int position);

        public extern Matrix4x4 GetTransformMatrix(Vector3Int position);
        public extern void SetTransformMatrix(Vector3Int position, Matrix4x4 transform);

        [NativeMethod(Name = "GetTileColor")]
        public extern Color GetColor(Vector3Int position);

        [NativeMethod(Name = "SetTileColor")]
        public extern void SetColor(Vector3Int position, Color color);

        public extern TileFlags GetTileFlags(Vector3Int position);
        public extern void SetTileFlags(Vector3Int position, TileFlags flags);
        public extern void AddTileFlags(Vector3Int position, TileFlags flags);
        public extern void RemoveTileFlags(Vector3Int position, TileFlags flags);

        [NativeMethod(Name = "GetTileInstantiatedObject")]
        public extern GameObject GetInstantiatedObject(Vector3Int position);

        [NativeMethod(Name = "GetTileObjectToInstantiate")]
        public extern GameObject GetObjectToInstantiate(Vector3Int position);

        [NativeMethod(Name = "SetTileColliderType")]
        public extern void SetColliderType(Vector3Int position, Tile.ColliderType colliderType);
        [NativeMethod(Name = "GetTileColliderType")]
        public extern Tile.ColliderType GetColliderType(Vector3Int position);

        [NativeMethod(Name = "GetTileAnimationFrameCount")]
        public extern int GetAnimationFrameCount(Vector3Int position);
        [NativeMethod(Name = "GetTileAnimationFrame")]
        public extern int GetAnimationFrame(Vector3Int position);
        [NativeMethod(Name = "SetTileAnimationFrame")]
        public extern void SetAnimationFrame(Vector3Int position, int frame);

        [NativeMethod(Name = "GetTileAnimationTime")]
        public extern float GetAnimationTime(Vector3Int position);
        [NativeMethod(Name = "SetTileAnimationTime")]
        public extern void SetAnimationTime(Vector3Int position, float time);

        public extern TileAnimationFlags GetTileAnimationFlags(Vector3Int position);
        public extern void SetTileAnimationFlags(Vector3Int position, TileAnimationFlags flags);
        public extern void AddTileAnimationFlags(Vector3Int position, TileAnimationFlags flags);
        public extern void RemoveTileAnimationFlags(Vector3Int position, TileAnimationFlags flags);

        public void FloodFill(Vector3Int position, TileBase tile)
        {
            FloodFillTileAsset(position, tile);
        }

        [NativeMethod(Name = "FloodFill")]
        private extern void FloodFillTileAsset(Vector3Int position, Object tile);

        public void BoxFill(Vector3Int position, TileBase tile, int startX, int startY, int endX, int endY)
        {
            BoxFillTileAsset(position, tile, startX, startY, endX, endY);
        }

        [NativeMethod(Name = "BoxFill")]
        private extern void BoxFillTileAsset(Vector3Int position, Object tile, int startX, int startY, int endX, int endY);

        public void InsertCells(Vector3Int position, Vector3Int insertCells)
        {
            InsertCells(position, insertCells.x, insertCells.y, insertCells.z);
        }

        public extern void InsertCells(Vector3Int position, int numColumns, int numRows, int numLayers);

        public void DeleteCells(Vector3Int position, Vector3Int deleteCells)
        {
            DeleteCells(position, deleteCells.x, deleteCells.y, deleteCells.z);
        }

        public extern void DeleteCells(Vector3Int position, int numColumns, int numRows, int numLayers);

        public extern void ClearAllTiles();
        public extern void ResizeBounds();

        [NativeMethod(Name = "CompressBounds")]
        private extern void CompressTilemapBounds(bool keepEditorPreview);

        public void CompressBounds() { CompressTilemapBounds(false); }

        internal void CompressBoundsKeepEditorPreview() { CompressTilemapBounds(true); }

        public extern Vector3Int editorPreviewOrigin
        {
            [NativeMethod(Name = "GetRenderOrigin")]
            get;
        }

        public extern Vector3Int editorPreviewSize
        {
            [NativeMethod(Name = "GetRenderSize")]
            get;
        }

        internal extern Object GetAnyTileAsset(Vector3Int position);
        internal TileBase GetAnyTile(Vector3Int position) { return GetAnyTileAsset(position) as TileBase; }
        internal T GetAnyTile<T>(Vector3Int position) where T : TileBase { return GetAnyTile(position) as T; }
        [NativeMethod(Name = "GetAnyTileAssetEntityId", IsThreadSafe = true)]
        internal extern EntityId GetAnyTileEntityId(Vector3Int position);

        [NativeMethod(Name = "GetAnyTileEntityIdFromHandle", IsThreadSafe = true)]
        internal static extern EntityId GetAnyTileEntityIdFromHandle(IntPtr tilemapHandle, Vector3Int position);

        [NativeMethod(Name = "GetAnyTileEntityIdsFromOffsets", IsThreadSafe = true)]
        private extern void GetAnyTileEntityIdsFromOffsets(Vector3Int position, IntPtr offsetsIntrPtr, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetAnyTileEntityIdsFromOffsetsAndHandle", IsThreadSafe = true)]
        private static extern void GetAnyTileEntityIdsFromOffsetsAndHandle(IntPtr tilemapHandle, Vector3Int position, IntPtr offsetsIntrPtr, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetAnyTileEntityIdsFromBlockOffset", IsThreadSafe = true)]
        private extern void GetAnyTileEntityIdsFromBlockOffset(Vector3Int position, BoundsInt blockOffset, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetAnyTileEntityIdsFromBlockOffsetAndHandle", IsThreadSafe = true)]
        private static extern void GetAnyTileEntityIdsFromBlockOffsetAndHandle(IntPtr tilemapHandle, Vector3Int position, BoundsInt blockOffset, IntPtr tilesIntPtr, int count);

        internal extern Object GetEditorPreviewTileAsset(Vector3Int position);
        public TileBase GetEditorPreviewTile(Vector3Int position) { return GetEditorPreviewTileAsset(position) as TileBase; }
        public T GetEditorPreviewTile<T>(Vector3Int position) where T : TileBase { return GetEditorPreviewTile(position) as T; }

        [NativeMethod(Name = "GetEditorPreviewTileAssetEntityId", IsThreadSafe = true)]
        public extern EntityId GetEditorPreviewTileEntityId(Vector3Int position);

        internal extern void SetEditorPreviewTileAsset(Vector3Int position, Object tile);
        public void SetEditorPreviewTile(Vector3Int position, TileBase tile) { SetEditorPreviewTileAsset(position, tile); }

        public bool HasEditorPreviewTile(Vector3Int position)
        {
            return GetEditorPreviewTileAsset(position) != null;
        }

        public extern Sprite GetEditorPreviewSprite(Vector3Int position);

        public extern Matrix4x4 GetEditorPreviewTransformMatrix(Vector3Int position);
        public extern void SetEditorPreviewTransformMatrix(Vector3Int position, Matrix4x4 transform);

        [NativeMethod(Name = "GetEditorPreviewTileColor")]
        public extern Color GetEditorPreviewColor(Vector3Int position);
        [NativeMethod(Name = "SetEditorPreviewTileColor")]
        public extern void SetEditorPreviewColor(Vector3Int position, Color color);

        public extern TileFlags GetEditorPreviewTileFlags(Vector3Int position);

        public void EditorPreviewFloodFill(Vector3Int position, TileBase tile)
        {
            EditorPreviewFloodFillTileAsset(position, tile);
        }

        [NativeMethod(Name = "EditorPreviewFloodFill")]
        private extern void EditorPreviewFloodFillTileAsset(Vector3Int position, Object tile);

        public void EditorPreviewBoxFill(Vector3Int position, Object tile, int startX, int startY, int endX, int endY)
        {
            EditorPreviewBoxFillTileAsset(position, tile, startX, startY, endX, endY);
        }

        [NativeMethod(Name = "EditorPreviewBoxFill")]
        private extern void EditorPreviewBoxFillTileAsset(Vector3Int position, Object tile, int startX, int startY, int endX, int endY);

        [NativeMethod(Name = "ClearAllEditorPreviewTileAssets")]
        public extern void ClearAllEditorPreviewTiles();

        [RequiredByNativeCode]
        private ITilemap GetITilemapProxy()
        {
            return ITilemap.CreateInstanceFromTilemap(this);
        }

        [RequiredByNativeCode]
        internal void GetLoopEndedForTileAnimationCallbackSettings(ref bool hasEndLoopForTileAnimationCallback)
        {
            hasEndLoopForTileAnimationCallback = HasLoopEndedForTileAnimationCallback();
        }

        [RequiredByNativeCode]
        private void DoLoopEndedForTileAnimationCallback(int count, IntPtr positionsIntPtr)
        {
            HandleLoopEndedForTileAnimationCallback(count, positionsIntPtr);
        }

        [RequiredByNativeCode]
        public struct SyncTile
        {
            internal Vector3Int m_Position;
            internal TileBase m_Tile;
            internal TileData m_TileData;

            public Vector3Int position
            {
                get { return m_Position; }
            }

            public TileBase tile
            {
                get { return m_Tile; }
            }

            public TileData tileData
            {
                get { return m_TileData; }
            }

            [RequiredByNativeCode]
            internal static void ReconstructArrayElementRaw(SyncTile[] array, int index, TileBase tile, Vector3Int position, TileData tileData)
            {
                ref SyncTile tmp = ref array[index];
                tmp.m_Tile = tile;
                tmp.m_Position = position;
                tmp.m_TileData = tileData;
            }
        }

        internal struct SyncTileCallbackSettings
        {
            internal bool hasSyncTileCallback;
            internal bool hasPositionsChangedCallback;
            internal bool isBufferSyncTile;
        }

        [RequiredByNativeCode]
        internal void GetSyncTileCallbackSettings(ref SyncTileCallbackSettings settings)
        {
            settings.hasSyncTileCallback = HasSyncTileCallback();
            settings.hasPositionsChangedCallback = HasPositionsChangedCallback();
            settings.isBufferSyncTile = bufferSyncTile;
        }

        internal extern void SendAndClearSyncTileBuffer();

        [RequiredByNativeCode]
        private void DoSyncTileCallback(SyncTile[] syncTiles)
        {
            HandleSyncTileCallback(syncTiles);
        }

        [RequiredByNativeCode]
        private void DoPositionsChangedCallback(int count, IntPtr positionsIntPtr)
        {
            HandlePositionsChangedCallback(count, positionsIntPtr);
        }

        #region Non Allocating Getters

        [StructLayout(LayoutKind.Sequential)]
        internal struct TilemapBuffer : IDisposable
        {
            public readonly IntPtr buffer => m_Buffer;
            public readonly int length => m_Length;
            public readonly Allocator allocator => m_Allocator;

            public TilemapBuffer()
            {
                m_Buffer = IntPtr.Zero;
                m_Length = 0;
                m_Allocator = Allocator.None;
            }

            public unsafe TilemapBuffer(IntPtr buffer, int length)
            {
                m_Buffer = buffer;
                m_Length = length;
                m_Allocator = Allocator.None;
            }

            public unsafe readonly T AsEngineObject<T>(int index) where T : class
            {
                if (index < 0 || index >= m_Length)
                    throw new ArgumentOutOfRangeException("index");

                var entityId = UnsafeUtility.ArrayElementAsRef<EntityId>(m_Buffer.ToPointer(), index);
                return Resources.EntityIdIsValid(entityId) ? Resources.EntityIdToObject(entityId) as T : null;
            }

            public unsafe readonly T As<T>(int index) where T : struct
            {
                if (index < 0 || index >= m_Length)
                    throw new ArgumentOutOfRangeException("index");

                return UnsafeUtility.ArrayElementAsRef<T>(m_Buffer.ToPointer(), index);
            }

            public unsafe void SetEngineObject<T>(T value, int index) where T : UnityEngine.Object
            {
                if (index < 0 || index >= m_Length)
                    throw new ArgumentOutOfRangeException("index");

                var entityId = EntityId.None;
                if (value != null)
                    entityId = value.GetEntityId();
                UnsafeUtility.ArrayElementAsRef<EntityId>(m_Buffer.ToPointer(), index) = entityId;
            }

            public unsafe void SetValue<T>(T value, int index) where T : struct
            {
                if (index < 0 || index >= m_Length)
                    throw new ArgumentOutOfRangeException("index");

                UnsafeUtility.ArrayElementAsRef<T>(m_Buffer.ToPointer(), index) = value;
            }

            public unsafe void Dispose()
            {
                if (m_Buffer == null || m_Length == 0)
                    return;

                // Free the allocation.
                if (m_Allocator != Allocator.None)
                    UnsafeUtility.FreeTracked(m_Buffer.ToPointer(), m_Allocator);

                m_Buffer = IntPtr.Zero;
                m_Length = 0;
                m_Allocator = Allocator.None;
            }

            #region Internal

            IntPtr m_Buffer;
            int m_Length;
            Allocator m_Allocator;

            #endregion
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TileArray : IEnumerable<TileBase>, IDisposable
        {
            internal struct TileArrayEnumerator : IEnumerator<TileBase>
            {
                TileArray m_TileArray;
                int m_Index;

                public TileArrayEnumerator(TileArray tileArray)
                {
                    m_TileArray = tileArray;
                    m_Index = -1;
                }

                TileBase IEnumerator<TileBase>.Current => m_TileArray[m_Index];

                object IEnumerator.Current => m_TileArray[m_Index];

                void IDisposable.Dispose()
                {
                    // Does not own the buffer, so nothing to dispose
                }

                bool IEnumerator.MoveNext()
                {
                    if (m_TileArray.Length == 0)
                        return false;

                    return ++m_Index < m_TileArray.Length;
                }

                void IEnumerator.Reset()
                {
                    m_Index = -1;
                }
            }

            public TileArray(int length, Allocator allocator)
            {
                if (allocator != Allocator.Temp && allocator != Allocator.Persistent && allocator != Allocator.Domain)
                    throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);

                if (length <= 0)
                {
                    m_TilemapBuffer = default;
                    m_Allocator = Allocator.None;
                    m_MemoryLabel = default;
                    return;
                }

                unsafe
                {
                    int size = UnsafeUtility.SizeOf(typeof(EntityId));
                    var buffer = UnsafeUtility.MallocTracked(length * size, size, allocator, 1);
                    UnsafeUtility.MemClear(buffer, length * size);
                    m_TilemapBuffer = new TilemapBuffer((IntPtr)buffer, length);
                }
                m_Allocator = allocator;
                m_MemoryLabel = default;
            }

            public TileArray(int length, MemoryLabel memoryLabel)
            {
                if (length <= 0)
                {
                    m_TilemapBuffer = default;
                    m_Allocator = Allocator.None;
                    m_MemoryLabel = default;
                    return;
                }

                unsafe
                {
                    int size = UnsafeUtility.SizeOf(typeof(EntityId));
                    var buffer = UnsafeUtility.MallocTracked(length * size, size, memoryLabel, 1);
                    UnsafeUtility.MemClear(buffer, length * size);
                    m_TilemapBuffer = new TilemapBuffer((IntPtr) buffer, length);
                }
                m_Allocator = Allocator.None;
                m_MemoryLabel = memoryLabel;
            }

            internal TileArray(TilemapBuffer tilemapBuffer)
            {
                m_TilemapBuffer = tilemapBuffer;
                m_Allocator = Allocator.None;
                m_MemoryLabel = default;
            }

            public int Length => m_TilemapBuffer.length;

            public TileBase this[int index]
            {
                get => m_TilemapBuffer.AsEngineObject<TileBase>(index);
                set
                {
                    m_TilemapBuffer.SetEngineObject<TileBase>(value, index);
                }
            }

            #region Enumeration

            public readonly IEnumerator<TileBase> GetEnumerator() => new TileArrayEnumerator(this);
            readonly IEnumerator IEnumerable.GetEnumerator() => new TileArrayEnumerator(this);

            public void Dispose()
            {
                if (m_MemoryLabel.IsCreated)
                {
                    unsafe
                    {
                        UnsafeUtility.FreeTracked((void*)m_TilemapBuffer.buffer, m_MemoryLabel);
                    }
                }
                else if (m_Allocator != Allocator.None)
                {
                    unsafe
                    {
                        UnsafeUtility.FreeTracked((void*)m_TilemapBuffer.buffer, m_Allocator);
                    }
                }
                m_TilemapBuffer.Dispose();
            }

            #endregion

            #region Internal

            TilemapBuffer m_TilemapBuffer;
            Allocator m_Allocator;
            MemoryLabel m_MemoryLabel;

            internal readonly TilemapBuffer buffer => m_TilemapBuffer;

            #endregion
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SpriteArray : IEnumerable<Sprite>, IDisposable
        {
            internal struct SpriteArrayEnumerator : IEnumerator<Sprite>
            {
                SpriteArray m_SpriteArray;
                int m_Index;

                public SpriteArrayEnumerator(SpriteArray spriteArray)
                {
                    m_SpriteArray = spriteArray;
                    m_Index = -1;
                }

                Sprite IEnumerator<Sprite>.Current => m_SpriteArray[m_Index];

                object IEnumerator.Current => m_SpriteArray[m_Index];

                void IDisposable.Dispose()
                {
                    // Does not own the buffer, so nothing to dispose
                }

                bool IEnumerator.MoveNext()
                {
                    if (m_SpriteArray.Length == 0)
                        return false;

                    return ++m_Index < m_SpriteArray.Length;
                }

                void IEnumerator.Reset()
                {
                    m_Index = -1;
                }
            }

            internal SpriteArray(TilemapBuffer tilemapBuffer)
            {
                m_TilemapBuffer = tilemapBuffer;
            }

            public readonly int Length => m_TilemapBuffer.length;
            public readonly Sprite this[int index] => m_TilemapBuffer.AsEngineObject<Sprite>(index);

            #region Enumeration

            public readonly IEnumerator<Sprite> GetEnumerator() => new SpriteArrayEnumerator(this);
            readonly IEnumerator IEnumerable.GetEnumerator() => new SpriteArrayEnumerator(this);

            public void Dispose() => m_TilemapBuffer.Dispose();

            #endregion

            #region Internal

            TilemapBuffer m_TilemapBuffer;

            #endregion
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PositionArray : IEnumerable<Vector3Int>, IDisposable
        {
            internal struct PositionArrayEnumerator : IEnumerator<Vector3Int>
            {
                PositionArray m_PositionArray;
                int m_Index;

                public PositionArrayEnumerator(PositionArray positionArray)
                {
                    m_PositionArray = positionArray;
                    m_Index = -1;
                }

                Vector3Int IEnumerator<Vector3Int>.Current => m_PositionArray[m_Index];

                object IEnumerator.Current => m_PositionArray[m_Index];

                void IDisposable.Dispose()
                {
                    // Does not own the buffer, so nothing to dispose
                }

                bool IEnumerator.MoveNext()
                {
                    if (m_PositionArray.Length == 0)
                        return false;

                    return ++m_Index < m_PositionArray.Length;
                }

                void IEnumerator.Reset()
                {
                    m_Index = -1;
                }
            }

            internal PositionArray(TilemapBuffer tilemapBuffer)
            {
                m_TilemapBuffer = tilemapBuffer;
            }

            public readonly int Length => m_TilemapBuffer.length;
            public readonly Vector3Int this[int index] => m_TilemapBuffer.As<Vector3Int>(index);

            #region Enumeration

            public readonly IEnumerator<Vector3Int> GetEnumerator() => new PositionArrayEnumerator(this);
            readonly IEnumerator IEnumerable.GetEnumerator() => new PositionArrayEnumerator(this);
            public void Dispose() => m_TilemapBuffer.Dispose();

            #endregion

            #region Internal

            TilemapBuffer m_TilemapBuffer;

            #endregion
        }

        private const string k_TilemapAllocationArgumentExceptionMessage = "Allocator must be 'Temp', 'Domain' or `Persistent`";

        public TileArray GetUsedTiles(Allocator allocator = Allocator.Temp)
        {
            if (allocator == Allocator.Temp || allocator == Allocator.Persistent || allocator == Allocator.Domain)
                return new(Internal_GetUsedTiles(allocator, IntPtr.Zero));

            throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);
        }

        public TileArray GetUsedTiles(MemoryLabel memoryLabel)
        {
            // Memory Label Allocators must be Persistent or Domain
            return new(Internal_GetUsedTiles_MemoryLabel(memoryLabel));
        }

        public SpriteArray GetUsedSprites(Allocator allocator = Allocator.Temp)
        {
            if (allocator == Allocator.Temp || allocator == Allocator.Persistent || allocator == Allocator.Domain)
                return new(Internal_GetUsedSprites(allocator, IntPtr.Zero));

            throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);
        }

        public SpriteArray GetUsedSprites(MemoryLabel memoryLabel)
        {
            // Memory Label Allocators are Persistent or Domain
            return new(Internal_GetUsedSprites_MemoryLabel(memoryLabel));
        }

        public TileArray GetTiles(BoundsInt bounds, Allocator allocator = Allocator.Temp)
        {
            if (allocator == Allocator.Temp || allocator == Allocator.Persistent || allocator == Allocator.Domain)
                return new(Internal_GetTiles(bounds.min, bounds.size, allocator, IntPtr.Zero));

            throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);
        }

        public TileArray GetTiles(BoundsInt bounds, MemoryLabel memoryLabel)
        {
            // Memory Label Allocators are Persistent or Domain
            return new(Internal_GetTiles_MemoryLabel(bounds.min, bounds.size, memoryLabel));
        }

        public int GetTiles(BoundsInt bounds, out PositionArray positions, out TileArray tiles, Allocator allocator = Allocator.Temp, bool withinBounds = true)
        {
            if (allocator == Allocator.Temp || allocator == Allocator.Persistent || allocator == Allocator.Domain)
            {
                var positionsBuffer = new TilemapBuffer();
                var tilesBuffer = new TilemapBuffer();
                var length = Internal_GetTilePositions(bounds.min, bounds.max, ref positionsBuffer, ref tilesBuffer, withinBounds ? 1 : 0, allocator, IntPtr.Zero);

                positions = new(positionsBuffer);
                tiles = new(tilesBuffer);

                return length;
            }

            throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);
        }

        public int GetTiles(BoundsInt bounds, out PositionArray positions, out TileArray tiles, MemoryLabel memoryLabel, bool withinBounds = true)
        {
            var positionsBuffer = new TilemapBuffer();
            var tilesBuffer = new TilemapBuffer();

            // Memory Label Allocators are Persistent or Domain
            var length = Internal_GetTilePositions_MemoryLabel(bounds.min, bounds.max, ref positionsBuffer, ref tilesBuffer, withinBounds ? 1 : 0, memoryLabel);

            positions = new(positionsBuffer);
            tiles = new(tilesBuffer);

            return length;
        }

        [FreeFunction(Name = "TilemapBindings::GetUsedTiles", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetUsedTiles(Allocator allocator, IntPtr memLabelPtr);
        [FreeFunction(Name = "TilemapBindings::GetUsedTiles_MemoryLabel", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetUsedTiles_MemoryLabel(MemoryLabel memoryLabel);

        [FreeFunction(Name = "TilemapBindings::GetUsedSprites", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetUsedSprites(Allocator allocator, IntPtr memLabelPtr);
        [FreeFunction(Name = "TilemapBindings::GetUsedSprites_MemoryLabel", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetUsedSprites_MemoryLabel(MemoryLabel memoryLabel);

        [FreeFunction(Name = "TilemapBindings::GetTiles", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetTiles(Vector3Int startPosition, Vector3Int blockDimensions, Allocator allocator, IntPtr memLabelPtr);

        [FreeFunction(Name = "TilemapBindings::GetTiles_MemoryLabel", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetTiles_MemoryLabel(Vector3Int startPosition, Vector3Int blockDimensions, MemoryLabel memoryLabel);

        [FreeFunction(Name = "TilemapBindings::GetTilePositions", HasExplicitThis = true)]
        extern int Internal_GetTilePositions(Vector3Int startPosition, Vector3Int endPosition, ref TilemapBuffer positions, ref TilemapBuffer tiles, int withinBounds, Allocator allocator, IntPtr memLabelPtr);
        [FreeFunction(Name = "TilemapBindings::GetTilePositions_MemoryLabel", HasExplicitThis = true)]
        extern int Internal_GetTilePositions_MemoryLabel(Vector3Int startPosition, Vector3Int endPosition, ref TilemapBuffer positions, ref TilemapBuffer tiles, int withinBounds, MemoryLabel memoryLabel);

        [FreeFunction(Name = "TilemapBindings::SetTileAssets", HasExplicitThis = true)]
        extern unsafe void Internal_SetTileAssets(void* positionArray, TilemapBuffer tileArray);

        [FreeFunction(Name = "TilemapBindings::SetTileAssetsBlock", HasExplicitThis = true)]
        extern void Internal_SetTileAssetsBlock(Vector3Int position, Vector3Int size, TilemapBuffer tileArray);

        [FreeFunction(Name = "TilemapBindings::SetTileChangeDataArray", HasExplicitThis = true)]
        extern unsafe void Internal_SetTileChangeDataArray(void* positionPtr, TilemapBuffer tileArray, void* colorPtr, void* transformPtr, bool ignoreLockFlags);

        #endregion
    }

    // ==============================================================
    // TilemapRenderer — 瓦片地图渲染器
    // ==============================================================
    // 🎯 负责将 Tilemap 中的瓦片实际渲染到屏幕上
    //
    // 📌 渲染模式（Mode）：
    //   - Chunk：按块合并渲染（默认，性能最好）
    //   - Individual：逐瓦片独立渲染（方便调试但 Draw Call 多）
    //   - SRPBatch：SRP 批处理模式（URP/HDRP 推荐）
    //
    // 📌 排序顺序（SortOrder）：
    //   控制同一块内瓦片的渲染顺序：
    //   - BottomLeft：从左下角开始
    //   - BottomRight：从右下角开始
    //   - TopLeft：从左上角开始
    //   - TopRight：从右上角开始
    //
    // 📌 关键属性：
    //   chunkSize：每个渲染块的大小（默认 16x16）
    //   chunkCullingBounds：块裁剪的额外边界
    //   maskInteraction：遮罩交互模式
    //   detectChunkCullingBounds：自动/手动检测裁剪边界
    //
    // ⚡ 性能优化：
    //   maxChunkCount：最大同时渲染的块数
    //   maxFrameAge：块的最大过期帧数（影响更新频率）
    // ==============================================================
    [RequireComponent(typeof(Tilemap))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeHeader("Modules/Tilemap/TilemapRendererJobs.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapMarshalling.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapRenderer.h")]
    public sealed partial class TilemapRenderer : Renderer
    {
        public enum SortOrder
        {
            BottomLeft = 0,
            BottomRight = 1,
            TopLeft = 2,
            TopRight = 3,
        }

        public enum Mode
        {
            Chunk = 0,
            Individual = 1,
            SRPBatch = 2,
        }

        public enum DetectChunkCullingBounds
        {
            Auto = 0,
            Manual = 1,
        }

        public extern Vector3Int chunkSize
        {
            get;
            set;
        }

        public extern Vector3 chunkCullingBounds
        {
            [FreeFunction("TilemapRendererBindings::GetChunkCullingBounds", HasExplicitThis = true)]
            get;
            [FreeFunction("TilemapRendererBindings::SetChunkCullingBounds", HasExplicitThis = true)]
            set;
        }

        public extern int maxChunkCount
        {
            get;
            set;
        }

        public extern int maxFrameAge
        {
            get;
            set;
        }

        public extern SortOrder sortOrder
        {
            get;
            set;
        }

        [NativeProperty("RenderMode")]
        public extern Mode mode
        {
            get;
            set;
        }

        public extern DetectChunkCullingBounds detectChunkCullingBounds
        {
            get;
            set;
        }

        public extern SpriteMaskInteraction maskInteraction
        {
            get;
            set;
        }

        [RequiredByNativeCode]
        internal void RegisterSpriteAtlasRegistered()
        {
            SpriteAtlasManager.atlasRegistered += OnSpriteAtlasRegistered;
        }

        [RequiredByNativeCode]
        internal void UnregisterSpriteAtlasRegistered()
        {
            SpriteAtlasManager.atlasRegistered -= OnSpriteAtlasRegistered;
        }

        internal extern void OnSpriteAtlasRegistered(SpriteAtlas atlas);

        [FreeFunction(Name = "TilemapRendererBindings::SetShaderUserValue", HasExplicitThis = true)] extern internal void Internal_SetShaderUserValueUInt(UInt32 v);
        public void SetShaderUserValue(UInt32 v) => Internal_SetShaderUserValueUInt(v);
        [FreeFunction(Name = "TilemapRendererBindings::GetShaderUserValue", HasExplicitThis = true)] extern internal UInt32 Internal_GetShaderUserValueUInt();
        public UInt32 GetShaderUserValue() { return Internal_GetShaderUserValueUInt(); }
    }

    // ==============================================================
    // TileData — 瓦片数据结构
    // ==============================================================
    // 🎯 描述一个瓦片单元格的完整渲染数据
    //
    // 📌 字段：
    //   sprite：瓦片使用的精灵
    //   color：瓦片颜色叠加（Color.white = 不着色）
    //   transform：瓦片变换矩阵（缩放/旋转/平移）
    //   gameObject：瓦片关联的 GameObject
    //   flags：瓦片标志位（锁定颜色/变换等）
    //   colliderType：碰撞类型（None/Grid/Sprite）
    //
    // 💡 与 TileBase 的区别：
    //   TileBase 是资产（Asset），TileData 是运行时数据（Data）。
    //   TileBase.GetTileData() 方法填充 TileData 结构体。
    // ==============================================================
    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeHeader("Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileData
    {
        public Sprite sprite { get { return Object.ForceLoadFromInstanceID(m_Sprite) as Sprite; } set { m_Sprite = value != null ? value.GetEntityId() : EntityId.None; } }
        public EntityId spriteEntityId { get => m_Sprite; set => m_Sprite = value; }
        public Color color { get { return m_Color; } set { m_Color = value; } }
        public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }
        public GameObject gameObject { get { return Object.ForceLoadFromInstanceID(m_GameObject) as GameObject; } set { m_GameObject = value != null ? value.GetEntityId() : EntityId.None; } }
        public EntityId gameObjectEntityId { get => m_GameObject; set => m_GameObject = value; }
        public TileFlags flags { get { return m_Flags; } set { m_Flags = value; } }
        public Tile.ColliderType colliderType { get { return m_ColliderType; } set { m_ColliderType = value; } }

        private EntityId m_Sprite;
        private Color m_Color;
        private Matrix4x4 m_Transform;
        private EntityId m_GameObject;
        private TileFlags m_Flags;
        private Tile.ColliderType m_ColliderType;

        internal static readonly TileData Default = CreateDefault();
        private static TileData CreateDefault()
        {
            TileData tileData = default;
            tileData.m_Sprite = EntityId.None;
            tileData.m_Color = Color.white;
            tileData.m_Transform = Matrix4x4.identity;
            tileData.m_GameObject = EntityId.None;
            tileData.m_Flags = default;
            tileData.m_ColliderType = default;
            return tileData;
        }
    }

    // ==============================================================
    // TileChangeData — 瓦片变更数据结构
    // ==============================================================
    // 🎯 用于批量设置瓦片时传递完整变更信息
    //
    // 📌 字段：
    //   position：变更位置（网格坐标 Vector3Int）
    //   tile：目标瓦片资产（TileBase）
    //   color：颜色
    //   transform：变换矩阵
    //
    // 💡 使用场景：
    //   SetTile(TileChangeData, ignoreLockFlags) 允许一次性设置
    //   位置、瓦片、颜色和变换，且可选择忽略锁定标志。
    //   比分别调用 SetTile/SetColor/SetTransformMatrix 更高效。
    // ==============================================================
    [Serializable]
    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeHeader("Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileChangeData
    {
        public Vector3Int position { get { return m_Position; } set { m_Position = value; } }
        public TileBase tile { get { return m_TileAsset as TileBase; } set { m_TileAsset = value; } }
        public Color color { get { return m_Color; } set { m_Color = value; } }
        public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }

        [SerializeField]
        private Vector3Int m_Position;
        [SerializeField]
        private Object m_TileAsset;
        [SerializeField]
        private Color m_Color;
        [SerializeField]
        private Matrix4x4 m_Transform;

        public TileChangeData(Vector3Int position, TileBase tile, Color color, Matrix4x4 transform)
        {
            m_Position = position;
            m_TileAsset = tile;
            m_Color = color;
            m_Transform = transform;
        }
    }

    // ==============================================================
    // TileAnimationData — 瓦片动画数据结构
    // ==============================================================
    // 🎯 定义瓦片的动画帧序列和播放参数
    //
    // 📌 字段：
    //   animatedSprites：动画帧序列（Sprite 数组）
    //   animationSpeed：播放速度倍率
    //   animationStartTime：动画起始时间偏移
    //   flags：动画标志（循环/暂停/物理更新等）
    //
    // 💡 运行时行为：
    //   Tilemap 引擎在 Update 中根据 animationFrameRate
    //   推进每帧动画，并通过 RefreshTile 更新显示。
    //   SyncAnimation 标志让同一 Tilemap 的瓦片同步动画帧。
    // ==============================================================
    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeHeader("Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileAnimationData
    {
        public Sprite[] animatedSprites { get { return m_AnimatedSprites; } set { m_AnimatedSprites = value; } }
        public float animationSpeed { get { return m_AnimationSpeed; } set { m_AnimationSpeed = value; } }
        public float animationStartTime { get { return m_AnimationStartTime; } set { m_AnimationStartTime = value; } }
        public TileAnimationFlags flags { get { return m_Flags; } set { m_Flags = value; } }

        private Sprite[] m_AnimatedSprites;
        private float m_AnimationSpeed;
        private float m_AnimationStartTime;
        private TileAnimationFlags m_Flags;
    }

    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeHeader("Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileAnimationEntityIdData
    {
        public NativeArray<EntityId> animatedSpritesEntityIds
        {
            set
            {
                if (!value.IsCreated)
                    return;
                unsafe
                {
                    m_AnimatedSpritesEntityIdPtr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(value);
                    m_Count = value.Length;
                }
            }
        }

        internal IntPtr animatedSpritesEntityIdPtr { get => m_AnimatedSpritesEntityIdPtr; set => m_AnimatedSpritesEntityIdPtr = value; }
        internal int count { get => m_Count; set => m_Count = value; }
        public float animationSpeed { get { return m_AnimationSpeed; } set { m_AnimationSpeed = value; } }
        public float animationStartTime { get { return m_AnimationStartTime; } set { m_AnimationStartTime = value; } }
        public TileAnimationFlags flags { get { return m_Flags; } set { m_Flags = value; } }

        private IntPtr m_AnimatedSpritesEntityIdPtr;
        private int m_Count;
        private float m_AnimationSpeed;
        private float m_AnimationStartTime;
        private TileAnimationFlags m_Flags;

        internal void CopyFrom(TileAnimationData other)
        {
            m_AnimatedSpritesEntityIdPtr = IntPtr.Zero;
            m_Count = 0;
            if (other.animatedSprites != null && other.animatedSprites.Length > 0)
            {
                var spriteArray = new NativeArray<EntityId>(other.animatedSprites.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < other.animatedSprites.Length; ++i)
                {
                    var sprite = other.animatedSprites[i];
                    spriteArray[i] = sprite != null ? sprite.GetEntityId() : EntityId.None;
                }
                animatedSpritesEntityIds = spriteArray;
                m_Count = other.animatedSprites.Length;
            }
            m_AnimationSpeed = other.animationSpeed;
            m_AnimationStartTime = other.animationStartTime;
            m_Flags = other.flags;
        }
    };

    // ==============================================================
    // TilemapCollider2D — 瓦片地图 2D 碰撞体
    // ==============================================================
    // 🎯 为 Tilemap 中的瓦片自动生成 2D 碰撞体
    //
    // 📌 关键属性：
    //   useDelaunayMesh：是否使用 Delaunay 三角化优化碰撞网格
    //   maximumTileChangeCount：最大变更队列长度（批量处理）
    //   extrusionFactor：碰撞体向外扩展的因子（防止缝隙）
    //   hasTilemapChanges：是否有待处理的瓦片变更
    //
    // 💡 工作流程：
    //   当 Tilemap 中的瓦片被修改时，碰撞体不会立即更新。
    //   调用 ProcessTilemapChanges() 处理变更队列并更新碰撞体。
    //   引擎会在物理更新前自动调用此方法。
    // ==============================================================
    [RequireComponent(typeof(Tilemap))]
    [NativeHeader("Modules/Tilemap/Public/TilemapCollider2D.h")]
    public sealed partial class TilemapCollider2D : Collider2D
    {
        // Get/Set Delaunay mesh usage.
        extern public bool useDelaunayMesh { get; set; }

        public extern uint maximumTileChangeCount
        {
            get;
            set;
        }

        public extern float extrusionFactor
        {
            get;
            set;
        }

        public extern bool hasTilemapChanges
        {
            [NativeMethod("HasTilemapChanges")]
            get;
        }

        [NativeMethod(Name = "ProcessTileChangeQueue")]
        public extern void ProcessTilemapChanges();
    }
}
