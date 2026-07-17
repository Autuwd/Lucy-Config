// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngineInternal;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;

namespace UnityEngine
{
    // ============================================================
    // Application 类 — Unity 运行时信息的核心入口
    //
    // 功能概述：
    //   Application 是 Unity 引擎暴露给用户空间的「运行时信息中心」。
    //   它提供了访问以下信息的途径：
    //   - 生命周期控制：Quit / Unload / wantsToQuit / quitting
    //   - 运行时状态：isPlaying / isFocused / isEditor / runInBackground
    //   - 文件路径：dataPath / streamingAssetsPath / persistentDataPath / temporaryCachePath
    //   - 版本信息：unityVersion / version / buildGUID
    //   - 项目配置：productName / companyName / identifier
    //   - 平台信息：platform / systemLanguage / internetReachability
    //   - 性能控制：targetFrameRate / backgroundLoadingPriority
    //   - 日志系统：logMessageReceived / stackTraceLogType
    //   - 平台服务：OpenURL / RequestUserAuthorization
    //
    // 实现方式：
    //   Application 是一个 partial class，其中：
    //   - Application.bindings.cs（本文件）：Native → C# 绑定层，通过 [NativeHeader] 和
    //     [FreeFunction] 属性将 C# 属性/方法映射到 C++ 原生实现
    //   - Application.cs：纯 C# 实现部分，包含枚举定义、事件管理、回调分发、
    //     以及标记为 [Obsolete] 的遗留兼容 API
    //
    // ============================================================

    // [NativeHeader] 说明
    // 下面的每个 [NativeHeader] 属性都告诉 IL2CPP / 绑定代码生成器，
    // 该类的某个成员对应的 C++ 原生实现在哪个头文件中声明。
    // 这是 Unity 内部绑定系统（bindings generator）的工作方式，
    // 它根据这些属性自动生成 C++ 和 C# 之间的互操作代码。
    //
    // 各头文件功能说明：
    [NativeHeader("Runtime/Application/AdsIdHandler.h")]           // 广告标识符（IDFA/AAID）请求处理
    [NativeHeader("Runtime/Application/ApplicationInfo.h")]         // 应用程序信息（version/identifier/installMode/sandboxType）
    [NativeHeader("Runtime/BaseClasses/IsPlaying.h")]               // isPlaying 运行时判断标志
    [NativeHeader("Runtime/Export/Application/Application.bindings.h")] // 本文件的 C++ 绑定声明
    [NativeHeader("Runtime/File/ApplicationSpecificPersistentDataPath.h")] // 持久化数据路径计算
    [NativeHeader("Runtime/Input/GetInput.h")]                      // 输入系统获取
    [NativeHeader("Runtime/Input/InputManager.h")]                  // 输入管理器（Quit/CancelQuit 等）
    [NativeHeader("Runtime/Input/TargetFrameRate.h")]               // targetFrameRate 读写
    [NativeHeader("NativeKernel/Logging/LogSystem.h")]              // 日志系统（consoleLogPath 等）
    [NativeHeader("Runtime/Misc/BuildSettings.h")]                  // 构建设置（buildGUID/HasProLicense 等）
    [NativeHeader("Runtime/Misc/Player.h")]                         // 播放器核心（isBatchMode 等）
    [NativeHeader("Runtime/Misc/PlayerSettings.h")]                 // PlayerSettings（runInBackground/productName/companyName 等）
    [NativeHeader("Runtime/Misc/SystemInfo.h")]                     // 系统信息（platform/systemLanguage）
    [NativeHeader("Runtime/Network/NetworkUtility.h")]              // 网络可达性
    [NativeHeader("Runtime/PreloadManager/LoadSceneOperation.h")]   // 场景加载操作（CanStreamedLevelBeLoaded）
    [NativeHeader("Runtime/PreloadManager/PreloadManager.h")]       // 预加载管理器（isLoadingLevel/backgroundLoadingPriority）
    [NativeHeader("Runtime/Utilities/Argv.h")]                      // 命令行参数解析
    [NativeHeader("Runtime/Utilities/URLUtility.h")]                // URL 工具（OpenURL）
    public partial class Application
    {
            // ============================================================
        // 生命周期控制
        // ============================================================

