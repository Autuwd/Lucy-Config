// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Reflection;
using UnityEngine.Bindings;

namespace UnityEngine.Assemblies;

//=============================================================================
// 📌 CurrentAssemblies —— 当前程序集查询入口
//
// 设计说明:
//   此类作为当前已加载程序集的查询入口（目前为 stub 阶段）。
//   后续版本会在此类中提供获取/筛选当前 AppDomain 中已加载程序集的功能。
//
// 🎯 未来扩展方向:
//   提供类似 Assembly.GetExecutingAssembly() 的便捷访问方式，
//   以及程序集热重载后的状态追踪。
//=============================================================================

public static partial class CurrentAssemblies
{
}
