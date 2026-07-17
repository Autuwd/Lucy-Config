// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ================================================================
// Transform - Unity 引擎核心组件
// ================================================================
//
// 【概述】
// Transform 是 Unity 场景中所有 GameObject 必备的组件，负责管理对象在
// 3D/2D 空间中的位置（Position）、旋转（Rotation）和缩放（Scale）。
// 每个 GameObject 有且仅有一个 Transform 组件，且不可删除。
//
// 【继承链】
// Transform -> Component -> Object
//
// RectTransform（UI 系统）继承自 Transform，增加了锚点（Anchor）
// 和轴心点（Pivot）等 UI 布局概念。
//
// 【与 GameObject 的关系】
// - 每个 GameObject 创建时自动附带 Transform
// - gameObject.transform 是获取 Transform 的最常用方式
// - Transform 的父子关系决定了场景层级（Hierarchy）结构
//
// 【坐标系统】
// - 世界坐标（World Space）：场景全局坐标系
// - 局部坐标（Local Space）：相对于父级 Transform 的坐标系
// - position 是世界坐标，localPosition 是相对父级的坐标
//
// 【层级结构】
// Transform 通过父子关系组成树形结构：
// - 根 Transform（root）：层级树的最顶层，位于场景根级
// - 父 Transform（parent）：当前对象的直接父级
// - 子 Transform（GetChild）：当前对象的直接子级
// - 兄弟节点（Sibling）：同一父级下的同级对象
//
// 【核心功能】
// 1. 空间变换：position/rotation/scale 的读写
// 2. 层级管理：parent/child 关系的增删查改
// 3. 坐标变换：world <-> local 的点、方向、向量转换
// 4. 便捷操作：Translate/Rotate/LookAt/RotateAround
// 5. 矩阵运算：worldToLocalMatrix/localToWorldMatrix
// ================================================================

using UnityEngine.Bindings;
using UnityEngine.Scripting;

using System;
using System.Collections;

namespace UnityEngine
{
    // ================================================================
    // RotationOrder - 欧拉角旋转顺序
    // ================================================================
    // 定义欧拉角应用的顺序。不同顺序会产生不同的最终旋转结果，
    // 例如 XYZ 和 ZYX 在相同角度值下得到的方向完全不同。
    // 此枚举用于编辑器中的旋转顺序设置（Euler Angles 模式）。
    // ================================================================
    //*undocumented
    internal enum RotationOrder { OrderXYZ, OrderXZY, OrderYZX, OrderYXZ, OrderZXY, OrderZYX }

    // ================================================================
    // Transform - 游戏对象空间变换组件
    // ================================================================
    //
    // Transform 是 Unity 中最核心的组件之一，每个 GameObject 都自动拥有。
    // 它管理对象的空间状态（位置/旋转/缩放）并维护场景层级树。
    //
    // 【核心概念】
    //
    // 1. 位置（Position）
    //    - position：在世界空间中的坐标
    //    - localPosition：相对于父级 Transform 的坐标
    //    - 如果没有父级，position == localPosition
    //
    // 2. 旋转（Rotation）
    //    - 内部使用四元数（Quaternion）存储，避免万向锁
    //    - eulerAngles 属性提供欧拉角表示（方便 Inspector 编辑）
    //    - 注意：欧拉角存在万向锁（Gimbal Lock）问题
    //
    // 3. 缩放（Scale）
    //    - localScale：相对于父级的缩放
    //    - lossyScale：世界空间中的最终缩放（只读，受父级影响）
    //
    // 4. 层级父子关系
    //    - parent：设置/获取父级 Transform
    //    - SetParent()：带 worldPositionStays 参数控制位置保持策略
    //    - childCount / GetChild()：遍历子级
    //
    // 5. 坐标轴
    //    - right (红色 X 轴) / up (绿色 Y 轴) / forward (蓝色 Z 轴)
    //    - 这些是当前旋转下局部轴在世界空间中的方向
    //
    // 6. 坐标变换
    //    - TransformPoint / InverseTransformPoint：位置转换
    //    - TransformDirection / InverseTransformDirection：方向转换（不受缩放影响）
    //    - TransformVector / InverseTransformVector：向量转换（受缩放影响）
    //
    // 【性能提示】
    // - Transform 属性每次访问都会与 C++ 端通信，频繁访问应缓存
    // - hasChanged 属性可用于检测 Transform 是否被修改
    // - 大批量对象操作时考虑使用 TransformAccessArray
    //
    // 官方文档：https://docs.unity3d.com/ScriptReference/Transform.html
    // ================================================================
    [NativeHeader("Configuration/UnityConfigure.h")]
    [NativeHeader("Runtime/Transform/Transform.h")]
    [NativeHeader("Runtime/Transform/ScriptBindings/TransformScriptBindings.h")]
    [RequiredByNativeCode]
    public partial class Transform : Component, IEnumerable
    {
        protected Transform() { }

        // ================================================================
        // position - 世界空间中的位置
        // ================================================================
        // 返回/设置 Transform 在世界坐标系中的位置。
        //
        // 【世界坐标 vs 局部坐标】
        // - position：世界坐标，是最终渲染使用的实际位置
        // - localPosition：相对于父级的坐标
        // - 如果没有父级，position == localPosition
        //
        // 【坐标转换关系】
        // position = parent.position + parent.rotation * (parent.localScale * localPosition)
        // 其中 parent.rotation * (...) 表示旋转后的偏移
        //
        // 【注意事项】
        // - 设置 position 会同时改变 localPosition（自动计算反向变换）
        // - 频繁读写 position 有性能开销，应缓存
        // - 对于 UI 元素（RectTransform），使用 anchoredPosition 替代
        // ================================================================
        public extern Vector3 position { get; set; }