        /// <summary>
        ///   退出应用程序。在编辑器和 WebGL 平台下调用无效。
        ///   exitCode 会作为进程退出码返回给操作系统（如 Android / Standalone 平台）。
        ///   调用 Quit 后，Unity 会发出 Application.quitting 事件，
        ///   然后开始卸载场景和资源，最后结束进程。
        /// </summary>
        /// <param name="exitCode">进程退出码，默认为 0 表示正常退出</param>
        [FreeFunction("GetInputManager().QuitApplication")]
        extern public static void Quit(int exitCode);

        /// <summary>
        ///   以默认退出码 0 退出应用程序。
        /// </summary>
        public static void Quit()
        {
            Quit(0);
        }

        /// <summary>
        ///   取消退出操作。
        ///   【已废弃】请使用 Application.wantsToQuit 事件代替。
        ///   wantsToQuit 是 Func&lt;bool&gt; 委托，在退出前触发，
        ///   返回 false 即可取消退出，比 CancelQuit 更灵活。
        /// </summary>
        [Obsolete("CancelQuit is deprecated. Use the wantsToQuit event instead.")]
        [FreeFunction("GetInputManager().CancelQuitApplication")]
        extern public static void CancelQuit();

        /// <summary>
        ///   卸载 Unity 引擎但不退出应用程序。
        ///   和 Quit 的区别：Unload 执行引擎反初始化（释放资源、卸载脚本域等），
        ///   但不结束进程。常用于编辑器内的重载（如进入播放模式前的清理）。
        /// </summary>
        [FreeFunction("Application_Bindings::Unload")]
        extern public static void Unload();

        // ============================================================
        // 已废弃 API — 流加载 / Web Player 残留
        // 下面这些 API 在 Unity 5.x 时代用于 Web Player 的流式加载，
        // 自 Unity 移除 Web Player 后已全部废弃，仅保留空实现保证编译兼容。
        // ============================================================

        [Obsolete("此属性已废弃，请使用 LoadLevelAsync 检测场景是否正在加载。")]
        extern public static bool isLoadingLevel
        {
            [FreeFunction("GetPreloadManager().IsLoadingOrQueued")]
            get;
        }

        /// <summary>
        ///   模拟特定级别的内存使用情况（仅内部测试用）。
        /// </summary>
        [FreeFunction("UpdateMemoryUsage")]
        internal static extern void SimulateMemoryUsage(ApplicationMemoryUsage usage);

        [Obsolete("流加载原是 Unity Web Player 的功能，此功能已移除。本方法已废弃，对于合法关卡索引始终返回 1.0。")]
        public static float GetStreamProgressForLevel(int levelIndex)
        {
            if (levelIndex >= 0 && levelIndex < SceneManager.sceneCountInBuildSettings)
                return 1.0F;
            else
                return 0.0F;
        }

        [Obsolete("流加载原是 Unity Web Player 的功能，此功能已移除。本方法已废弃，始终返回 1.0。")]
        public static float GetStreamProgressForLevel(string levelName) { return 1.0f; }

        [Obsolete("流加载原是 Unity Web Player 的功能，此功能已移除。此属性已废弃，始终返回 0。")]
        public static int streamedBytes
        {
            get
            {
                return 0;
            }
        }

        // 注意：以下 API 不能移除，因为 SyntaxTree.VisualStudio.Unity.Bridge.dll（Microsoft 随 VS 发布的程序集）
        // 仍引用了它，我们无法更新该程序集。
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Application.webSecurityEnabled 已不再支持，因为 Unity Web Player 已不再被 Unity 支持", true)]
        static public bool webSecurityEnabled
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        ///   检查指定索引的关卡是否可以流式加载。
        /// </summary>
        public static bool CanStreamedLevelBeLoaded(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex < SceneManager.sceneCountInBuildSettings;
        }

