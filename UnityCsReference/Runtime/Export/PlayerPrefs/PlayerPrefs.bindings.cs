// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ==============================================================
// 🎯 PlayerPrefs — 玩家偏好存储（键值对持久化）
//
// 📌 作用：
//   在本地存储简单的键值数据（int / float / string），跨游戏会话保留
//
// 💡 存储机制（平台相关）：
//   - Windows: 注册表 HKCU\Software\公司名\产品名
//   - macOS: ~/Library/Preferences/...plist
//   - Android: SharedPreferences
//   - iOS: NSUserDefaults
//
// ⚠️ 注意：
//   - 不适合大量数据（建议用序列化存档代替）
//   - Save() 是同步写磁盘，频繁调用可能卡顿
//   - DeleteAll() 会清除所有键值，谨慎使用
//
// 💡 适用于：音量、画质设置、玩家进度等少量配置
// ==============================================================

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // This exception is thrown by the [[PlayerPrefs]] class in the Web player if the preference file would exceed the allotted storage space when setting a value.
    public class PlayerPrefsException : Exception
    {
        //*undocumented*
        public PlayerPrefsException(string error) : base(error) {}
    }

    // Stores and accesses player preferences between game sessions.
    [NativeHeader("Runtime/Utilities/PlayerPrefs.h")]
    public class PlayerPrefs
    {
        [NativeMethod("SetInt")]
        extern private static bool TrySetInt(string key, int value);

        [NativeMethod("SetFloat")]
        extern private static bool TrySetFloat(string key, float value);

        [NativeMethod("SetString")]
        extern private static bool TrySetSetString(string key, string value);

        // Sets the value of the preference identified by /key/.
        public static void SetInt(string key, int value) { if (!TrySetInt(key, value)) throw new PlayerPrefsException("Could not store preference value"); }

        // Returns the value corresponding to /key/ in the preference file if it exists.
        public extern static int GetInt(string key, int defaultValue);

        public static int GetInt(string key)
        {
            return GetInt(key, 0);
        }

        // Sets the value of the preference identified by /key/.
        public static void SetFloat(string key, float value) { if (!TrySetFloat(key, value)) throw new PlayerPrefsException("Could not store preference value"); }

        // Returns the value corresponding to /key/ in the preference file if it exists.
        public extern static float GetFloat(string key, float defaultValue);

        public static float GetFloat(string key)
        {
            return GetFloat(key, 0.0f);
        }

        // Sets the value of the preference identified by /key/.
        public static void SetString(string key, string value) { if (!TrySetSetString(key, value)) throw new PlayerPrefsException("Could not store preference value"); }


        // Returns the value corresponding to /key/ in the preference file if it exists.
        public extern static string GetString(string key, string defaultValue);

        public static string GetString(string key)
        {
            return GetString(key, "");
        }

        // Returns true if /key/ exists in the preferences.
        public extern static bool HasKey(string key);

        // Removes /key/ and its corresponding value from the preferences.
        public extern static void DeleteKey(string key);

        // Removes all keys and values from the preferences. Use with caution.
        [NativeMethod("DeleteAllWithCallback")]
        public extern static void DeleteAll();

        // Writes all modified preferences to disk.
        [NativeMethod("Sync")]
        public extern static void Save();

        // NOTE: DisposeSentinel requires access to EditorPrefs but from UnityEngine.dll
        //       (Which cant access UnityEditor.dll)
        //       So we expose the API here. Internal only, users should use the normal EditorPrefs class

        [StaticAccessor("EditorPrefs", StaticAccessorType.DoubleColon)]
        [NativeMethod("SetInt")]
        extern internal static void EditorPrefsSetInt(string key, int value);

        [StaticAccessor("EditorPrefs", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetInt")]
        extern internal static int EditorPrefsGetInt(string key, int defaultValue);
    }
}