        // ================================================================
        // localPosition - 相对于父级的局部位置
        // ================================================================
        // 返回/设置 Transform 相对于父级 Transform 的局部坐标系位置。
        //
        // 【使用场景】
        // - 当希望对象相对于父级移动时使用 localPosition
        // - 在编辑器 Inspector 中看到的 Position 值就是 localPosition
        //
        // 【示例】
        // 父级在 (10, 0, 0)，子级的 localPosition 为 (2, 0, 0)，
        // 则子级的 position（世界坐标）为 (12, 0, 0)
        // ================================================================
        public extern Vector3 localPosition { get; set; }

        // 获取指定旋转顺序下的局部欧拉角（内部编辑使用）
        internal extern Vector3 GetLocalEulerAngles(RotationOrder order);

        // 使用指定旋转顺序设置局部欧拉角（内部编辑使用）
        internal extern void SetLocalEulerAngles(Vector3 euler, RotationOrder order);

        // 设置局部欧拉角提示（编辑器专用，用于 Inspector 连续旋转）
        [NativeConditional("UNITY_EDITOR")]
        internal extern void SetLocalEulerHint(Vector3 euler);

        // ================================================================
        // eulerAngles - 欧拉角表示的世界旋转（度）
        // ================================================================
        // 以欧拉角（度）的形式获取/设置世界空间中的旋转。
        //
        // 【四元数 vs 欧拉角】
        // - Unity 内部使用四元数（Quaternion）存储旋转，有 4 个分量 (x,y,z,w)
        // - eulerAngles 是将四元数转换为欧拉角 3 个分量 (x,y,z) 的便捷属性
        //
        // 【万向锁（Gimbal Lock）警告】
        // 欧拉角在旋转到 90 度时会出现万向锁，丢失一个旋转自由度。
        // 如果需要进行平滑旋转插值，应直接使用 Quaternion 操作。
        //
        // 【旋转顺序】
        // Unity 的欧拉角应用顺序为 Z → X → Y（相对于局部坐标轴）
        //
        // 【性能提示】
        // - get { return rotation.eulerAngles; } - 有 Quaternion → Vector3 转换开销
        // - set { rotation = Quaternion.Euler(value); } - 有 Vector3 → Quaternion 转换开销
        // ================================================================
        public Vector3 eulerAngles { get { return rotation.eulerAngles; } set { rotation = Quaternion.Euler(value); } }

        // ================================================================
        // localEulerAngles - 欧拉角表示的局部旋转（度）
        // ================================================================
        // 以欧拉角（度）的形式获取/设置相对于父级的局部旋转。
        // 等同于 localRotation 的欧拉角表示。
        // ================================================================
        public Vector3 localEulerAngles { get { return localRotation.eulerAngles; } set { localRotation = Quaternion.Euler(value); } }

        // ================================================================
        // right - 世界空间中的右方向（红色 X 轴）
        // ================================================================
        // 返回/设置 Transform 的红色箭头（X 轴正方向）在世界空间中的指向。
        //
        // 【说明】
        // - get：返回 rotation * Vector3.right，即当前旋转后的 X 轴方向
        // - set：使用 FromToRotation 计算从默认 Vector3.right 到目标方向的旋转
        //
        // 【常见用途】
        // - 获取物体的右侧方向
        // - 用于侧向移动：transform.right * speed * Time.deltaTime
        // ================================================================
        public Vector3 right { get { return rotation * Vector3.right; } set { rotation = Quaternion.FromToRotation(Vector3.right, value); } }

        // ================================================================
        // up - 世界空间中的上方向（绿色 Y 轴）
        // ================================================================
        // 返回/设置 Transform 的绿色箭头（Y 轴正方向）在世界空间中的指向。
        //
        // 【说明】
        // - get：返回 rotation * Vector3.up
        // - set：使用 FromToRotation 计算旋转
        //
        // 【常见用途】
        // - 获取物体的正上方方向
        // - 用于上方向量相关计算
        // ================================================================
        public Vector3 up { get { return rotation * Vector3.up; } set { rotation = Quaternion.FromToRotation(Vector3.up, value); } }

        // ================================================================
        // forward - 世界空间中的前方向（蓝色 Z 轴）
        // ================================================================
        // 返回/设置 Transform 的蓝色箭头（Z 轴正方向）在世界空间中的指向。
        //
        // 【说明】
        // - get：返回 rotation * Vector3.forward
        // - set：使用 LookRotation 直接将 forward 对齐到目标方向
        //
        // ⚠️ 注意：Unity 的 forward 是 Z 轴正方向（与 OpenGL 一致），
        //   与某些引擎（如 Unreal 的 X 轴为 forward）不同。
        //
        // 【常见用途】
        // - 获取物体的朝向：transform.forward
        // - 让物体沿自身朝向移动：transform.forward * speed * Time.deltaTime
        // ================================================================
        public Vector3 forward { get { return rotation * Vector3.forward; } set { rotation = Quaternion.LookRotation(value); } }

        // ================================================================
        // rotation - 世界空间中的旋转（四元数）
        // ================================================================
        // 返回/设置 Transform 在世界空间中的旋转，以四元数（Quaternion）表示。
        //
        // 【四元数优点】
        // - 无万向锁（Gimbal Lock）问题
        // - 适合做平滑插值（Quaternion.Slerp/Lerp）
        // - 占 4 个 float（比 3x3 矩阵省空间）
        //
        // 【四元数缺点】
        // - 不直观，难以直接编辑
        // - Inspector 中以欧拉角显示
        //
        // 【常用操作】
        // - rotation = Quaternion.Euler(0, 90, 0);     // 欧拉角 → 四元数
        // - rotation = Quaternion.LookRotation(dir);   // 朝向目标方向
        // - rotation = Quaternion.Slerp(a, b, t);      // 平滑插值
        // ================================================================
        public extern Quaternion rotation { get; set; }