        /// <summary>
        ///   根据关卡名称检查是否可以流式加载。
        /// </summary>
        [FreeFunction("Application_Bindings::CanStreamedLevelBeLoaded")]
        public extern static bool CanStreamedLevelBeLoaded(string levelName);

        // ============================================================
        // 运行时状态查询
        // ============================================================

        /// <summary>
        ///   当前是否处于「播放模式」（任何类型的运行时）。
        ///   - 在编辑器中点击 Play 按钮后，isPlaying 变为 true
        ///   - 在真机上始终为 true
        ///   - 这是 Unity 开发中最常用的运行时判断标志之一
        ///   对应 C++ 实现：IsWorldPlaying()
        /// </summary>
        public extern static bool isPlaying
        {
            [FreeFunction("IsWorldPlaying")]
            get;
        }

        /// <summary>
        ///   判断指定对象所属的场景/组件当前是否处于运行状态。
        ///   参数不可为 null。
        /// </summary>
        /// <param name="obj">任意 UnityEngine.Object</param>
        /// <returns>如果对象处于播放状态则返回 true</returns>
        [FreeFunction]
        public extern static bool IsPlaying([NotNull] UnityEngine.Object obj);

        /// <summary>
        ///   应用程序窗口是否获得焦点（前台运行）。
        ///   - Standalone：窗口激活时为 true，最小化或切换到其他应用时为 false
        ///   - 移动端：通常始终为 true
        ///   配合 Application.focusChanged 事件可以监听焦点变化。
        /// </summary>
        public extern static bool isFocused
        {
            [FreeFunction("IsPlayerFocused")]
            get;
        }

        /// <summary>
        ///   【已废弃】获取构建标签。
        /// </summary>
        [FreeFunction("GetBuildSettings().GetBuildTags")]
        [Obsolete("Application.GetBuildTags 已不再支持，将被移除。", false)]
        extern public static string[] GetBuildTags();

        /// <summary>
        ///   【已废弃】设置构建标签。
        /// </summary>
        [FreeFunction("GetBuildSettings().SetBuildTags")]
        [Obsolete("Application.SetBuildTags 已不再支持，将被移除。", false)]
        extern public static void SetBuildTags(string[] buildTags);

        /// <summary>
        ///   本次构建的唯一标识 GUID。
        ///   每次 Build 都会生成一个新的 GUID，可以用来追踪构建版本。
        /// </summary>
        extern public static string buildGUID
        {
            [FreeFunction("Application_Bindings::GetBuildGUID")]
            get;
        }

        /// <summary>
        ///   应用程序在后台时是否继续运行。
        ///   - Standalone：当窗口最小化或失去焦点时是否继续执行 Update
        ///   - 移动端：应用切到后台时是否保持运行
        ///   对应 PlayerSettings 中的 "Run In Background" 设置。
        /// </summary>
        extern public static bool runInBackground
        {
            [FreeFunction("GetPlayerSettingsRunInBackground")]
            get;
            [FreeFunction("SetPlayerSettingsRunInBackground")]
            set;
        }

        /// <summary>
        ///   是否激活了 Unity Pro 许可证。
        ///   Pro 许可证提供一些额外的功能（如 splash screen 自定义等）。
        /// </summary>
        [FreeFunction("GetBuildSettings().GetHasPROVersion")]
        extern public static bool HasProLicense();

        /// <summary>
        ///   当前是否在批处理模式（batch mode）下运行。
        ///   Batch mode 用于命令行自动化（如 CI/CD、自动构建等），没有窗口和图形界面。
        ///   对应 -batchmode 命令行参数。
        /// </summary>
        extern public static bool isBatchMode
        {
            [FreeFunction("::IsBatchmode")]
            get;
        }

        /// <summary>
        ///   当前是否在测试运行模式（Unity Test Framework 使用）。
        /// </summary>
        extern static internal bool isTestRun
        {
            [FreeFunction("::IsTestRun")]
            get;
        }

