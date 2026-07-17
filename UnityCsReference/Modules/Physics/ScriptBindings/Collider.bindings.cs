// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.LowLevelPhysics;

namespace UnityEngine
{
    //=============================================================================
    // Collider 基类
    //=============================================================================
    // 碰撞器（Collider）是物理系统中定义物体碰撞形状的组件。
    // 所有具体碰撞器类型（BoxCollider, SphereCollider, CapsuleCollider, MeshCollider 等）
    // 都继承自此类。
    //
    // 碰撞器必须附加到 GameObject 上，并且 GameObject 必须同时具有 Transform 组件。
    //
    // 碰撞器有两种工作模式：
    //   1. 普通碰撞（isTrigger = false）：产生物理碰撞响应，触发 OnCollisionX 事件
    //   2. 触发器（isTrigger = true）：不产生物理响应，只触发 OnTriggerX 事件（穿透检测）
    //
    // 物理材质（PhysicsMaterial）控制碰撞表面的摩擦力和弹力等属性。
    //=============================================================================
    [NativeHeader("Modules/Physics/Collider.h")]
    public partial class Collider : Component
    {
        /// <summary>碰撞器是否启用。禁用后不参与物理碰撞检测。</summary>
        extern public bool enabled { get; set; }

        /// <summary>获取附加到此碰撞器的 Rigidbody 组件。如果未附加 Rigidbody，返回 null。</summary>
        extern public Rigidbody attachedRigidbody { [NativeMethod("GetRigidbody")] get; }

        /// <summary>获取附加到此碰撞器的 ArticulationBody 组件。</summary>
        extern public ArticulationBody attachedArticulationBody { [NativeMethod("GetArticulationBody")] get; }

        /// <summary>
        ///   是否为触发器。
        ///   true：不产生碰撞响应，但会触发 OnTriggerEnter/Stay/Exit 事件。
        ///   false（默认）：触发正常的物理碰撞，调用 OnCollisionEnter/Stay/Exit。
        /// </summary>
        extern public bool isTrigger { get; set; }

        /// <summary>
        ///   碰撞器的接触偏移量。
        ///   在碰撞器表面外扩展的一个"安全区域"，用于在碰撞发生前产生接触事件。
        ///   增大此值可以提高碰撞检测的稳定性，但会降低碰撞精度。
        /// </summary>
        extern public float contactOffset { get; set; }

        /// <summary>
        ///   计算碰撞器表面上离指定世界坐标点最近的点。
        ///   用于获取物体表面的精确位置。
        /// </summary>
        /// <param name="position">世界空间中的查询点</param>
        /// <returns>碰撞器表面上的最近点（世界坐标）</returns>
        extern public Vector3 ClosestPoint(Vector3 position);

        /// <summary>
        ///   碰撞器的世界空间轴对齐包围盒（AABB）。
        ///   只读，由物理引擎每帧自动更新。
        ///   注意：对于旋转的碰撞器，bounds 会包含整个旋转形状。
        /// </summary>
        extern public Bounds bounds { get; }

        /// <summary>
        ///   是否允许在运行时修改此碰撞器的接触信息。
        ///   需要在 Physics.ContactModifyEvent 事件中修改接触点数据时设为 true。
        /// </summary>
        extern public bool hasModifiableContacts { get; set; }

        /// <summary>碰撞器是否提供详细的接触点信息供查询。</summary>
        extern public bool providesContacts { get; set; }

        /// <summary>
        ///   层覆盖优先级。
        ///   当两个碰撞器使用 includeLayers / excludeLayers 进行碰撞过滤时，
        ///   优先级较高的碰撞器的层设置会覆盖优先级较低的。
        /// </summary>
        extern public int layerOverridePriority { get; set; }

        /// <summary>
        ///   排除层列表。
        ///   此碰撞器不会与指定层上的碰撞器发生碰撞。
        ///   配合 layerOverridePriority 使用。
        /// </summary>
        extern public LayerMask excludeLayers { get; set; }

        /// <summary>
        ///   包含层列表。
        ///   此碰撞器仅与指定层上的碰撞器发生碰撞。
        ///   优先级高于 excludeLayers。
        /// </summary>
        extern public LayerMask includeLayers { get; set; }

        /// <summary>
        ///   获取碰撞器的几何体持有者（GeometryHolder）。
        ///   包含了碰撞器的底层几何体数据（球体/盒子/胶囊体/网格等）。
        /// </summary>
        public GeometryHolder GeometryHolder { get => this.GetGeometryHolder(); }

        /// <summary>
        ///   以指定几何体类型获取碰撞器的底层几何数据。
        ///   T 可以是 BoxGeometry、SphereGeometry、CapsuleGeometry 等。
        ///   如果碰撞器类型与 T 不匹配，会抛出 InvalidOperationException。
        /// </summary>
        public T GetGeometry<T>() where T : struct, IGeometry
        {
            return this.GetGeometryHolder().As<T>();
        }

        /// <summary>
        ///   获取或设置与此碰撞器关联的 PhysicsMaterial。
        ///   同一个 PhysicsMaterial 资源可被多个碰撞器共享。
        ///   修改 sharedMaterial 会影响所有使用此材质的碰撞器。
        /// </summary>
        [NativeMethod("Material")]
        extern public PhysicsMaterial sharedMaterial { get; set; }

        /// <summary>
        ///   获取或设置此碰撞器的独立 PhysicsMaterial 副本。
        ///   修改 material 只会影响当前碰撞器。
        ///   getter 返回材质的克隆，setter 将新材质应用到碰撞器。
        /// </summary>
        extern public PhysicsMaterial material
        {
            [NativeMethod("GetClonedMaterial")]
            get;
            [NativeMethod("SetMaterial")]
            set;
        }

        //----- 碰撞器上的射线检测 ---------------------------------------------

        extern private RaycastHit Raycast(Ray ray, float maxDistance, ref bool hasHit);

        /// <summary>
        ///   从碰撞器自身发射射线检测，判断射线是否碰到本碰撞器。
        ///   与 Physics.Raycast 不同，此方法限定只检测当前碰撞器。
        /// </summary>
        /// <param name="ray">射线</param>
        /// <param name="hitInfo">碰撞信息输出</param>
        /// <param name="maxDistance">最大检测距离</param>
        /// <returns>是否命中</returns>
        public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            bool hasHit = false;
            hitInfo = Raycast(ray, maxDistance, ref hasHit);
            return hasHit;
        }

        //----- 边界上的最近点 -------------------------------------------------

        [NativeName("ClosestPointOnBounds")]
        extern private void Internal_ClosestPointOnBounds(Vector3 point, ref Vector3 outPos, ref float distance);

        /// <summary>
        ///   获取碰撞器 AABB 边界上离指定点最近的点。
        ///   与 ClosestPoint 不同，此方法只返回边界框上的点，不一定是碰撞器表面的精确点。
        ///   效率高于 ClosestPoint，但精度较低。
        /// </summary>
        public Vector3 ClosestPointOnBounds(Vector3 position)
        {
            float dist = 0f;
            Vector3 outpos = Vector3.zero;
            Internal_ClosestPointOnBounds(position, ref outpos, ref dist);
            return outpos;
        }
    }
}