        // ================================================================
        // localRotation - 相对于父级的局部旋转（四元数）
        // ================================================================
        // 返回/设置 Transform 相对于父级的局部旋转。
        //
        // 【关系】
        // rotation = parent.rotation * localRotation
        // （如果没有父级则 rotation == localRotation）
        // ================================================================
        public extern Quaternion localRotation { get; set; }

        // 编辑器专用的欧拉角旋转顺序设置
        // 控制当使用欧拉角旋转时，XYZ 轴的应用顺序
        [NativeConditional("UNITY_EDITOR")]
        internal RotationOrder rotationOrder
        {
            get { return (RotationOrder)GetRotationOrderInternal(); }
            set { SetRotationOrderInternal(value); }
        }

        [NativeConditional("UNITY_EDITOR")]
        [NativeMethod("GetRotationOrder")]
        internal extern int GetRotationOrderInternal();
        [NativeConditional("UNITY_EDITOR")]
        [NativeMethod("SetRotationOrder")]
        internal extern void SetRotationOrderInternal(RotationOrder rotationOrder);

        // ================================================================
        // localScale - 局部缩放
        // ================================================================
        // 返回/设置 Transform 相对于父级的缩放比例。
        //
        // 【缩放对子级的影响】
        // - 子级的 position/rotation 会受到父级缩放影响
        // - 非均匀缩放（如 (1, 2, 1)）会使子级旋转产生切变
        //
        // 【注意事项】
        // - localScale 为 (0, 0, 0) 会使对象不可见
        // - 负值缩放会使对象翻转（镜像）
        // - localScale 为 1 表示原始大小
        // ================================================================
        public extern Vector3 localScale { get; set; }

        // ================================================================
        // parent - 父级 Transform
        // ================================================================
        // 获取/设置 Transform 的父级。
        //
        // 【设置父级的两种方式】
        // 1. parent 属性（直接赋值）
        //    - 保持世界位置不变，相当于 SetParent(p, true)
        //    - 对 RectTransform 有警告（应使用 SetParent）
        //
        // 2. SetParent() 方法
        //    - SetParent(p, true)：保持世界位置不变
        //    - SetParent(p, false)：保持局部位置不变
        //
        // 【worldPositionStays 参数详解】
        // - true（默认）：保持对象在世界空间中的位置/旋转/缩放不变。
        //   改变父级后，会自动重新计算 localPosition/localRotation/localScale。
        //   效果：看到的位置不变，但 Inspector 中的 local 值会变。
        //
        // - false：保持对象的 localPosition/localRotation/localScale 不变。
        //   改变父级后，世界位置会随新父级而改变。
        //   效果：Inspector 中的 local 值不变，但场景中的世界位置会变。
        //
        // 【设置为 null】
        // 将父级设为 null 使 Transform 成为场景根对象。
        // 此时 position == localPosition，rotation == localRotation。
        // ================================================================
        public Transform parent
        {
            get { return parentInternal; }
            set
            {
                if (this is RectTransform)
                    Debug.LogWarning("Parent of RectTransform is being set with parent property. Consider using the SetParent method instead, with the worldPositionStays argument set to false. This will retain local orientation and scale rather than world orientation and scale, which can prevent common UI scaling issues.", this);
                parentInternal = value;
            }
        }

        internal Transform parentInternal
        {
            get { return GetParent(); }
            set { SetParent(value); }
        }

        private extern Transform GetParent();

        // ================================================================
        // SetParent - 设置父级 Transform
        // ================================================================
        // SetParent(Transform parent)
        //   默认 worldPositionStays = true，保持世界位置不变。
        //
        // SetParent(Transform parent, bool worldPositionStays)
        //   worldPositionStays = true  -- 世界位置不变（默认行为）
        //   worldPositionStays = false -- 局部位置不变
        //
        // 【使用建议】
        // - 对普通 GameObject 用 parent 属性或 SetParent(p, true) 均可
        // - 对 UI 的 RectTransform 总是使用 SetParent(p, false)
        //   以避免父级缩放导致的 UI 变形
        // ================================================================
        public void SetParent(Transform p)
        {
            SetParent(p, true);
        }

        [FreeFunction("SetParent", HasExplicitThis = true)]
        public extern void SetParent(Transform parent, bool worldPositionStays);

        // ================================================================
        // worldToLocalMatrix - 世界 → 局部变换矩阵（只读）
        // ================================================================
        // 返回将点从世界空间变换到局部空间的 4x4 矩阵。
        // 该矩阵包含了位置、旋转和缩放的逆变换。
        //
        // 本质上是 localToWorldMatrix 的逆矩阵。
        // ================================================================
        public extern Matrix4x4 worldToLocalMatrix { get; }

        // ================================================================
        // localToWorldMatrix - 局部 → 世界变换矩阵（只读）
        // ================================================================
        // 返回将点从局部空间变换到世界空间的 4x4 矩阵。
        // 该矩阵包含了位置、旋转和缩放的组合变换。
        //
        // 【矩阵构成】
        // localToWorldMatrix = T * R * S
        // 其中 T = 平移矩阵, R = 旋转矩阵, S = 缩放矩阵
        //
        // 【注意】
        // - 如果有父级，包含所有父级的变换
        // - 变换顺序：先缩放 → 再旋转 → 后平移（相对于局部轴）
        // ================================================================
        public extern Matrix4x4 localToWorldMatrix { get; }

        // ================================================================
        // SetPositionAndRotation - 同时设置位置和旋转
        // ================================================================
        // 在世界空间中同时设置 position 和 rotation。
        //
        // 【性能优势】
        // 比分别设置 position 和 rotation 更高效，因为只需一次
        // C++ 原生调用就能完成两个值的设置。
        // ================================================================
        public extern void SetPositionAndRotation(Vector3 position, Quaternion rotation);

        // 在局部空间中同时设置 localPosition 和 localRotation
        public extern void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation);

