// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// =================================================================================================
// 🎯 GridLayout — 网格布局坐标变换基础类
// =================================================================================================
// GridLayout 定义了"网格坐标 ↔ 世界坐标"的完整变换管线，是 Tilemap/WFC/Marching Squares
// 等所有基于网格的系统的数学基础。
//
// 📌 坐标转换方程组
//   - CellToLocal(Vector3Int): 单元格整数坐标 → 局部空间位置（单位缩放）
//   - LocalToCell(Vector3): 局部空间位置 → 最近单元格整数坐标（含 floor 截断）
//   - CellToLocalInterpolated(Vector3): 浮点单元格坐标 → 局部空间（亚像素插值）
//   - LocalToCellInterpolated(Vector3): 局部空间 → 浮点单元格坐标
//   - CellToWorld / WorldToCell: 经过 Transform 矩阵的完整世界空间转换
//   - GetBoundsLocal(Vector3Int): 获取指定单元格的 AABB 包围盒
//
// 💡 Layout 类型差异
//   - Rectangle: 标准笛卡尔网格，CellToWorld = 线性缩放 + 平移
//   - Hexagon: 奇数行偏移（row offset），点顶式/平顶式由 cellSize.y 符号决定
//   - Isometric: 菱形网格，x 轴沿屏幕对角线 26.565°
//   - IsometricZAsY: Z 轴替换 Y 轴的特殊模式（用于 Tilemap 中 Z-as-Y 排序）
//
// ⚡ 性能要点
//   - 所有转换方法都是纯数学计算（无 GC 分配）
//   - Swizzle 操作是分量重排序（常量级操作）
//   - GetLayoutCellCenter 缓存每帧的布局偏移
// =================================================================================================

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeHeader("Modules/Grid/Public/Grid.h")]
    public partial class GridLayout : Behaviour
    {
        // Enums.
        public enum CellLayout
        {
            Rectangle = 0,
            Hexagon = 1,
            Isometric = 2,
            IsometricZAsY = 3,
        }

        public enum CellSwizzle
        {
            XYZ = 0,
            XZY = 1,
            YXZ = 2,
            YZX = 3,
            ZXY = 4,
            ZYX = 5
        }

        public extern Vector3 cellSize
        {
            [FreeFunction("GridLayoutBindings::GetCellSize", HasExplicitThis = true)]
            get;
        }

        public extern Vector3 cellGap
        {
            [FreeFunction("GridLayoutBindings::GetCellGap", HasExplicitThis = true)]
            get;
        }

        public extern CellLayout cellLayout
        {
            get;
        }

        public extern CellSwizzle cellSwizzle
        {
            get;
        }

        [FreeFunction("GridLayoutBindings::GetBoundsLocal", HasExplicitThis = true)]
        public extern Bounds GetBoundsLocal(Vector3Int cellPosition);

        public Bounds GetBoundsLocal(Vector3 origin, Vector3 size)
        {
            return GetBoundsLocalOriginSize(origin, size);
        }

        [FreeFunction("GridLayoutBindings::GetBoundsLocalOriginSize", HasExplicitThis = true)]
        private extern Bounds GetBoundsLocalOriginSize(Vector3 origin, Vector3 size);

        [FreeFunction("GridLayoutBindings::CellToLocal", HasExplicitThis = true)]
        public extern Vector3 CellToLocal(Vector3Int cellPosition);

        [FreeFunction("GridLayoutBindings::LocalToCell", HasExplicitThis = true)]
        public extern Vector3Int LocalToCell(Vector3 localPosition);

        [FreeFunction("GridLayoutBindings::CellToLocalInterpolated", HasExplicitThis = true)]
        public extern Vector3 CellToLocalInterpolated(Vector3 cellPosition);

        [FreeFunction("GridLayoutBindings::LocalToCellInterpolated", HasExplicitThis = true)]
        public extern Vector3 LocalToCellInterpolated(Vector3 localPosition);

        [FreeFunction("GridLayoutBindings::CellToWorld", HasExplicitThis = true)]
        public extern Vector3 CellToWorld(Vector3Int cellPosition);

        [FreeFunction("GridLayoutBindings::WorldToCell", HasExplicitThis = true)]
        public extern Vector3Int WorldToCell(Vector3 worldPosition);

        [FreeFunction("GridLayoutBindings::LocalToWorld", HasExplicitThis = true)]
        public extern Vector3 LocalToWorld(Vector3 localPosition);

        [FreeFunction("GridLayoutBindings::WorldToLocal", HasExplicitThis = true)]
        public extern Vector3 WorldToLocal(Vector3 worldPosition);

        [FreeFunction("GridLayoutBindings::GetLayoutCellCenter", HasExplicitThis = true)]
        public extern Vector3 GetLayoutCellCenter();

        [RequiredByNativeCode]
        private void DoNothing() {}
    }
}
