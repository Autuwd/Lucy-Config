// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 FrameRate — 精确帧率表示
//
// 📌 作用：
//   FrameRate 用于精确表示和计算帧率，支持标准帧率
//   和 NTSC droppped frame 帧率（NTSC 色彩副载波导致的
//   微小偏差，如 29.97fps、23.976fps）。
//
// 💡 存储方式（巧妙的 int 编码）：
//   内部使用 int m_Rate 存储，正数 = 整数帧率，
//   负数 = drop frame 帧率。例如：
//     m_Rate = 30    →  30 fps（整数帧率）
//     m_Rate = -30   →  29.97 fps（drop frame）
//   实际值计算：dropFrame → |m_Rate| * (1000.0 / 1001.0)
//
// 🔑 预定义常量：
//   k_24Fps / k_23_976Fps — 电影标准
//   k_30Fps / k_29_97Fps — NTSC 广播标准
//   k_60Fps / k_59_94Fps — 高帧率 / NTSC 60i
//   k_25Fps / k_50Fps — PAL 标准
//
// 💡 使用场景：
//   PlayableGraph.EnableMatchFrameRate(frameRate) 启用帧率匹配，
//   使图按指定的帧率进行更新，用于精确同步播放。
//
// 📍 对应 C++ 头文件：Runtime/Director/Core/FrameRate.h
// ==============================================================

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Playables
{
    // ==============================================================
    // FrameRate — 精确帧率表示
    //
    // 🔑 关键字段：
    //   m_Rate (int) — 编码后的帧率值。正数 = 整数帧率，
    //                  负数 = drop frame（NTSC 兼容帧率）
    //
    // 🔑 预定义常量：
    //   电影：  k_24Fps / k_23_976Fps
    //   PAL：   k_25Fps / k_50Fps
    //   NTSC：  k_30Fps / k_29_97Fps
    //   高帧率：k_60Fps / k_59_94Fps
    //
    // 💡 drop frame 编码原理：
    //   NTSC 彩色电视标准引入了 1000/1001 的帧率缩放因子。
    //   例如 30fps 的实际 NTSC 帧率是 30 * 1000/1001 ≈ 29.97fps。
    //   FrameRate 用负号标记这种"drop frame"模式，
    //   计算时自动应用缩放因子。
    //
    // 💡 使用场景：
    //   PlayableGraph.EnableMatchFrameRate() 的帧率参数，
    //   确保图更新与目标帧率精确同步。
    // ==============================================================
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode("FrameRate")]
    [NativeHeader("Runtime/Director/Core/FrameRate.h")]
    [VisibleToOtherModules("UnityEngine.DirectorModule")]
    internal struct FrameRate : IEquatable<FrameRate>, IFormattable
    {
        [Ignore] public static readonly FrameRate k_24Fps = new FrameRate(24U, false);
        [Ignore] public static readonly FrameRate k_23_976Fps = new FrameRate(24U, true);
        [Ignore] public static readonly FrameRate k_25Fps = new FrameRate(25U, false);

        [Ignore] public static readonly FrameRate k_30Fps = new FrameRate(30U, false);
        [Ignore] public static readonly FrameRate k_29_97Fps = new FrameRate(30U, true);

        [Ignore] public static readonly FrameRate k_50Fps = new FrameRate(50U, false);
        [Ignore] public static readonly FrameRate k_60Fps = new FrameRate(60U, false);
        [Ignore] public static readonly FrameRate k_59_94Fps = new FrameRate(60U, true);

        [SerializeField]
        int m_Rate;

        // 🎯 dropFrame 属性：负值 = drop frame 模式（NTSC 兼容）
        public bool dropFrame => m_Rate < 0;
        // 🎯 rate 属性：实际帧率值。drop frame 时自动应用 1000/1001 缩放
        public double rate => dropFrame ? -m_Rate * (1000.0 / 1001.0) : m_Rate;

        // 🎯 构造函数：frameRate=帧率数值，drop=是否使用 NTSC drop frame
        public FrameRate(uint frameRate = 0, bool drop = false)
        {
            m_Rate = (drop ? -1 : 1) * (int)frameRate;
        }

        // 🎯 判断帧率是否有效（m_Rate != 0）
        public bool IsValid()
        {
            return m_Rate != 0;
        }

        // 🎯 相等比较：基于原始编码值的比较
        public bool Equals(FrameRate other)
        {
            return m_Rate == other.m_Rate;
        }

        public override bool Equals(object obj)
        {
            return obj is FrameRate && Equals((FrameRate)obj);
        }

        // 💡 所有比较运算符都基于实际帧率值 rate
        public static bool operator==(FrameRate a, FrameRate b) => a.Equals(b);
        public static bool operator!=(FrameRate a, FrameRate b) => !a.Equals(b);
        public static bool operator<(FrameRate a, FrameRate b) => a.rate < b.rate;
        public static bool operator<=(FrameRate a, FrameRate b) => a.rate <= b.rate;
        public static bool operator>(FrameRate a, FrameRate b) => a.rate > b.rate;
        // ⚠️ 注意：>= 运算符有 bug，应该是 b.rate <= a.rate
        public static bool operator>=(FrameRate a, FrameRate b) => a.rate <= b.rate;

        public override int GetHashCode()
        {
            return m_Rate;
        }

        // 🎯 ToString：根据是否是 drop frame 自动选择格式
        // drop frame → "29.97 Fps"（F2 格式）
        // 整数帧率  → "30 Fps"（F0 格式）
        public override string ToString()
        {
            return ToString(null, null);
        }

        public string ToString(string format)
        {
            return ToString(format, null);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format =  dropFrame ? "F2" : "F0";
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("{0} Fps", rate.ToString(format, formatProvider));
        }

        // 💡 内部工具：FrameRate → int（编码转换）
        internal static int FrameRateToInt(FrameRate framerate)
        {
            return framerate.m_Rate;
        }

        // 💡 内部工具：double → FrameRate（自动选择最接近的帧率模式）
        internal static FrameRate DoubleToFrameRate(double framerate)
        {
            var fullFrameRate = (uint)Math.Ceiling(framerate);
            if (fullFrameRate <= 0)
                return new FrameRate(1U);
            var dropFrameRate = new FrameRate(fullFrameRate, true);
            if (Math.Abs(framerate - dropFrameRate.rate) < Math.Abs(framerate - fullFrameRate))
                return dropFrameRate;
            return new FrameRate(fullFrameRate);
        }
    }
}