        /// <summary>
        ///   当前是否在构建编辑器资源（Asset Store / 包管理资源构建流程中使用）。
        /// </summary>
        extern static internal bool isBuildingEditorResources
        {
            [FreeFunction("::IsBuildingEditorResources")]
            [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
            get;
        }

        /// <summary>
        ///   当前是否有人类用户正在交互控制（非自动化/批处理模式）。
        /// </summary>
        extern static internal bool isHumanControllingUs
        {
            [FreeFunction("::IsHumanControllingUs")]
            [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
            get;
        }

        [FreeFunction("HasARGV")]
        extern static internal bool HasARGV(string name);

        [FreeFunction("GetFirstValueForARGV")]
        extern static internal string GetValueForARGV(string name);

        // ============================================================
        // 文件路径（只读）
        // ============================================================

        /// <summary>
        ///   游戏数据文件夹路径（只读）。
        ///   对应构建产物中的 xxx_Data 文件夹，包含所有资源、配置和 Managed 程序集。
        ///   各平台示例：
        ///   - Standalone Windows: &lt;项目根目录&gt;/xxx_Data/
        ///   - iOS: Application/xxx.app/
        ///   - Android: APK 内部（不可直接写入）
        ///   注意：此路径是只读的，不可写入文件。
        ///   线程安全：是（Native 方法直接返回字符串常量）。
        /// </summary>
        extern public static string dataPath
        {
            [FreeFunction("GetAppDataPath", IsThreadSafe = true)]
            get;
        }

        /// <summary>
        ///   StreamingAssets 文件夹路径（只读）。
        ///   此文件夹下的内容在构建时会被原样复制到目标平台，是 Unity 唯一的
        ///   「保持原始文件格式」的资源存放位置。
        ///   各平台示例：
        ///   - Standalone Windows: &lt;dataPath&gt;/StreamingAssets/
        ///   - iOS: Application/xxx.app/Data/Raw/
        ///   - Android: APK 内部 assets/ 目录（使用 UnityWebRequest 读取）
        ///   注意：Android 平台不能使用 System.IO 直接读取，必须用 UnityWebRequest。
        ///   线程安全：是。
        /// </summary>
        extern public static string streamingAssetsPath
        {
            [FreeFunction("GetStreamingAssetsPath", IsThreadSafe = true)]
            get;
        }

        /// <summary>
        ///   持久化数据目录路径（只读）。
        ///   这是推荐存放用户存档、下载内容、设置文件等数据的目录，
        ///   在应用更新/卸载前数据会一直保留。
        ///   各平台示例：
        ///   - Windows: %USERPROFILE%/AppData/LocalLow/&lt;CompanyName&gt;/&lt;ProductName&gt;/
        ///   - iOS: Application/xxx/Documents/
        ///   - Android: /storage/emulated/0/Android/data/&lt;package&gt;/files/
        /// </summary>
        extern public static string persistentDataPath
        {
            [FreeFunction("GetPersistentDataPathApplicationSpecific")]
            get;
        }

        /// <summary>
        ///   临时缓存目录路径（只读）。
        ///   用于存放临时文件，操作系统可能随时清理此目录下的内容。
        ///   适合存放下载的临时资源、缓存数据等不重要的文件。
        ///   各平台示例：
        ///   - Windows: %USERPROFILE%/AppData/Local/Temp/&lt;CompanyName&gt;/&lt;ProductName&gt;/
        ///   - iOS: Application/xxx/Caches/
        ///   - Android: /storage/emulated/0/Android/data/&lt;package&gt;/cache/
        /// </summary>
        extern public static string temporaryCachePath
        {
            [FreeFunction("GetTemporaryCachePathApplicationSpecific")]
            get;
        }

        /// <summary>
        ///   WebGL 平台当前页面的 URL（浏览器地址栏内容）。
        ///   仅在 WebGL 平台有效，其他平台返回空字符串。
        ///   可用于获取 URL 参数等场景。
        /// </summary>
        extern public static string absoluteURL
        {
            [FreeFunction("GetPlayerSettings().GetAbsoluteURL")]
            get;
        }

        // ============================================================
        // 已废弃 API — 外部脚本调用（原用于 Web Player）
        // ============================================================

        /// <summary>
        ///   在 Web Player 的宿主页面中执行 JavaScript 脚本。
        ///   【已废弃】Web Player 已移除。
        ///   替代方案：对于 WebGL 平台，使用 jslib 插件或 Application.ExternalCall。
        /// </summary>
        [Obsolete("Application.ExternalEval 已废弃。请参考 https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html 了解替代方案。")]
        public static void ExternalEval(string script)
        {
            if (script.Length > 0 && script[script.Length - 1] != ';')
                script += ';';
            Internal_ExternalCall(script);
        }

        [FreeFunction("Application_Bindings::ExternalCall")]
        extern private static void Internal_ExternalCall(string script);

        // ============================================================
        // 版本信息
        // ============================================================

        // 注意：unityVersion 的线程安全性已确认，
        // 因为原生方法仅返回一个 #DEFINE 预处理器常量值。

        /// <summary>
        ///   当前运行内容的 Unity 运行时版本号。
        ///   返回格式如 "2022.3.1f1" 的完整版本字符串。
        ///   线程安全：是（仅返回编译时常量）。
        /// </summary>
        extern public static string unityVersion
        {
            [FreeFunction("Application_Bindings::GetUnityVersion", IsThreadSafe = true)]
            get;
        }

        /// <summary>
        ///   Unity 版本号的「修订号」（Ver）部分。
        ///   内部使用，例如版本 2022.3.1f1 中对应 1。
        /// </summary>
        extern internal static int unityVersionVer
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            [FreeFunction("Application_Bindings::GetUnityVersionVer", IsThreadSafe = true)]
            get;
        }

        /// <summary>
        ///   Unity 版本号的「主版本号」（Major）部分。
        ///   内部使用，例如版本 2022.3.1f1 中对应 2022。
        /// </summary>
        extern internal static int unityVersionMaj
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            [FreeFunction("Application_Bindings::GetUnityVersionMaj", IsThreadSafe = true)]
            get;
        }

