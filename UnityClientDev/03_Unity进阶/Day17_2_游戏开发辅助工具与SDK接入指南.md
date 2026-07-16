# 游戏开发辅助工具与 SDK 接入指南 (Day 17 延伸)

本指南针对唐老狮“商业游戏开发前端/后端工具开发”及“平台开发技能”大纲设计，补齐原大纲中在移动端平台开发、自动化工具链及第三方 SDK 接入方面的空白。

---

## 一、 移动端平台开发基础 (Android & iOS)

### 1.1 Android 平台基本编程
*   **核心技能：**
    *   Android Studio 使用与 Gradle 编译配置。
    *   Java/Kotlin 基础，Android 生命周期与 Activity。
    *   **Unity 与 Android 交互：**
        *   C# 调用 Java：`AndroidJavaClass` 与 `AndroidJavaObject`。
        *   Java 调用 C#：`UnityPlayer.UnitySendMessage`。
*   **C# 示例：Unity 调用 Android 震动与原生弹窗**
    ```csharp
    using UnityEngine;

    public class AndroidNativeBridge : MonoBehaviour
    {
        public void Vibrate(long milliseconds)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator != null)
                {
                    vibrator.Call("vibrate", milliseconds);
                }
            }
            #endif
        }
    }
    ```

### 1.2 iOS 平台基本编程
*   **核心技能：**
    *   Mac OS 环境、Xcode 配置、CocoaPods 依赖管理。
    *   Objective-C/Swift 语言基础。
    *   **Unity 与 iOS 交互：**
        *   通过 `[DllImport("__Internal")]` 引入 C-Style 函数。
        *   在 iOS (.m/.mm 文件) 中通过 `UnitySendMessage` 回调 C#。

---

## 二、 客户端自主开发工具链

### 2.1 Excel 转二进制/数据档生成工具
*   **核心概念：** 游戏配置通常在 Excel 中编辑，但运行时加载 `.xlsx` 极其缓慢且耗内存。需编写工具将其转换为紧凑的**二进制文件**、**ScriptableObject** 或 **JSON**。
*   **设计思路：** 使用 EPPlus / ExcelDataReader 读取表格，根据首行字段定义生成 C# 结构体定义，并将数据流打包为二进制。

### 2.2 多语言本地化（Localization）工具
*   **核心概念：**
    *   多语言 Key-Value 配置表管理（Excel → JSON/CSV）。
    *   Unity 渲染组件（Text/Image）绑定多语言组件。
    *   字体动态字形剪裁（Font Subset）与多语言图片资源动态替换。

### 2.3 自动化打包与出档脚本 (CI/CD)
*   **核心概念：** 避免在 Unity 编辑器中手动点击 Build，使用命令行和 Shell/Python 脚本实现一键打包。
*   **Unity C# 静态打包函数示例：**
    ```csharp
    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;

    public class BuildBatch
    {
        public static void BuildAndroid()
        {
            string[] scenes = { "Assets/Scenes/Main.unity" };
            string outputPath = "Builds/Android/Game.apk";
            
            BuildPlayerOptions opt = new BuildPlayerOptions();
            opt.scenes = scenes;
            opt.locationPathName = outputPath;
            opt.target = BuildTarget.Android;
            opt.options = BuildOptions.None;

            var report = BuildPipeline.BuildPlayer(opt);
            Debug.Log("Build Result: " + report.summary.result);
        }
    }
    #endif
    ```
*   **命令行调用：**
    ```bash
    Unity -quit -batchmode -projectPath "ProjectDir" -executeMethod BuildBatch.BuildAndroid -logFile build.log
    ```

---

## 三、 第三方 SDK 与平台接入

### 3.1 TalkingData / 渠道数据统计
*   **核心功能：** 统计激活数、留存率、DAU、LTV、自定义关卡/充值事件。
*   **接入要点：** 在游戏初始化、玩家登录、付费、关卡结束等生命周期节点进行打点埋点。

### 3.2 闪退与异常监测 (Bugly)
*   **核心功能：** 捕获 C# Crash、Android Java Crash、iOS Objective-C Crash 以及 native NDK 崩溃（SO 库报错）。
*   **接入要点：** 挂接 `Application.logMessageReceived` 委托，收集异常并上报 Bugly 后台；配置符号表（Symbol File）以便将堆栈地址还原为可读代码行。

### 3.3 性能检测与云测平台 (腾讯 WeTest)
*   **核心功能：** 机型适配测试、性能压测（帧率、内存泄漏、发热、Draw Call 监控）。
