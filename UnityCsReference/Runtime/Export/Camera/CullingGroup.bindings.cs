// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 CullingGroup — 自定义裁剪分组 API
//
// 📌 作用：
//   CullingGroup 允许开发者创建自定义的物体裁剪系统，
//   而不依赖 Unity 内置的 Camera 裁剪。常用于：
//   - 大世界中的 AI 实体激活/休眠
//   - 粒子系统的距离分级管理
//   - 自定义的 LOD 切换逻辑
//
// 🏗 核心概念：
//
//   【BoundingSphere】
//   - 每个被裁剪的对象用一个包围球表示（position + radius）
//   - 通过 SetBoundingSpheres 一次性设置所有对象
//
//   【距离区间】
//   - SetBoundingDistances：定义距离区间（如 [10, 50, 200]）
//   - 每个对象被分配到一个距离区间（distanceIndex）
//
//   【状态变更回调】
//   - onStateChanged：当对象可见性或距离区间变化时触发
//   - CullingGroupEvent 提供：index（对象索引）、isVisible、wasVisible
//   - hasBecomeVisible / hasBecomeInvisible：状态转换检测
//
//   【查询】
//   - QueryIndices：查询指定可见性/距离区间内的对象列表
//   - IsVisible / GetDistance：查询单个对象状态
//
// 💡 理解关键：
//   - CullingGroup 与 Camera 挂钩，使用 Camera 的视锥体进行裁剪
//   - SetDistanceReferencePoint 定义距离计算的参考点（通常设为主相机）
//   - EraseSwapBack 使用交换删除策略保持数组紧凑
//
// 📍 对应 C++ 头文件：Runtime/Export/Camera/CullingGroup.bindings.h
// ==============================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // ==============================================================
    // BoundingSphere — 包围球结构体
    // 🎯 用 position + radius 描述一个被裁剪对象的空间范围
    // ==============================================================
    public struct BoundingSphere
    {
        public Vector3    position;
        public float      radius;

        public BoundingSphere(Vector3 pos, float rad) { position = pos; radius = rad; }
        public BoundingSphere(Vector4 packedSphere) { position = new Vector3(packedSphere.x, packedSphere.y, packedSphere.z); radius = packedSphere.w; }
    }

    // ==============================================================
    // CullingQueryOptions — 裁剪查询选项枚举
    // 🎯 控制 QueryIndices 时的过滤维度（可见性/距离/两者）
    // ==============================================================
    internal enum CullingQueryOptions
    {
        Normal = 0,
        IgnoreVisibility = 1,
        IgnoreDistance = 2
    }

    // ==============================================================
    // CullingGroupEvent — 裁剪状态变更事件结构体
    // 🎯 封装对象的可见性变化和距离区间变化信息
    // ==============================================================
    public struct CullingGroupEvent
    {
        #pragma warning disable 649
        private int m_Index;
        private byte m_PrevState;
        private byte m_ThisState;

        public int index { get { return m_Index; } }

        private const byte kIsVisibleMask = 1 << 7;
        private const byte kDistanceMask = (1 << 7) - 1;

        public bool isVisible             { get { return (m_ThisState & kIsVisibleMask) != 0; } }
        public bool wasVisible            { get { return (m_PrevState & kIsVisibleMask) != 0; } }

        public bool hasBecomeVisible      { get { return isVisible && !wasVisible; } }
        public bool hasBecomeInvisible    { get { return !isVisible && wasVisible; } }

        public int currentDistance        { get { return m_ThisState & kDistanceMask; } }
        public int previousDistance       { get { return m_PrevState & kDistanceMask; } }
    }

    // ==============================================================
    // CullingGroup — 自定义裁剪分组管理类
    //
    // 🔑 核心属性与方法：
    //   - onStateChanged：可见性/距离变化回调（StateChanged 委托）
    //   - targetCamera：关联的裁剪摄像机
    //   - SetBoundingSpheres()：设置被管理的包围球数组
    //   - SetBoundingDistances()：设置距离区间
    //   - QueryIndices()：按可见性/距离查询对象
    //   - IsVisible() / GetDistance()：单对象查询
    //   - SetDistanceReferencePoint()：距离计算参考点
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Export/Camera/CullingGroup.bindings.h")]
    public class CullingGroup : IDisposable
    {
        internal IntPtr m_Ptr;

        public delegate void StateChanged(CullingGroupEvent sphere);

        public CullingGroup()
        {
            m_Ptr = Init(this);
        }

        ~CullingGroup()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                FinalizerFailure();
            }
        }

        [FreeFunction("CullingGroup_Bindings::Dispose", HasExplicitThis = true)]
        extern private void DisposeInternal();

        public void Dispose()
        {
            DisposeInternal();
            m_Ptr = IntPtr.Zero;
        }

        public StateChanged onStateChanged
        {
            get { return m_OnStateChanged; }
            set { m_OnStateChanged = value; }
        }

        extern public bool enabled { get; set; }
        extern public Camera targetCamera { get; set; }

        extern public void SetBoundingSpheres([UnityMarshalAs(NativeType.ScriptingObjectPtr)]BoundingSphere[] array);
        extern public void SetBoundingSphereCount(int count);
        extern public void EraseSwapBack(int index);


        public static void EraseSwapBack<T>(int index, T[] myArray, ref int size)
        {
            size--;
            myArray[index] = myArray[size];
        }

        public int QueryIndices(bool visible, int[] result, int firstIndex)
        {
            return QueryIndices(visible, -1, CullingQueryOptions.IgnoreDistance, result, firstIndex);
        }

        public int QueryIndices(int distanceIndex, int[] result, int firstIndex)
        {
            return QueryIndices(false, distanceIndex, CullingQueryOptions.IgnoreVisibility, result, firstIndex);
        }

        public int QueryIndices(bool visible, int distanceIndex, int[] result, int firstIndex)
        {
            return QueryIndices(visible, distanceIndex, CullingQueryOptions.Normal, result, firstIndex);
        }

        [FreeFunction("CullingGroup_Bindings::QueryIndices", HasExplicitThis = true, ThrowsException = true)]
        extern private int QueryIndices(bool visible, int distanceIndex, CullingQueryOptions options, int[] result, int firstIndex);

        [FreeFunction("CullingGroup_Bindings::IsVisible", HasExplicitThis = true, ThrowsException = true)]
        extern public bool IsVisible(int index);

        [FreeFunction("CullingGroup_Bindings::GetDistance", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetDistance(int index);

        [FreeFunction("CullingGroup_Bindings::SetBoundingDistances", HasExplicitThis = true)]
        extern public void SetBoundingDistances(float[] distances);

        [FreeFunction("CullingGroup_Bindings::SetDistanceReferencePoint", HasExplicitThis = true)]
        extern private void SetDistanceReferencePoint_InternalVector3(Vector3 point);

        [NativeMethod("SetDistanceReferenceTransform")]
        extern private void SetDistanceReferencePoint_InternalTransform(Transform transform);

        public void SetDistanceReferencePoint(Vector3 point)
        {
            SetDistanceReferencePoint_InternalVector3(point);
        }

        public void SetDistanceReferencePoint(Transform transform)
        {
            SetDistanceReferencePoint_InternalTransform(transform);
        }

        // private

        private StateChanged m_OnStateChanged = null;

        [System.Security.SecuritySafeCritical]
        [RequiredByNativeCode]
        unsafe private static void SendEvents(CullingGroup cullingGroup, IntPtr eventsPtr, int count)
        {
            CullingGroupEvent* events = (CullingGroupEvent*)eventsPtr.ToPointer();
            if (cullingGroup.m_OnStateChanged == null)
                return;

            for (int i = 0; i < count; ++i)
                cullingGroup.m_OnStateChanged(events[i]);
        }

        [FreeFunction("CullingGroup_Bindings::Init")]
        extern private static IntPtr Init(object scripting);

        [FreeFunction("CullingGroup_Bindings::FinalizerFailure", HasExplicitThis = true, IsThreadSafe = true)]
        extern private void FinalizerFailure();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(CullingGroup cullingGroup) => cullingGroup.m_Ptr;
        }
    }
}
