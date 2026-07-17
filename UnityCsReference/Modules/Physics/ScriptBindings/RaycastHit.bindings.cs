// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /// <summary>
    /// 射线/形状投射的命中结果结构体，存储碰撞点、法线、距离、碰撞器等详细信息。
    ///
    /// 由 Physics.Raycast、Physics.SphereCast 等查询方法返回。
    /// 底层数据来自 PhysX 的 PxRaycastHit / PxSweepHit。
    /// 此结构体是值类型（struct），在批量查询（RaycastCommand.ScheduleBatch）中可安全用于 Job System。
    /// </summary>
    [NativeHeader("Runtime/Interfaces/IPhysics.h")]
    [NativeHeader("PhysicsScriptingClasses.h")]
    [NativeHeader("Modules/Physics/RaycastHit.h")]
    [UsedByNativeCode]
    public partial struct RaycastHit
    {
        //==================== 底层字段（与 C++ 侧直接对应） ====================//
        [NativeName("point")] internal Vector3 m_Point;           // 命中点坐标（世界空间）
        [NativeName("normal")] internal Vector3 m_Normal;         // 命中点表面法线（世界空间）
        [NativeName("faceID")] internal uint m_FaceID;            // 命中三角面的索引
        [NativeName("distance")] internal float m_Distance;       // 从射线起点到命中点的距离
        [NativeName("uv")] internal Vector2 m_UV;                 // 命中点的重心坐标（UV 编码）
        [NativeName("collider")] internal EntityId m_Collider;    // 被命中碰撞器的实体 ID

        //==================== 公开属性 ====================//

        /// <summary>
        /// 被射线命中的碰撞器引用。
        /// 通过 EntityId 从 C++ 侧查找对应的托管 Collider 对象。
        /// </summary>
        public Collider collider { get { return Object.FindObjectFromInstanceID(m_Collider) as Collider; } }

        /// <summary>
        /// 已废弃（编译错误）：使用 colliderEntityId 代替。
        /// </summary>
        [System.Obsolete("RaycastHit.colliderInstanceID is obsolete. Use RaycastHit.colliderEntityId instead.", true)]
        public int colliderInstanceID { get { return m_Collider; } }

        /// <summary>被命中碰撞器的实体 ID，用于底层物理查询和 ECS 互操作。</summary>
        public EntityId colliderEntityId { get { return m_Collider; } }

        /// <summary>命中点在世界空间中的位置坐标。</summary>
        public Vector3 point { get { return m_Point; } set { m_Point = value; } }

        /// <summary>命中点处的表面法线向量（世界空间，已归一化）。</summary>
        public Vector3 normal { get { return m_Normal; } set { m_Normal = value; } }

        /// <summary>
        /// 命中点的重心坐标（barycentric coordinate）。
        /// 存储方式：UV.x = 重心坐标 v，UV.y = 重心坐标 w，UV.z = 1 - v - w。
        /// 用于在三角面上插值顶点属性。
        /// </summary>
        public Vector3 barycentricCoordinate { get { return new Vector3(1.0F - (m_UV.y + m_UV.x), m_UV.x, m_UV.y); } set { m_UV = value; } }

        /// <summary>从射线起点到命中点的距离。</summary>
        public float distance { get { return m_Distance; } set { m_Distance = value; } }

        /// <summary>被命中三角面的索引（仅对 MeshCollider 有效）。</summary>
        public int triangleIndex { get { return (int)m_FaceID; } }

        /// <summary>
        /// 内部方法：计算命中点的纹理坐标。
        /// 通过 C++ 侧的 PhysX 函数计算 UV 坐标。
        /// </summary>
        [NativeMethod("CalculateRaycastTexCoord", true, true)]
        extern static private Vector2 CalculateRaycastTexCoord(EntityId colliderInstanceID, Vector2 uv, Vector3 pos, uint face, int textcoord);

        /// <summary>命中点的第一套纹理坐标（UV0）。</summary>
        public Vector2 textureCoord { get { return CalculateRaycastTexCoord(m_Collider, m_UV, m_Point, m_FaceID, 0); } }

        /// <summary>命中点的第二套纹理坐标（UV1，通常用于光照贴图）。</summary>
        public Vector2 textureCoord2 { get { return CalculateRaycastTexCoord(m_Collider, m_UV, m_Point, m_FaceID, 1); } }

        /// <summary>
        /// 被命中物体的 Transform 组件。
        /// 优先返回刚体的 Transform；若无刚体则返回碰撞器的 Transform。
        /// </summary>
        public Transform transform
        {
            get
            {
                Rigidbody body = rigidbody;
                if (body != null)
                    return body.transform;
                else if (collider != null)
                    return collider.transform;
                else
                    return null;
            }
        }

        /// <summary>被命中物体上的 Rigidbody 组件（如果有）。</summary>
        public Rigidbody rigidbody { get { return collider != null ? collider.attachedRigidbody : null; } }

        /// <summary>被命中物体上的 ArticulationBody 组件（如果有）。</summary>
        public ArticulationBody articulationBody { get { return collider != null ? collider.attachedArticulationBody : null; } }

        /// <summary>
        /// 命中点的光照贴图 UV 坐标。
        /// 通过纹理坐标（UV1）和渲染器的 lightmapScaleOffset 变换计算得出。
        /// </summary>
        public Vector2 lightmapCoord
        {
            get
            {
                Vector2 coord = CalculateRaycastTexCoord(m_Collider, m_UV, m_Point, m_FaceID, 1);
                if (collider.GetComponent<Renderer>() != null)
                {
                    Vector4 st = collider.GetComponent<Renderer>().lightmapScaleOffset;
                    coord.x = coord.x * st.x + st.z;
                    coord.y = coord.y * st.y + st.w;
                }
                return coord;
            }
        }
    }
}
