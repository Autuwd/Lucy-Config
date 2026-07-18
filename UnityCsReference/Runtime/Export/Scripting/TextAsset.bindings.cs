// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 TextAsset — 文本/二进制数据资源
//
// 📌 作用：
//   将文本文件（.txt/.json/.xml/.csv 等）或二进制文件作为资源导入
//
// 💡 关键属性：
//   - text: 文件内容的字符串（UTF-8 编码）
//   - bytes: 原始字节数组
//   - GetDataPtr() / GetDataSize(): 获取底层数据指针和大小（Native 互操作）
//
// 🎯 使用方式：
//   TextAsset asset = Resources.Load<TextAsset>("myFile");
//   string content = asset.text;
//
// 💡 父类 Object 的 name 属性返回文件名（不含扩展名）
// ==============================================================

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Scripting/TextAsset.h")]
    public partial class TextAsset : Object
    {
        // The raw bytes of the text asset. (RO)
        public extern byte[] bytes { [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)] get; }

        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern byte[] GetPreviewBytes(int maxByteCount);

        extern static void Internal_CreateInstance([Writable] TextAsset self, string text);

        extern static void Internal_CreateInstanceFromBytes([Writable] TextAsset self, ReadOnlySpan<byte> bytes);

        extern IntPtr GetDataPtr();
        extern long GetDataSize();

        static extern AtomicSafetyHandle GetSafetyHandle(TextAsset self);
    }
}