        // 同时获取世界空间的 position 和 rotation（一次 C++ 调用）
        public extern void GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        // 同时获取局部空间的 localPosition 和 localRotation（一次 C++ 调用）
        public extern void GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation);

        // ================================================================
        // Translate - 移动 Transform
        // ================================================================
        // 将 Transform 沿指定方向和距离移动。
        //
        // 【重载版本】
        // 1. Translate(translation, Space.Self)       -- 沿局部轴移动
        // 2. Translate(translation, Space.World)      -- 沿世界轴移动
        // 3. Translate(translation, relativeTo)        -- 沿指定 Transform 的局部轴移动
        // 4. Translate(x, y, z, relativeTo)            -- 分量形式
        //
        // 【Space.Self 与 Space.World 的区别】
        // - Space.Self：沿自身局部坐标轴移动（受旋转影响）
        //   例如向右转 90 度后，Translate(0,0,1) 会沿原来 X 方向移动
        //
        // - Space.World：沿世界坐标轴移动（不受旋转影响）
        //   无论朝向如何，Translate(0,0,1, Space.World) 总是沿世界 Z 轴
        //
        // 【实现原理】
        // Space.Self 时：position += TransformDirection(translation)
        //   将局部方向的 translation 转换到世界方向再移动
        //
        // Space.World 时：position += translation
        //   直接在世界空间中移动
        // ================================================================

        // 沿指定空间（Self/World）移动指定距离
        public void Translate(Vector3 translation, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            if (relativeTo == Space.World)
                position += translation;
            else
                position += TransformDirection(translation);
        }

        // 默认沿局部坐标轴移动
        public void Translate(Vector3 translation)
        {
            Translate(translation, Space.Self);
        }

        // 沿指定空间移动（分量形式）
        public void Translate(float x, float y, float z, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            Translate(new Vector3(x, y, z), relativeTo);
        }

        // 默认沿局部坐标轴移动（分量形式）
        public void Translate(float x, float y, float z)
        {
            Translate(new Vector3(x, y, z), Space.Self);
        }

        // 沿指定 Transform 的局部轴移动
        // 如果 relativeTo 为 null，则沿世界坐标轴移动
        public void Translate(Vector3 translation, Transform relativeTo)
        {
            if (relativeTo)
                position += relativeTo.TransformDirection(translation);
            else
                position += translation;
        }

        // 沿指定 Transform 的局部轴移动（分量形式）
        public void Translate(float x, float y, float z, Transform relativeTo)
        {
            Translate(new Vector3(x, y, z), relativeTo);
        }

        // ================================================================
        // Rotate - 旋转 Transform
        // ================================================================
        // 应用旋转到 Transform。
        //
        // 【旋转的两种形式】
        //
        // 1. 欧拉角旋转：Rotate(eulerAngles, relativeTo)
        //    按 Z → X → Y 的顺序应用欧拉角
        //
        // 2. 轴角旋转：Rotate(axis, angle, relativeTo)
        //    绕指定轴旋转指定角度
        //
        // 【Space.Self vs Space.World 的区别（欧拉角版本）】
        // - Space.Self：localRotation = localRotation * eulerRot
        //   在局部空间中旋转，相当于在自身坐标中旋转
        //
        // - Space.World：rotation = rotation * (Inverse(rotation) * eulerRot * rotation)
        //   先转换到世界空间再旋转
        //
        // 【Space.Self vs Space.World 的区别（轴角版本）】
        // - Space.Self：将局部轴转换为世界轴再调用内部旋转
        // - Space.World：直接使用世界轴调用内部旋转
        // ================================================================

        // 按欧拉角旋转，可指定参考坐标系（Space.Self / Space.World）
        public void Rotate(Vector3 eulers, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            Quaternion eulerRot = Quaternion.Euler(eulers.x, eulers.y, eulers.z);
            if (relativeTo == Space.Self)
                localRotation = localRotation * eulerRot;
            else
            {
                rotation = rotation * (Quaternion.Inverse(rotation) * eulerRot * rotation);
            }
        }

        // 默认沿局部坐标轴旋转
        public void Rotate(Vector3 eulers)
        {
            Rotate(eulers, Space.Self);
        }

        // 按欧拉角旋转（分量形式）
        public void Rotate(float xAngle, float yAngle, float zAngle, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            Rotate(new Vector3(xAngle, yAngle, zAngle), relativeTo);
        }

        // 默认沿局部坐标轴旋转（分量形式）
        public void Rotate(float xAngle, float yAngle, float zAngle)
        {
            Rotate(new Vector3(xAngle, yAngle, zAngle), Space.Self);
        }

        // 内部轴角旋转方法（C++ 原生实现）
        [NativeMethod("RotateAround")]
        internal extern void RotateAroundInternal(Vector3 axis, float angle);

        // 绕指定轴旋转指定角度
        // Space.Self: axis 是局部坐标轴
        // Space.World: axis 是世界坐标轴
        public void Rotate(Vector3 axis, float angle, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            if (relativeTo == Space.Self)
                RotateAroundInternal(transform.TransformDirection(axis), angle * Mathf.Deg2Rad);
            else
                RotateAroundInternal(axis, angle * Mathf.Deg2Rad);
        }

        // 默认绕局部轴旋转
        public void Rotate(Vector3 axis, float angle)
        {
            Rotate(axis, angle, Space.Self);
        }

        // ================================================================
        // RotateAround - 绕空间中的某一点和轴旋转
        // ================================================================
        // 绕通过世界坐标中 point 点的 axis 轴旋转 angle 度。
        //
        // 【应用场景】
        // - 行星公转效果（绕另一物体旋转）
        // - 相机围绕目标旋转
        // - 物体围绕关卡中的特定点旋转
        //
        // 【实现原理】
        // 1. 计算从旋转中心到物体的向量：dif = position - point
        // 2. 用角度轴四元数旋转这个向量：dif = q * dif
        // 3. 计算新位置：position = point + dif
        // 4. 同时旋转物体的自身朝向：RotateAroundInternal(axis, angle)
        //
        // 【示例】
        // transform.RotateAround(Vector3.zero, Vector3.up, 10 * Time.deltaTime);
        // 使物体绕原点（0,0,0）的 Y 轴旋转，类似公转效果
        // ================================================================
        public void RotateAround(Vector3 point, Vector3 axis, float angle)
        {
            Vector3 worldPos = position;
            Quaternion q = Quaternion.AngleAxis(angle, axis);
            Vector3 dif = worldPos - point;
            dif = q * dif;
            worldPos = point + dif;
            position = worldPos;
            RotateAroundInternal(axis, angle * Mathf.Deg2Rad);
        }

        // ================================================================
        // LookAt - 让物体朝向目标
        // ================================================================
        // 旋转 Transform，使 forward（蓝色 Z 轴）指向目标位置。
        //
        // 【重载版本】
        // - LookAt(Transform target)：看向另一个 Transform
        // - LookAt(Vector3 worldPosition)：看向世界坐标中的点
        // - LookAt(target, worldUp)：指定上方向
        //
        // 【参数说明】
        // - target/worldPosition：目标位置，让 forward 指向这里
        // - worldUp：指定哪个方向是"上"（默认 Vector3.up）
        //   当需要让物体倾斜着看向目标时使用自定义 worldUp
        //
        // 【使用场景】
        // - 相机始终看向角色
        // - 炮塔瞄准目标
        // - NPC 面向玩家说话
        //
        // 【注意】
        // - 如果目标与自身位置重合，行为未定义（旋转会异常）
        // - worldUp 参数不会改变物体绕 forward 轴的自转角度
        //   （Z 轴始终指向目标，但 Y 轴会尽量对齐 worldUp）
        //
        // 【实现原理】
        // 内部使用 Quaternion.LookRotation(target - position, worldUp)
        // ================================================================

        // 看向另一个 Transform 对象
        public void LookAt(Transform target, [UnityEngine.Internal.DefaultValue("Vector3.up")] Vector3 worldUp) { if (target) LookAt(target.position, worldUp); }
        public void LookAt(Transform target) { if (target) LookAt(target.position, Vector3.up); }

        // 看向世界空间中的某个位置
        public void LookAt(Vector3 worldPosition, [UnityEngine.Internal.DefaultValue("Vector3.up")] Vector3 worldUp) { Internal_LookAt(worldPosition, worldUp); }
        public void LookAt(Vector3 worldPosition) { Internal_LookAt(worldPosition, Vector3.up); }

        [FreeFunction("Internal_LookAt", HasExplicitThis = true)]
        private extern void Internal_LookAt(Vector3 worldPosition, Vector3 worldUp);

        // ================================================================
        // TransformDirection - 方向从局部空间 → 世界空间
        // ================================================================
        // 将方向向量从局部空间转换到世界空间。
        //
        // 【与 TransformPoint / TransformVector 的区别】
        //
        // ┌─────────────────┬──────────┬──────────┬──────────┐
        // │     方法        │ 受旋转影响 │ 受缩放影响 │ 受平移影响 │
        // ├─────────────────┼──────────┼──────────┼──────────┤
        // │ TransformDirection │   ✅    │    ❌    │    ❌    │
        // │ TransformVector    │   ✅    │    ✅    │    ❌    │
        // │ TransformPoint     │   ✅    │    ✅    │    ✅    │
        // └─────────────────┴──────────┴──────────┴──────────┘
        //
        // TransformDirection：只应用旋转，不包含缩放和平移。
        // 适用于表示"方向"的向量（如速度方向、法线方向）。
        //
        // 【示例】
        // 物体旋转 90 度后，TransformDirection(Vector3.forward)
        // 返回物体的前方向在世界坐标中的表示。
        // ================================================================
        public extern Vector3 TransformDirection(Vector3 direction);

        // 局部方向 → 世界方向（分量形式）
        public Vector3 TransformDirection(float x, float y, float z) { return TransformDirection(new Vector3(x, y, z)); }

        // 批量方向转换：局部空间 → 世界空间（高性能批量版本）
        [NativeMethod(Name = "TransformDirections")]
        internal unsafe extern void TransformDirectionsInternal(ReadOnlySpan<Vector3> directions, Span<Vector3> transformedDirections);
        public unsafe void TransformDirections(ReadOnlySpan<Vector3> directions, Span<Vector3> transformedDirections)
        {
            if (directions.Length != transformedDirections.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.TransformDirections() must be the same length");

           TransformDirectionsInternal(directions, transformedDirections);
        }
        public unsafe void TransformDirections(Span<Vector3> directions)
        {
            TransformDirectionsInternal(directions, directions);
        }


        // ================================================================
        // InverseTransformDirection - 方向从世界空间 → 局部空间
        // ================================================================
        // 将方向向量从世界空间转换到局部空间。
        // 是 TransformDirection 的逆操作。
        //
        // 同 TransformDirection 一样，只受旋转影响，不受缩放/平移影响。
        // ================================================================
        public extern Vector3 InverseTransformDirection(Vector3 direction);

        // 世界方向 → 局部方向（分量形式）
        public Vector3 InverseTransformDirection(float x, float y, float z) { return InverseTransformDirection(new Vector3(x, y, z)); }

        // 批量方向逆转换：世界空间 → 局部空间
        [NativeMethod(Name = "InverseTransformDirections")]
        internal unsafe extern void InverseTransformDirectionsInternal(ReadOnlySpan<Vector3> directions, Span<Vector3> transformedDirections);
        public unsafe void InverseTransformDirections(ReadOnlySpan<Vector3> directions, Span<Vector3> transformedDirections)
        {
            if (directions.Length != transformedDirections.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.InverseTransformDirections() must be the same length");

            InverseTransformDirectionsInternal(directions, transformedDirections);
        }
        public unsafe void InverseTransformDirections(Span<Vector3> directions)
        {
            InverseTransformDirectionsInternal(directions, directions);
        }


        // ================================================================
        // TransformVector - 向量从局部空间 → 世界空间
        // ================================================================
        // 将向量从局部空间转换到世界空间。
        //
        // 【与 TransformDirection 的区别】
        // - TransformVector 同时受旋转和缩放影响（拉伸效果）
        // - TransformDirection 只受旋转影响（忽略缩放）
        //
        // 【与 TransformPoint 的区别】
        // - TransformVector 不受平移影响（纯向量，无位置成分）
        // - TransformPoint 受平移影响（有位置成分）
        //
        // 【适用场景】
        // - 局部空间中定义的位移量或力
        // - 需要正确反映缩放效果的向量
        // ================================================================
        public extern Vector3 TransformVector(Vector3 vector);

        // 局部向量 → 世界向量（分量形式）
        public Vector3 TransformVector(float x, float y, float z) { return TransformVector(new Vector3(x, y, z)); }

        // 批量向量转换：局部空间 → 世界空间
        [NativeMethod(Name = "TransformVectors")]
        internal unsafe extern void TransformVectorsInternal(ReadOnlySpan<Vector3> vectors, Span<Vector3> transformedVectors);
        public unsafe void TransformVectors(ReadOnlySpan<Vector3> vectors, Span<Vector3> transformedVectors)
        {
            if (vectors.Length != transformedVectors.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.TransformVectors() must be the same length");

            TransformVectorsInternal(vectors, transformedVectors);
        }
        public unsafe void TransformVectors(Span<Vector3> vectors)
        {
            TransformVectorsInternal(vectors, vectors);
        }


        // ================================================================
        // InverseTransformVector - 向量从世界空间 → 局部空间
        // ================================================================
        // 将向量从世界空间转换到局部空间。
        // 是 TransformVector 的逆操作。
        //
        // 同时受旋转和缩放影响（包含逆缩放和逆旋转）。
        // ================================================================
        public extern Vector3 InverseTransformVector(Vector3 vector);

        // 世界向量 → 局部向量（分量形式）
        public Vector3 InverseTransformVector(float x, float y, float z) { return InverseTransformVector(new Vector3(x, y, z)); }

        // 批量向量逆转换：世界空间 → 局部空间
        [NativeMethod(Name = "InverseTransformVectors")]
        internal unsafe extern void InverseTransformVectorsInternal(ReadOnlySpan<Vector3> vectors, Span<Vector3> transformedVectors);
        public unsafe void InverseTransformVectors(ReadOnlySpan<Vector3> vectors, Span<Vector3> transformedVectors)
        {
            if (vectors.Length != transformedVectors.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.InverseTransformVectors() must be the same length");

            InverseTransformVectorsInternal(vectors, transformedVectors);
        }
        public unsafe void InverseTransformVectors(Span<Vector3> vectors)
        {
            InverseTransformVectorsInternal(vectors, vectors);
        }


        // ================================================================
        // TransformPoint - 位置从局部空间 → 世界空间
        // ================================================================
        // 将位置点从局部空间转换到世界空间。
        //
        // 【完整变换过程】
        // 应用所有父级的 旋转 → 缩放 → 平移，与 Transform 的层级运算一致。
        //
        // 【与 TransformDirection / TransformVector 的区别】
        // 见 TransformDirection 处的对照表。
        //
        // 【常见场景】
        // - 获取子物体的世界坐标
        // - 获取挂载点的世界位置
        // - 射线检测的起点计算
        // ================================================================
        public extern Vector3 TransformPoint(Vector3 position);

        // 局部位置 → 世界位置（分量形式）
        public Vector3 TransformPoint(float x, float y, float z) { return TransformPoint(new Vector3(x, y, z)); }

        // 批量位置转换：局部空间 → 世界空间
        [NativeMethod(Name = "TransformPoints")]
        internal unsafe extern void TransformPointsInternal(ReadOnlySpan<Vector3> positions, Span<Vector3> transformedPositions);
        public unsafe void TransformPoints(ReadOnlySpan<Vector3> positions, Span<Vector3> transformedPositions)
        {
            if (positions.Length != transformedPositions.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.TransformPoints() must be the same length");

            TransformPointsInternal(positions, transformedPositions);
        }
        public unsafe void TransformPoints(Span<Vector3> positions)
        {
            TransformPointsInternal(positions, positions);
        }


        // ================================================================
        // InverseTransformPoint - 位置从世界空间 → 局部空间
        // ================================================================
        // 将位置点从世界空间转换到局部空间。
        // 是 TransformPoint 的逆操作。
        //
        // 【注意】
        // 如果只需要方向（不受位置影响），请使用 InverseTransformDirection。
        // 如果只需要向量（受缩放影响但不受位置影响），请使用 InverseTransformVector。
        // ================================================================
        public extern Vector3 InverseTransformPoint(Vector3 position);

        // 世界位置 → 局部位置（分量形式）
        public Vector3 InverseTransformPoint(float x, float y, float z) { return InverseTransformPoint(new Vector3(x, y, z)); }

        // 批量位置逆转换：世界空间 → 局部空间
        [NativeMethod(Name = "InverseTransformPoints")]
        internal unsafe extern void InverseTransformPointsInternal(ReadOnlySpan<Vector3> positions, Span<Vector3> transformedPositions);
        public unsafe void InverseTransformPoints(ReadOnlySpan<Vector3> positions, Span<Vector3> transformedPositions)
        {
            if (positions.Length != transformedPositions.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.InverseTransformPoints() must be the same length");

            InverseTransformPointsInternal(positions, transformedPositions);
        }
        public unsafe void InverseTransformPoints(Span<Vector3> positions)
        {
            InverseTransformPoints(positions, positions);
        }


        // ================================================================
        // root - 获取层级树的根 Transform
        // ================================================================
        // 返回场景层级树中最顶层的根 Transform。
        //
        // 【说明】
        // - 如果当前 Transform 没有父级，则返回自身
        // - 遍历父级直到最顶层
        // - 根 Transform 的 parent == null
        //
        // 【用途】
        // - 快速获取场景中某个子物体的根对象
        // - 判断是否为场景根级对象
        // ================================================================
        public Transform root { get { return GetRoot(); } }

        private extern Transform GetRoot();

        // ================================================================
        // childCount - 子物体数量
        // ================================================================
        // 返回当前 Transform 的直接子物体数量（不包含孙子级）。
        //
        // 【使用示例】
        // for (int i = 0; i < transform.childCount; i++)
        // {
        //     Transform child = transform.GetChild(i);
        //     // 处理子物体...
        // }
        // ================================================================
        public extern int childCount
        {
            [NativeMethod("GetChildrenCount")]
            get;
        }

        // ================================================================
        // DetachChildren - 解除所有子物体
        // ================================================================
        // 将所有子级从此 Transform 解除父子关系，使其成为场景根对象。
        // 子级的世界位置保持不变。
        //
        // 【使用场景】
        // - 角色死亡时掉落所有装备
        // - 拆分组合体对象
        // ================================================================
        [FreeFunction("DetachChildren", HasExplicitThis = true)]
        public extern void DetachChildren();

        // ================================================================
        // SetAsFirstSibling / SetAsLastSibling - 调整同级顺序
        // ================================================================
        // SetAsFirstSibling：将自身移到父级子数组的最前面（最底下渲染）
        // SetAsLastSibling：将自身移到父级子数组的最后面（最上面渲染）
        //
        // 在 Hierarchy 窗口中，第一个子级显示在最上方。
        // 这个顺序会影响渲染顺序（在同一深度级别下）。
        // ================================================================

        // 将自身移到父级子数组的开头（在 Hierarchy 中显示在最上面）
        public extern void SetAsFirstSibling();

        // 将自身移到父级子数组的末尾（在 Hierarchy 中显示在最下面）
        public extern void SetAsLastSibling();

        // ================================================================
        // SetSiblingIndex - 设置同级索引位置
        // ================================================================
        // 设置当前 Transform 在父级子数组中的索引位置。
        // index 范围从 0（第一个子级）到 childCount - 1（最后一个子级）。
        // ================================================================
        public extern void SetSiblingIndex(int index);

        // 内部方法：将当前 Transform 移动到指定 Transform 之后
        [NativeMethod("MoveAfterSiblingInternal")]
        internal extern void MoveAfterSibling(Transform transform, bool notifyEditorAndMarkDirty);

        // ================================================================
        // GetSiblingIndex - 获取同级索引位置
        // ================================================================
        // 返回当前 Transform 在父级子数组中的索引。
        // 从 0 开始计数。
        // ================================================================
        public extern int GetSiblingIndex();

        [FreeFunction(HasExplicitThis = true)]
        private extern Transform FindRelativeTransformWithPath(string path, [UnityEngine.Internal.DefaultValue("false")] bool isActiveOnly);

        // ================================================================
        // Find - 通过名称查找子物体
        // ================================================================
        // 按名称查找子级 Transform（支持路径式查找）。
        //
        // 【查找规则】
        // - 按名称递归查找所有子级（深度优先）
        // - 支持 "/" 分隔的路径格式，如 "Arm/Hand/Finger"
        // - 路径以 "/" 开头时从根级开始查找
        // - 区分大小写
        //
        // 【性能提示】
        // - Find 是递归遍历，性能开销较大
        // - 在 Awake/Start 中缓存查找结果，避免每帧调用
        // - 频繁查找应使用引用缓存而非每次 Find
        //
        // 【与 GameObject.Find 的区别】
        // - Transform.Find：只在当前 Transform 的子级中查找（不包含自身）
        // - GameObject.Find：在整个场景中查找（包含非激活对象）
        //
        // 【注意】
        // - name 参数不能为 null（会抛出 ArgumentNullException）
        // - 找不到时返回 null
        // - 不查找非激活（inactive）的子物体
        // ================================================================
        public Transform Find(string n)
        {
            if (n == null)
                throw new ArgumentNullException("Name cannot be null");
            return FindRelativeTransformWithPath(n, false);
        }

        //*undocumented
        [NativeConditional("UNITY_EDITOR")]
        internal extern void SendTransformChangedScale();

        // ================================================================
        // lossyScale - 世界空间中的全局缩放（只读）
        // ================================================================
        // 获取世界空间中的最终缩放值。
        //
        // 【localScale vs lossyScale】
        //
        // - localScale：对象自身的缩放，不考虑父级
        // - lossyScale：结合所有父级缩放后的最终世界缩放（只读）
        //
        // 【示例】
        // 父级 localScale = (2, 2, 2)
        // 子级 localScale = (3, 3, 3)
        // 子级 lossyScale = (6, 6, 6)  ← 父级 × 子级
        //
        // 【注意】
        // - lossyScale 为只读属性，不可设置
        // - 当层级中存在非均匀缩放（Uneven Scale）时，lossyScale 的计算
        //   不完全准确（受旋转与缩放的耦合影响）
        // - 如果有旋转 + 非均匀缩放，实际变换矩阵不是简单的 scale 相乘
        // ================================================================
        public extern Vector3 lossyScale
        {
            [NativeMethod("GetWorldScaleLossy")]
            get;
        }

        // ================================================================
        // IsChildOf - 判断是否为指定 Transform 的子级
        // ================================================================
        // 检查当前 Transform 是否是 parent 的子级（或自身）。
        //
        // 【注意】
        // - 如果 parent 是当前 transform 自身，也返回 true
        // - 递归检查所有祖先级别
        // - 如果 parent 为 null，返回 false
        // ================================================================
        [FreeFunction("Internal_IsChildOrSameAsOtherTransform", HasExplicitThis = true)]
        public extern bool IsChildOf([NotNull] Transform parent);

        // ================================================================
        // hasChanged - 变换变更检测
        // ================================================================
        // 获取/设置 Transform 是否发生过变化的标志。
        //
        // 【行为说明】
        // - Unity 在每帧更新 Transform 数据时将此标志置为 true
        // - 用户代码可以手动将其设为 false 来重置
        // - 当 Transform 的 position/rotation/scale 被修改时自动设为 true
        //
        // 【使用场景】
        // - 缓存系统：检测 Transform 是否变化以决定是否需要更新缓存
        // - 自定义动画系统：检测外部修改
        //
        // 【注意】
        // - 不会检测子级的变化（子级变化不会设置父级的 hasChanged）
        // - 在 LateUpdate 中重置，在下一帧的 Update 中检查
        // ================================================================
        [NativeProperty("HasChangedDeprecated")]
        public extern bool hasChanged { get; set; }

        //*undocumented*
        [Obsolete("FindChild has been deprecated. Use Find instead (UnityUpgradable) -> Find([mscorlib] System.String)", false)]
        public Transform FindChild(string n) { return Find(n); }

        //*undocumented* Documented separately
        public IEnumerator GetEnumerator()
        {
            return new Transform.Enumerator(this);
        }

        // ================================================================
        // Enumerator - Transform 子级遍历枚举器
        // ================================================================
        // 支持 foreach 遍历 Transform 的所有直接子级：
        //
        // foreach (Transform child in transform)
        // {
        //     // 处理子物体
        // }
        //
        // 等价于：
        // for (int i = 0; i < transform.childCount; i++)
        //     Process(transform.GetChild(i));
        // ================================================================
        private class Enumerator : IEnumerator
        {
            Transform outer;
            int currentIndex = -1;

            internal Enumerator(Transform outer)
            {
                this.outer = outer;
            }

            //*undocumented*
            public object Current
            {
                get { return outer.GetChild(currentIndex); }
            }

            //*undocumented*
            public bool MoveNext()
            {
                int childCount = outer.childCount;
                return ++currentIndex < childCount;
            }

            //*undocumented*
            public void Reset() { currentIndex = -1; }
        }

        // ================================================================
        // 已废弃的方法
        // ================================================================

        // *undocumented* DEPRECATED - 改用 Transform.Rotate(eulerAngles)
        [Obsolete("warning use Transform.Rotate instead.")]
        public extern void RotateAround(Vector3 axis, float angle);

        // *undocumented* DEPRECATED - 改用 Transform.Rotate(axis, angle)
        [Obsolete("warning use Transform.Rotate instead.")]
        public extern void RotateAroundLocal(Vector3 axis, float angle);

        // ================================================================
        // GetChild - 按索引获取子级 Transform
        // ================================================================
        // 返回指定索引处的子级 Transform。
        //
        // 【参数】
        // - index：子级索引，范围 0 ~ childCount - 1
        //
        // 【异常】
        // - 参数越界时抛出异常（ThrowsException = true）
        //
        // 【注意】
        // - 子级顺序与 Hierarchy 窗口中的显示顺序一致
        // - 频繁调用 GetChild 有性能开销，建议缓存
        // ================================================================
        [FreeFunction("GetChild", HasExplicitThis = true, ThrowsException = true)]
        public extern Transform GetChild(int index);

        //*undocumented* DEPRECATED - 改用 Transform.childCount
        [Obsolete("warning use Transform.childCount instead (UnityUpgradable) -> Transform.childCount", false)]
        [NativeMethod("GetChildrenCount")]
        public extern int GetChildCount();

        // ================================================================
        // hierarchyCapacity / hierarchyCount - 层级缓存容量与层级深度
        // ================================================================
        //
        // hierarchyCapacity：
        //   预分配层级缓存大小，优化频繁增删子级时的性能。
        //   适用于已知子级数量的场景。
        //
        // hierarchyCount：
        //   返回包含自身在内的所有祖先层级深度。
        //   根级 Transform 的 hierarchyCount 为 1。
        // ================================================================
        public int hierarchyCapacity
        {
            get { return internal_getHierarchyCapacity(); }
            set { internal_setHierarchyCapacity(value); }
        }

        [FreeFunction("GetHierarchyCapacity", HasExplicitThis = true)]
        private extern int internal_getHierarchyCapacity();

        [FreeFunction("SetHierarchyCapacity", HasExplicitThis = true)]
        private extern void internal_setHierarchyCapacity(int value);

        public int hierarchyCount { get { return internal_getHierarchyCount(); } }

        [FreeFunction("GetHierarchyCount", HasExplicitThis = true)]
        private extern int internal_getHierarchyCount();

        // 判断是否为非均匀缩放（编辑器专用）
        // 非均匀缩放时子级可能出现切变效果
        [NativeConditional("UNITY_EDITOR")]
        [FreeFunction("IsNonUniformScaleTransform", HasExplicitThis = true)]
        internal extern bool IsNonUniformScaleTransform();

        // ================================================================
        // constrainProportionsScale - 等比缩放锁定（编辑器专用）
        // ================================================================
        // 在 Inspector 中锁定缩放比例，使 X/Y/Z 保持等比缩放。
        // 开启后修改任何一个轴的缩放值，其他轴会跟随变化以保持比例。
        // 仅在 Unity 编辑器中有效。
        // ================================================================
        [NativeConditional("UNITY_EDITOR")]
        internal bool constrainProportionsScale
        {
            get => IsConstrainProportionsScale();
            set => SetConstrainProportionsScale(value);
        }

        [NativeConditional("UNITY_EDITOR")]
        private extern void SetConstrainProportionsScale(bool isLinked);

        [NativeConditional("UNITY_EDITOR")]
        private extern bool IsConstrainProportionsScale();
    }
}
