// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// =================================================================================================
// 🎯 Grid — 瓦片地图网格运行时组件
// =================================================================================================
// Grid 继承自 GridLayout，是 Tilemap 系统的核心布局组件，定义了网格的几何属性。
//
// 📌 核心属性
//   - cellSize: 每个单元格的世界空间尺寸
//   - cellGap: 单元格之间的间隙偏移
//   - cellLayout: Rectangle（矩形）/ Hexagon（六边形）/ Isometric（等距）/ IsometricZAsY
//   - cellSwizzle: 坐标轴重映射（XYZ / XZY / YXZ 等 6 种排列）
//
// 💡 Tilemap 坐标系映射
//   - Grid 是"逻辑坐标 → 世界坐标"的桥梁
//   - cellLayout 决定了 GetCellCenter / CellToLocal / LocalToCell 等转换公式
//   - Hexagon 布局时 cellSize 的 x 控制六边形宽度，y 控制行高
//   - IsometricZAsY 将 Z 轴映射为 Y，实现 2.5D 透视效果
//
// ⚡ Swizzle 机制
//   - Swizzle(cellSwizzle, position): 按规则重排 vector 分量
//   - InverseSwizzle: 逆向还原，用于不同坐标系间数据转换
// =================================================================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeHeader("Modules/Grid/Public/Grid.h")]
    public sealed partial class Grid : GridLayout
    {
        public new extern Vector3 cellSize
        {
            [FreeFunction("GridBindings::GetCellSize", HasExplicitThis = true)]
            get;
            [FreeFunction("GridBindings::SetCellSize", HasExplicitThis = true)]
            set;
        }

        public new extern Vector3 cellGap
        {
            [FreeFunction("GridBindings::GetCellGap", HasExplicitThis = true)]
            get;
            [FreeFunction("GridBindings::SetCellGap", HasExplicitThis = true)]
            set;
        }

        public new extern GridLayout.CellLayout cellLayout
        {
            get;
            set;
        }

        public new extern GridLayout.CellSwizzle cellSwizzle
        {
            get;
            set;
        }

        internal extern Vector3 inverseCellStride
        {
            [FreeFunction("GridBindings::GetInverseCellStride", HasExplicitThis = true)]
            get;
        }

        [FreeFunction("GridBindings::CellSwizzle")]
        public extern static Vector3 Swizzle(GridLayout.CellSwizzle swizzle, Vector3 position);

        [FreeFunction("GridBindings::InverseCellSwizzle")]
        public extern static Vector3 InverseSwizzle(GridLayout.CellSwizzle swizzle, Vector3 position);
    }
}