        /// <summary>
        ///   Unity 版本号的「次版本号」（Minor）部分。
        ///   内部使用，例如版本 2022.3.1f1 中对应 3。
        /// </summary>
        extern internal static int unityVersionMin { [FreeFunction("Application_Bindings::GetUnityVersionMin", IsThreadSafe = true)] get; }

        // ============================================================
        // 应用配置信息
        // ============================================================

        /// <summary>
        ///   运行时应用程序版本号。
        ///   对应 PlayerSettings 中设置的 "Version" 字段，
        ///   用于向用户/系统标识应用的发布版本。
        /// </summary>
        extern public static string version
        {
            [FreeFunction("GetApplicationInfo().GetVersion")]
            get;
        }

        /// <summary>
        ///   安装程序名称（主要用于 Android 平台）。
        ///   标识应用是通过哪个商店/渠道安装的。
        ///   例如："com.android.vending"（Google Play）或空字符串。
        /// </summary>
        extern public static string installerName
        {
            [FreeFunction("GetApplicationInfo().GetInstallerName")]
            get;
        }

        /// <summary>
        ///   应用程序标识符（包名/Bundle Identifier）。
        ///   对应 PlayerSettings 中的 "Application Identifier"。
        ///   各平台对应关系：
        ///   - Android: package name（如 com.company.product）
        ///   - iOS: Bundle Identifier
        ///   - Standalone: 通常也为反向域名格式
        /// </summary>
        extern public static string identifier
        {
            [FreeFunction("GetApplicationInfo().GetApplicationIdentifier")]
            get;
        }

        /// <summary>
        ///   应用的安装方式（开发者构建 / 商店分发 / AdHoc 等）。
        ///   用于调试和分发渠道识别。
        /// </summary>
        extern public static ApplicationInstallMode installMode
        {
            [FreeFunction("GetApplicationInfo().GetInstallMode")]
            get;
        }

