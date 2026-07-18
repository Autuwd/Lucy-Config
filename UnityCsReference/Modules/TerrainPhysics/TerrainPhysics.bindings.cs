// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 TerrainCollider — 地形碰撞器
//
// 📌 作用：
//   为地形提供物理碰撞检测。TerrainCollider 不使用传统的
//   Mesh Collider（网格碰撞器），而是基于高度图生成碰撞形状，
//   效率远高于同等精度的 Mesh Collider。
//
// 🏗 碰撞生成原理：
//
//   高度图 (Heightmap) → TerrainCollider
//        ↓                     ↓
//   float[,] 数据         PhysX 的高度场碰撞形状
//        ↓                  (HeightField Shape)
//   每个格子=一个高度值      自适应三角形网格
//
//   💡 碰撞网格 ≠ 渲染网格！
//   - 渲染网格：由 Terrain 组件生成（可带 LOD、细节）
//   - 碰撞网格：由 TerrainCollider 生成（更简单、固定精度）
//   - 两者共享同一个高度图数据源
//
// ⚡ TerrainCollider 的优势：
//   - 内存占用远小于 Mesh Collider（不存储三角形，只存高度值）
//   - 碰撞检测性能与高度图分辨率线性相关
//   - 支持 Holes 孔洞（hitHoles 参数控制光线投射是否检测孔洞）
//
// 💡 工作原理细节：
//   TerrainCollider 为每个高度图格子生成一个四边形（2个三角形），
//   但 PhysX 的高度场（HeightField）会自适应简化远距离的碰撞网格。
//   terrainData 的 heightmapResolution 越高，碰撞精度越高，性能越低。
//
// 📌 重要提示：
//   TerrainCollider 的 Raycast 是 internal 方法，不对外公开。
//   外部代码通过 Physics.Raycast 间接调用，引擎内部会路由到这里。
// ==============================================================

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/TerrainPhysics/TerrainCollider.h")]
    [NativeHeader("Modules/Terrain/Public/TerrainData.h")]
    public class TerrainCollider : Collider
    {
        // ==============================================================
        // 📌 terrainData — 关联的地形数据
        //
        // TerrainCollider 不存储自己的高度图数据，
        // 而是引用 Terrain 组件使用的 TerrainData。
        // 这意味着修改 TerrainData 的高度图会自动更新碰撞形状。
        //
        // ⚠️ 一个 TerrainData 只能被一个 TerrainCollider 使用。
        // ==============================================================
        public extern TerrainData terrainData { get; set; }

        // ==============================================================
        // 🔫 Raycast — 地形光线投射（内部方法）
        //
        // 📌 参数：
        //   ray：       光线
        //   maxDistance：最大投射距离
        //   hitHoles：   是否检测孔洞区域（false=穿过孔洞，如洞穴入口）
        //
        // 💡 hitHoles 的使用场景：
        //   洞穴入口上方有地形孔洞，角色应该能走进去。
        //   此时 hitHoles=false 让 Raycast 穿过孔洞检测洞穴内部。
        // ==============================================================
        extern private RaycastHit Raycast(Ray ray, float maxDistance, bool hitHoles, ref bool hasHit);

        internal bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, bool hitHoles)
        {
            bool hasHit = false;
            hitInfo = Raycast(ray, maxDistance, hitHoles, ref hasHit);
            return hasHit;
        }
    }
}