        /// <summary>
        ///   应用的沙箱类型。
        ///   某些平台（如 iOS/macOS）要求应用运行在沙箱环境中，
        ///   此属性标识沙箱状态。
        /// </summary>
        extern public static ApplicationSandboxType sandboxType
        {
            [FreeFunction("GetApplicationInfo().GetSandboxType")]
            get;
        }

        // ============================================================
        // 项目设置（来自 PlayerSettings）
        // ============================================================

        /// <summary>
        ///   产品名称。对应 PlayerSettings 中的 "Product Name"。
        ///   通常显示在窗口标题栏和应用图标下方。
        /// </summary>
        extern public static string productName
        {
            [FreeFunction("GetPlayerSettings().GetProductName")]
            get;
        }

        /// <summary>
        ///   公司名称。对应 PlayerSettings 中的 "Company Name"。
        ///   影响 persistentDataPath 的路径（如 AppData/LocalLow/&lt;CompanyName&gt;/&lt;ProductName&gt;/）。
        /// </summary>
        extern public static string companyName
        {
            [FreeFunction("GetPlayerSettings().GetCompanyName")]
            get;
        }

        /// <summary>
        ///   Unity Cloud 项目 ID。
        ///   用于 Unity Cloud Build、Unity Analytics 等云服务。
        /// </summary>
        extern public static string cloudProjectId
        {
            [FreeFunction("GetPlayerSettings().GetCloudProjectId")]
            get;
        }

        // ============================================================
        // 平台服务
        // ============================================================

        /// <summary>
        ///   异步请求广告标识符。
        ///   在 iOS 上会弹出 ATT（App Tracking Transparency）权限对话框，
        ///   在 Android 上获取 AAID（Google Advertising ID）。
        ///   通过 callback 异步返回广告 ID 和追踪启用状态。
        /// </summary>
        /// <param name="delegateMethod">回调委托：(advertisingId, trackingEnabled, errorMsg) => void</param>
        /// <returns>如果请求已发起返回 true</returns>
        [FreeFunction("GetAdsIdHandler().RequestAdsIdAsync")]
        extern public static bool RequestAdvertisingIdentifierAsync(AdvertisingIdentifierCallback delegateMethod);

        /// <summary>
        ///   在默认浏览器中打开指定 URL。
        ///   - Standalone：调用系统默认浏览器
        ///   - 移动端：调用系统浏览器或应用内 WebView
        ///   - WebGL：当前页面跳转或打开新窗口
        /// </summary>
        /// <param name="url">要打开的完整 URL（需包含协议，如 "https://..."）</param>
        [FreeFunction("OpenURL")]
        extern public static void OpenURL(string url);

        /// <summary>
        ///   【已废弃】主动触发崩溃。
        ///   替代方案：使用 UnityEngine.Diagnostics.Utils.ForceCrash。
        /// </summary>
        [Obsolete("请使用 UnityEngine.Diagnostics.Utils.ForceCrash")]
        public static void ForceCrash(int mode)
        {
            UnityEngine.Diagnostics.Utils.ForceCrash((UnityEngine.Diagnostics.ForcedCrashCategory)mode);
        }

        // ============================================================
        // 帧率与性能控制
        // ============================================================

        /// <summary>
        ///   目标帧率。指示游戏尝试以指定的帧率渲染。
        ///   - 默认值：-1（不限制，尽可能快）
        ///   - 常见值：30（移动端省电），60（标准），120（高刷显示器）
        ///   - 设置为 -1 表示使用平台默认（通常是 60 或跟显示器刷新率）
        ///   注意：这只是一个「目标」，实际帧率可能因为性能瓶颈达不到。
        ///   Application.Quit 时 targetFrameRate 设置会丢失。
        /// </summary>
        extern public static int targetFrameRate
        {
            [FreeFunction("GetTargetFrameRate")]
            get;
            [FreeFunction("SetTargetFrameRate")]
            set;
        }

        // ============================================================
        // 日志系统
        // ============================================================

        [FreeFunction("Application_Bindings::SetLogCallbackDefined")]
        extern private static void SetLogCallbackDefined(bool defined);

        /// <summary>
        ///   【已废弃】全局堆栈追踪日志类型。
        ///   替代方案：使用 GetStackTraceLogType / SetStackTraceLogType 按 LogType 分别设置。
        /// </summary>
        [Obsolete("请使用 SetStackTraceLogType/GetStackTraceLogType 代替")]
        extern public static StackTraceLogType stackTraceLogType
        {
            [FreeFunction("Application_Bindings::GetStackTraceLogType")]
            get;
            [FreeFunction("Application_Bindings::SetStackTraceLogType")]
            set;
        }

        /// <summary>
        ///   获取指定日志类型的堆栈追踪输出设置。
        /// </summary>
        /// <param name="logType">日志类型</param>
        /// <returns>该日志类型的堆栈追踪输出方式</returns>
        [FreeFunction("GetStackTraceLogType")]
        extern public static StackTraceLogType GetStackTraceLogType(LogType logType);

        /// <summary>
        ///   设置指定日志类型的堆栈追踪输出方式。
        ///   可用于控制 Error 显示完整堆栈，而 Warning 只显示脚本信息等。
        /// </summary>
        /// <param name="logType">日志类型</param>
        /// <param name="stackTraceType">堆栈追踪输出方式</param>
        [FreeFunction("SetStackTraceLogType")]
        extern public static void SetStackTraceLogType(LogType logType, StackTraceLogType stackTraceType);

        /// <summary>
        ///   控制台日志文件的路径。
        ///   Standalone 平台 Unity 引擎内部日志的输出文件路径。
        ///   通常用于排查引擎级别的问题。
        /// </summary>
        extern public static string consoleLogPath
        {
            [FreeFunction("GetConsoleLogPath")]
            get;
        }

        // ============================================================
        // 后台加载优先级
        // ============================================================

        /// <summary>
        ///   后台资源加载线程的优先级。
        ///   影响 AsyncOperation 加载资源时的 CPU 占用策略：
        ///   - ThreadPriority.High：加载更快但可能影响主线程帧率
        ///   - ThreadPriority.Normal：默认平衡
        ///   - ThreadPriority.Low：加载较慢但主线程更流畅
        ///   对应 PreloadManager 中的线程优先级设置。
        /// </summary>
        extern public static ThreadPriority backgroundLoadingPriority
        {
            [FreeFunction("GetPreloadManager().GetThreadPriority")]
            get;
            [FreeFunction("GetPreloadManager().SetThreadPriority")]
            set;
        }

        // ============================================================
        // 完整性验证（反篡改）
        // ============================================================

        /// <summary>
        ///   应用程序是否在构建后未被篡改。
        ///   如果应用被修改（注入、破解等），返回 false。
        ///   需要 genuineCheckAvailable 为 true 时此值才可信。
        /// </summary>
        extern public static bool genuine
        {
            [FreeFunction("IsApplicationGenuine")]
            get;
        }

        /// <summary>
        ///   当前平台是否支持完整性检查。
        ///   部分平台（如某些低端 Android 设备）可能不支持此功能。
        /// </summary>
        extern public static bool genuineCheckAvailable
        {
            [FreeFunction("IsApplicationGenuineAvailable")]
            get;
        }

        // ============================================================
        // 用户授权（摄像头/麦克风）
        // ============================================================

        /// <summary>
        ///   请求用户授权使用摄像头（WebCam）或麦克风（Microphone）。
        ///   返回一个 AsyncOperation，可以通过它判断授权结果。
        ///   在 iOS 上会弹出系统权限对话框，在 Android 上根据 API 级别
        ///   可能会自动处理或弹出对话框。
        /// </summary>
        /// <param name="mode">UserAuthorization.WebCam 或 UserAuthorization.Microphone</param>
        /// <returns>异步操作对象，可通过 isDone 和 .allowSceneActivation 判断完成</returns>
        [FreeFunction("Application_Bindings::RequestUserAuthorization")]
        extern public static AsyncOperation RequestUserAuthorization(UserAuthorization mode);

        /// <summary>
        ///   检查用户是否已授权使用摄像头或麦克风。
        ///   在请求授权前可先调用此方法检查状态，避免重复弹窗。
        /// </summary>
        /// <param name="mode">UserAuthorization.WebCam 或 UserAuthorization.Microphone</param>
        /// <returns>如果已授权返回 true</returns>
        [FreeFunction("Application_Bindings::HasUserAuthorization")]
        extern public  static bool HasUserAuthorization(UserAuthorization mode);

        /// <summary>
        ///   是否提交使用数据到 Unity Analytics。
        ///   对应 PlayerSettings 中的设置。
        /// </summary>
        extern internal static bool submitAnalytics
        {
            [FreeFunction("GetPlayerSettings().GetSubmitAnalytics")]
            get;
        }

        /// <summary>
        ///   【已废弃】启动画面是否正在显示。
        ///   替代方案：使用 SplashScreen.isFinished。
        /// </summary>
        [Obsolete("此属性已废弃，请使用 SplashScreen.isFinished 代替")]
        public static bool isShowingSplashScreen
        {
            get
            {
                return !UnityEngine.Rendering.SplashScreen.isFinished;
            }
        }
    }

    public partial class Application
    {
        // ============================================================
        // 平台信息
        // ============================================================

        /// <summary>
        ///   当前游戏运行的目标平台（只读）。
        ///   用于运行时判断平台，执行平台相关的逻辑分支。
        ///   线程安全：是（Native 方法仅读取常量值）。
        /// </summary>
        extern public static RuntimePlatform platform
        {
            [FreeFunction("systeminfo::GetRuntimePlatform", IsThreadSafe = true)]
            get;
        }

        /// <summary>
        ///   当前是否在移动平台上运行。
        ///   明确为移动平台的判断（iOS / Android / VisionOS），
        ///   WSA 平台根据设备类型判断是否为手持设备。
        ///   用于 UI 缩放、性能预算、触摸输入等移动端适配逻辑。
        /// </summary>
        public static bool isMobilePlatform
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.Android:
                    case RuntimePlatform.VisionOS:
                        return true;
                    case RuntimePlatform.WSAPlayerX86:
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerARM:
                        return SystemInfo.deviceType == DeviceType.Handheld;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        ///   当前是否在主机平台上运行。
        ///   包括 Xbox（XboxOne / GameCoreXboxOne / GameCoreXboxSeries）、
        ///   PlayStation（PS4 / PS5）、Nintendo Switch 等。
        ///   用于主机平台特有的逻辑（手柄输入、成就系统、分辨率设置等）。
        /// </summary>
        public static bool isConsolePlatform
        {
            get
            {
                RuntimePlatform platform = Application.platform;
                return platform == RuntimePlatform.GameCoreXboxOne
                    || platform == RuntimePlatform.GameCoreXboxSeries
                    || platform == RuntimePlatform.PS4
                    || platform == RuntimePlatform.PS5
                    || platform == RuntimePlatform.Switch
                    || platform == RuntimePlatform.Switch2
                    || platform == RuntimePlatform.XboxOne;
            }
        }

        /// <summary>
        ///   用户操作系统的语言设置。
        ///   返回 SystemLanguage 枚举值（如 ChineseSimplified、English、Japanese 等）。
        ///   应用启动时读取一次，如果运行时切换系统语言需重启应用才能刷新。
        ///   可用于自动选择游戏 UI 语言（需配合本地化系统）。
        /// </summary>
        extern public static SystemLanguage systemLanguage
        {
            [FreeFunction("(SystemLanguage)systeminfo::GetSystemLanguage")]
            get;
        }

        /// <summary>
        ///   当前设备的网络可达性类型。
        ///   - NotReachable：无网络连接
        ///   - ReachableViaCarrierDataNetwork：通过移动数据网络（4G/5G）
        ///   - ReachableViaLocalAreaNetwork：通过 WiFi 或有线网络
        ///   常用于判断是否需要提醒用户使用移动数据下载资源。
        /// </summary>
        extern public static NetworkReachability internetReachability
        {
            [FreeFunction("GetInternetReachability")]
            get;
        }
    }
}
