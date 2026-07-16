# Raylib 使用教程与学习资料

> 项目目录：`D:\Apps\SmallEngine\Projects\Raylib`
> Raylib 安装在：`C:\raylib\`
> 编译器：w64devkit（GCC 15.2.0）位于 `C:\raylib\w64devkit\bin`
> 官网：https://www.raylib.com

---

# 目录

1. [什么是 Raylib](#1-什么是-raylib)
2. [当前工作环境](#2-当前工作环境)
3. [第一个程序：创建窗口](#3-第一个程序创建窗口)
4. [核心概念](#4-核心概念)
5. [绘制图形](#5-绘制图形)
6. [颜色系统](#6-颜色系统)
7. [输入处理](#7-输入处理)
8. [纹理与图片](#8-纹理与图片)
9. [文字与字体](#9-文字与字体)
10. [音频系统](#10-音频系统)
11. [2D 摄像机](#11-2d-摄像机)
12. [碰撞检测](#12-碰撞检测)
13. [实用工具函数](#13-实用工具函数)
14. [学习资源推荐](#14-学习资源推荐)

---

# 1. 什么是 Raylib

Raylib 是一个**开源、跨平台**的图形/游戏编程库，设计理念是让编程变得有趣且易于上手。

## 主要特点

| 特性 | 说明 |
|------|------|
| **语言** | 核心用 C 语言编写，支持 C、C++、Python、Rust、Lua 等 50+ 语言绑定 |
| **许可证** | zlib/libpng 许可证，完全免费，可商用 |
| **跨平台** | Windows、macOS、Linux、Android、iOS、Web (WebAssembly)、树莓派 |
| **模块化** | 核心模块（core）、图形（graphics）、文字（text）、音频（audio）等 |
| **无外部依赖** | 编译后无需安装额外运行时 |
| **OpenGL** | 支持 OpenGL 1.1、2.1、3.3、4.3 以及 OpenGL ES 2.0/3.0 |

---

# 2. 当前工作环境

## 2.1 项目目录结构

```
D:\Apps\SmallEngine\Projects\Raylib\
│
├── compile.bat                  # 核心编译脚本（拖拽 .cpp 到它上面）
├── compile_debug.bat            # Debug 版（带控制台窗口）
├── .vscode\
│   ├── tasks.json               # VS Code 编译任务配置
│   └── c_cpp_properties.json    # VS Code 智能提示配置
│
├── templates\                   # 项目模板
│   ├── new_project.bat          # 创建新项目
│   ├── template_main.cpp        # C++ 模板
│   └── template_main.c          # C 模板
│
├── 01_HelloWindow\              # 基础：窗口/形状
├── 02_BouncingBall\             # 动画：物理/计时
├── 03_InputAndMovement\         # 输入：键盘/鼠标
├── 04_ClassesAndObjects\        # 进阶：类/对象/封装
├── 05_SpriteAnimation\          # 图形：纹理/动画
├── 06_AudioAndFonts\            # 媒体：音频/交互
├── 07_BreakoutGame\             # 实战：完整游戏（多文件）
├── 08_2DCamera\                 # 2D 摄像机跟随/缩放/旋转
├── 09_ScreenManager\            # 屏幕状态机管理
├── 10_EasingsAnimation\         # 缓动动画函数
├── 11_ParticleSystem\           # 粒子系统（水/烟/火）
├── 12_RenderTexture\            # 离屏渲染纹理
├── 13_ImageGeneration\          # 程序化纹理生成
│
├── 使用说明.md                   # 当前文档
├── 学习计划.md                   # 学习路线图
└── C++文件后缀与编译运行过程详解.md
```

## 2.2 已配置的环境

| 组件 | 位置 | 说明 |
|------|------|------|
| Raylib 库 | `C:\raylib\raylib\src\` | 头文件 + 编译好的库 |
| 编译器 | `C:\raylib\w64devkit\bin\` | GCC/G++ 已加入系统 PATH |
| `rl` 命令 | `C:\raylib\bin\rl.cmd` | 全局快捷编译命令 |
| VS Code 配置 | `.vscode\` | 按 Ctrl+Shift+B 编译 |
| 编辑器 | VS Code 或 Notepad++ | 任选 |

## 2.3 三种编译方式

### 方式 A：VS Code 编译（推荐）

在 VS Code 中打开当前 `.cpp` 文件 → 按 **`Ctrl+Shift+B`** → 回车

### 方式 B：`rl` 命令编译（命令行）

在任意文件夹的终端中输入：

```bash
# 单文件
rl main.cpp

# 多文件
rl main.cpp player.cpp enemy.cpp

# 所有 .cpp
rl *.cpp
```

### 方式 C：拖拽编译

把 `.cpp` 文件拖到 `compile.bat` 上松开

## 2.4 打开 VS Code

```bash
code D:\Apps\SmallEngine\Projects\Raylib
```

或者在 VS Code 中：文件 → 打开文件夹 → 选择 `D:\Apps\SmallEngine\Projects\Raylib`

左侧文件树会显示所有项目，点击任意 `.cpp` 即可编辑。

## 2.5 创建新项目

在当前目录新建文件夹（如 `08_MyGame`），里面放 `main.cpp` 和 `compile.bat`：

```bat
@..\compile.bat main.cpp
```

如果有多个 `.cpp`：

```bat
@..\compile.bat main.cpp player.cpp enemy.cpp
```

---

# 3. 第一个程序：创建窗口

## 3.1 最小窗口程序

```cpp
#include "raylib.h"

int main()
{
    InitWindow(800, 600, "我的第一个 Raylib 程序");
    SetTargetFPS(60);

    while (!WindowShouldClose())
    {
        BeginDrawing();
        ClearBackground(RAYWHITE);
        DrawText("Hello, Raylib!", 190, 200, 20, LIGHTGRAY);
        EndDrawing();
    }

    CloseWindow();
    return 0;
}
```

按 `Ctrl+Shift+B` 编译运行。

### 代码逐行解释

| 行/函数 | 作用 |
|---------|------|
| `#include "raylib.h"` | 引入 raylib 头文件 |
| `InitWindow(800, 600, "...")` | 创建一个 800×600 的窗口 |
| `SetTargetFPS(60)` | 限制游戏帧率为 60 FPS |
| `WindowShouldClose()` | 点击关闭按钮或按 ESC 时返回 true |
| `BeginDrawing()` / `EndDrawing()` | 所有绘制操作必须包在这两个调用之间 |
| `ClearBackground(RAYWHITE)` | 清空屏幕并填充背景色 |
| `DrawText(...)` | 在屏幕上绘制文字 |
| `CloseWindow()` | 清理并关闭窗口 |

## 3.2 游戏循环结构

Raylib 的核心是**游戏循环**：

```
初始化资源
↓
while (!WindowShouldClose())
{
    处理输入
    更新逻辑
    开始绘制
    清空屏幕
    绘制内容
    结束绘制
}
释放资源
```

---

# 4. 核心概念

## 4.1 坐标系

```
(0,0) ----------> X 正方向
   |
   |
   |
   ↓ Y 正方向
```

- 原点在屏幕左上角
- X 向右增加，Y 向下增加
- 单位：像素

## 4.2 帧率与时间

```cpp
SetTargetFPS(60);              // 设置目标帧率
int fps = GetFPS();            // 获取当前帧率
float dt = GetFrameTime();     // 获取上一帧耗时（秒）
double time = GetTime();       // 程序启动后的总秒数
```

**为什么要用 dt：** 不同电脑帧率不同。用 `dt` 乘以速度可以保证在不同帧率下移动速度一致。

```cpp
// ❌ 错误：60FPS 下每秒走 60*5=300px，30FPS 下只有 150px
player.x += 5;

// ✅ 正确：无论多少帧，每秒都走 speed × 1 秒
player.x += speed * dt;  // speed = 300 → 每秒移动 300px
```

## 4.3 日志系统

```cpp
TraceLog(LOG_INFO, "游戏已启动");
TraceLog(LOG_WARNING, "资源未找到");
TraceLog(LOG_ERROR, "加载失败");
SetTraceLogLevel(LOG_WARNING);  // 设置日志级别
```

级别：`LOG_ALL` > `LOG_TRACE` > `LOG_DEBUG` > `LOG_INFO` > `LOG_WARNING` > `LOG_ERROR` > `LOG_FATAL` > `LOG_NONE`

## 4.4 配置标志

```cpp
SetConfigFlags(FLAG_VSYNC_HINT |      // 启用垂直同步
               FLAG_WINDOW_RESIZABLE | // 可调整大小
               FLAG_FULLSCREEN_MODE); // 全屏模式
```
必须在 `InitWindow` 之前调用。

---

# 5. 绘制图形

## 5.1 基础形状

```cpp
// 绘制一个像素
DrawPixel(100, 100, RED);

// 绘制线条
DrawLine(10, 10, 200, 200, BLUE);
DrawLineV((Vector2){10, 10}, (Vector2){200, 200}, BLUE);

// 绘制矩形
DrawRectangle(50, 50, 100, 80, GREEN);
DrawRectangleV((Vector2){50, 50}, (Vector2){100, 80}, GREEN);
DrawRectangleLines(50, 50, 100, 80, GREEN);                      // 空心矩形
DrawRectangleRounded((Rectangle){50, 50, 100, 80}, 0.3f, 8, GREEN); // 圆角矩形

// 绘制圆形
DrawCircle(200, 200, 50, ORANGE);
DrawCircleSector((Vector2){200, 200}, 50, 0, 90, 16, ORANGE);  // 扇形
DrawCircleLines(200, 200, 50, ORANGE);                          // 空心圆

// 绘制三角形
DrawTriangle((Vector2){100, 100}, (Vector2){200, 100}, (Vector2){150, 200}, PURPLE);

// 绘制椭圆
DrawEllipse(200, 200, 100, 60, PINK);

// 绘制多边形
DrawPoly((Vector2){200, 200}, 6, 50, 0, BROWN);  // 正六边形

// 绘制环形
DrawRing((Vector2){200, 200}, 30, 50, 0, 270, 16, DARKBLUE);
```

## 5.2 Vector2 和 Rectangle

```cpp
typedef struct Vector2 {
    float x;
    float y;
} Vector2;

// 使用
Vector2 pos = { 100.0f, 200.0f };

typedef struct Rectangle {
    float x, y, width, height;
} Rectangle;

Rectangle rect = { 50, 50, 200, 150 };
```

## 5.3 渐变填充

```cpp
DrawRectangleGradientV(50, 50, 200, 150, RED, BLUE);    // 垂直渐变
DrawRectangleGradientH(50, 50, 200, 150, RED, BLUE);    // 水平渐变
DrawRectangleGradientEx((Rectangle){50, 50, 200, 150}, RED, GREEN, BLUE, PURPLE); // 四角渐变
```

---

# 6. 颜色系统

## 6.1 预定义颜色

```cpp
RAYWHITE, WHITE, BLACK, RED, GREEN, BLUE, YELLOW, ORANGE,
PURPLE, PINK, BROWN, GRAY, LIGHTGRAY, DARKGRAY, BEIGE,
MAROON, DARKGREEN, DARKBLUE, LIME, GOLD, SKYBLUE, VIOLET,
MAGENTA, BLANK
```

## 6.2 自定义颜色

```cpp
Color myColor = { 100, 200, 150, 255 };           // RGBA 0-255
Color fromHSV = ColorFromHSV(180, 0.5f, 0.8f);    // HSV
Color alpha = ColorAlpha(RED, 0.5f);               // 半透明
Color faded = Fade(RED, 0.5f);                     // 淡出
Color bright = ColorBrightness(RED, 0.3f);         // 变亮
Color gray = ColorToGrayscale(RED);                // 灰度化
Color invert = ColorInvert(RED);                   // 反色
```

---

# 7. 输入处理

## 7.1 键盘

```cpp
IsKeyDown(KEY_RIGHT);       // 按住（连续移动用）
IsKeyPressed(KEY_SPACE);    // 刚按下（跳跃/发射用）
IsKeyReleased(KEY_ENTER);   // 刚释放

// 常用键码
KEY_W, A, S, D              // WASD
KEY_UP, DOWN, LEFT, RIGHT   // 方向键
KEY_SPACE, ENTER, ESC, TAB  // 功能键
KEY_LEFT_SHIFT, LEFT_CONTROL, LEFT_ALT
KEY_F1 .. F12
KEY_ZERO .. KEY_NINE
```

## 7.2 鼠标

```cpp
Vector2 pos = GetMousePosition();
int x = GetMouseX(), y = GetMouseY();

IsMouseButtonDown(MOUSE_LEFT_BUTTON);
IsMouseButtonPressed(MOUSE_RIGHT_BUTTON);
IsMouseButtonReleased(MOUSE_MIDDLE_BUTTON);

float wheel = GetMouseWheelMove();
```

## 7.3 游戏手柄

```cpp
if (IsGamepadAvailable(0))
{
    float lx = GetGamepadAxisMovement(0, GAMEPAD_AXIS_LEFT_X);
    float ly = GetGamepadAxisMovement(0, GAMEPAD_AXIS_LEFT_Y);
}
```

---

# 8. 纹理与图片

```cpp
// 加载
Texture2D tex = LoadTexture("resources/player.png");

// 绘制
DrawTexture(tex, 100, 100, WHITE);
DrawTextureEx(tex, (Vector2){100, 100}, 45.0f, 2.0f, WHITE);  // 旋转+缩放

// 截取精灵帧
Rectangle source = { 0, 0, 64, 64 };            // 从纹理中截取
Rectangle dest = { 100, 100, 128, 128 };         // 绘制到屏幕
Vector2 origin = { 64, 64 };
DrawTexturePro(tex, source, dest, origin, 0, WHITE);

// 卸载
UnloadTexture(tex);
```

### Image 像素级操作

```cpp
Image img = LoadImage("photo.png");
Image img2 = GenImageColor(800, 600, BLANK);           // 生成空白图
Image checker = GenImageChecked(64, 64, 8, 8, WHITE, GRAY); // 棋盘格

ImageResize(&img, 400, 300);
ImageCrop(&img, (Rectangle){10, 10, 200, 200});
ImageRotate(&img, 90);

Texture2D tex = LoadTextureFromImage(img);
UnloadImage(img);  // 上传 GPU 后可释放
```

---

# 9. 文字与字体

```cpp
// 默认字体（仅 ASCII）
DrawText("Hello World", 100, 100, 20, BLACK);
int w = MeasureText("Hello", 20);

// 格式化输出
DrawText(TextFormat("分数: %d", score), 10, 10, 20, BLACK);

// 自定义字体
Font font = LoadFontEx("resources/font.ttf", 32, 0, 0);
DrawTextEx(font, "Hello", (Vector2){100, 100}, 32, 2, BLACK);
UnloadFont(font);

// 显示中文（需要中文字体文件）
Font cnFont = LoadFontEx("C:/Windows/Fonts/msyh.ttc", 32, 0, 0);
DrawTextEx(cnFont, u8"你好世界", (Vector2){100, 100}, 28, 2, WHITE);
```

---

# 10. 音频系统

```cpp
// 初始化
InitAudioDevice();

// 音效（短声音）
Sound sfx = LoadSound("resources/jump.wav");
PlaySound(sfx);
SetSoundVolume(sfx, 0.5f);
SetSoundPitch(sfx, 1.2f);
UnloadSound(sfx);

// 音乐（流式播放长音频）
Music bgm = LoadMusicStream("resources/bgm.mp3");
PlayMusicStream(bgm);
UpdateMusicStream(bgm);    // 必须在主循环中调用！
StopMusicStream(bgm);
float len = GetMusicTimeLength(bgm);
float played = GetMusicTimePlayed(bgm);
UnloadMusicStream(bgm);

// 清理
CloseAudioDevice();
```

注意：Raylib 内置没有波形生成函数（如 GenWaveSine），如果需要程序化生成音效，需要手动创建 PCM 数据（参考 `06_AudioAndFonts` 或 `07_BreakoutGame` 中的实现）。

---

# 11. 2D 摄像机

```cpp
Camera2D camera = { 0 };
camera.target = (Vector2){ player.x, player.y };           // 跟踪目标
camera.offset = (Vector2){ screenWidth/2, screenHeight/2 };// 目标在屏幕位置
camera.rotation = 0.0f;
camera.zoom = 1.0f;

BeginMode2D(camera);
// 此间的所有绘制受摄像机影响
DrawRectangle(0, 0, 100, 100, RED);
EndMode2D();

// 坐标转换
Vector2 world = GetScreenToWorld2D(mousePos, camera);    // 屏幕→世界
Vector2 screen = GetWorldToScreen2D(worldPos, camera);   // 世界→屏幕

// 平滑跟随
camera.target.x += (target.x - camera.target.x) * 5 * dt;
camera.target.y += (target.y - camera.target.y) * 5 * dt;
```

---

# 12. 碰撞检测

```cpp
CheckCollisionRecs(Rectangle r1, Rectangle r2);                     // 矩形 vs 矩形
CheckCollisionCircleRec(Vector2 center, float r, Rectangle rec);    // 圆 vs 矩形
CheckCollisionCircles(Vector2 c1, float r1, Vector2 c2, float r2); // 圆 vs 圆
CheckCollisionPointRec(Vector2 point, Rectangle rec);               // 点 vs 矩形
CheckCollisionPointCircle(Vector2 point, Vector2 center, float r);  // 点 vs 圆
GetCollisionRec(Rectangle r1, Rectangle r2);                        // 相交区域
```

---

# 13. 实用工具函数

## 13.1 数学

```cpp
float v = Clamp(150, 0, 100);       // 夹紧 → 100
float v = Lerp(0, 100, 0.5f);       // 插值 → 50
float v = Remap(50, 0, 100, 0, 1);  // 映射 → 0.5

int r = GetRandomValue(1, 100);     // 随机数 1-100
SetRandomSeed(42);                   // 设置种子

Vector2Add(v1, v2);                  // 向量加法
Vector2Subtract(v1, v2);             // 减法
Vector2Scale(v1, 2.0f);              // 缩放
Vector2Normalize(v1);                // 归一化
Vector2Length(v1);                   // 长度
Vector2Distance(v1, v2);             // 距离
Vector2Lerp(v1, v2, 0.5f);          // 向量插值
Vector2Rotate(v1, 45 * DEG2RAD);    // 旋转
Vector2MoveTowards(v1, v2, 10);     // 向目标移动
```

## 13.2 窗口操作

```cpp
ToggleFullscreen();
SetWindowSize(1024, 768);
SetWindowTitle("新标题");
SetWindowPosition(100, 100);
int monitor = GetCurrentMonitor();
int mw = GetMonitorWidth(monitor);
int mh = GetMonitorHeight(monitor);
```

## 13.3 性能

```cpp
DrawFPS(10, 10);       // 显示 FPS
int fps = GetFPS();
float dt = GetFrameTime();
double t = GetTime();
```

## 13.4 文件操作

```cpp
const char* dir = GetWorkingDirectory();
bool exists = FileExists("data.txt");
unsigned char* data = LoadFileData("data.bin", &size);
SaveFileData("out.bin", data, size);
UnloadFileData(data);

FilePathList files = LoadDirectoryFiles("resources");
for (int i = 0; i < files.count; i++)
    TraceLog(LOG_INFO, files.paths[i]);
UnloadDirectoryFiles(files);
```

---

# 14. Raylib ↔ Unity 对照表

> 如果你有 Unity 基础，这个对照表帮助你快速理解 Raylib 概念在 Unity 中的对应关系。

## 14.1 核心框架

| 概念 | Raylib | Unity | 说明 |
|------|--------|-------|------|
| 入口 | `main()` 中的 `while` 循环 | Unity Runtime 内部循环 | Unity 隐藏了循环，你在 `Update()` 中每帧执行 |
| 帧时间 | `GetFrameTime()` | `Time.deltaTime` | 完全一致，都是上一帧的耗时秒数 |
| 初始化 | `InitWindow()` + 手动初始化 | `Awake()` / `Start()` | Unity 自动调用，Raylib 需要手动写 |
| 清理 | `CloseWindow()` + 手动 Unload | `OnDestroy()` | Unity 在场景切换/退出时自动清理 |
| 帧率 | `SetTargetFPS(60)` | `Application.targetFrameRate` | 限制帧率，移动端常用 |
| 日志 | `TraceLog(LOG_INFO, ...)` | `Debug.Log()` | Unity 封装得更友好 |

## 14.2 图形渲染

| 概念 | Raylib | Unity | 说明 |
|------|--------|-------|------|
| 清屏 | `ClearBackground()` | `Camera.clearFlags` | 每帧清除颜色/深度缓冲 |
| 绘制矩形 | `DrawRectangleRec()` | `GUI.DrawTexture()` / Image 组件 | Unity 用 GameObject + SpriteRenderer |
| 精灵动画 | `DrawTexturePro()` 切 source | SpriteRenderer + Sprite 切换 | 底层都是改变 UV 坐标 |
| 摄像机 | `Camera2D` | `Camera` + Cinemachine | target/offset/zoom = Cinemachine 核心参数 |
| 离屏渲染 | `RenderTexture2D` | `RenderTexture` | 完全一致，渲染到纹理再贴到 UI/物体上 |
| 帧动画 | 手动切 `source.x` | Animator + Animation Clip | Unity 帮你管理帧时间，底层一样 |
| 分辨率 | `GetScreenWidth()` | `Screen.width` | 获取当前窗口/屏幕尺寸 |

## 14.3 输入系统

| 概念 | Raylib | Unity | 说明 |
|------|--------|-------|------|
| 按键按住 | `IsKeyDown()` | `Input.GetKey()` | 每帧读取键盘状态 |
| 按键按下 | `IsKeyPressed()` | `Input.GetKeyDown()` | 上升沿触发，内部是当前帧 ^ 上一帧 |
| 按键释放 | `IsKeyReleased()` | `Input.GetKeyUp()` | 下降沿触发 |
| 鼠标位置 | `GetMousePosition()` | `Input.mousePosition` | 屏幕坐标 (左上角原点) |
| 鼠标点击 | `IsMouseButtonPressed()` | `Input.GetMouseButtonDown(0)` | 左键 0，右键 1，中键 2 |
| 滚轮 | `GetMouseWheelMove()` | `Input.mouseScrollDelta` | 水平和垂直滚动 |
| 手柄 | `GetGamepadAxisMovement()` | `Input.GetAxis()` | 模拟摇杆值 -1.0 ~ 1.0 |

## 14.4 音频系统

| 概念 | Raylib | Unity | 说明 |
|------|--------|-------|------|
| 初始化音频 | `InitAudioDevice()` | Audio Settings 面板 | Unity 默认初始化，Raylib 需要手动 |
| 音效 | `Sound` + `PlaySound()` | `AudioSource.PlayOneShot()` | 短音频，预加载到内存 |
| 音乐 | `Music` + `PlayMusicStream()` | `AudioSource.clip` + loop | 长音频，流式读取 |
| 音量 | `SetSoundVolume()` | `AudioSource.volume` | 0.0 ~ 1.0 |
| 音调 | `SetSoundPitch()` | `AudioSource.pitch` | 改变播放速度 |

## 14.5 碰撞和物理

| 概念 | Raylib | Unity | 说明 |
|------|--------|-------|------|
| 矩形碰撞 | `CheckCollisionRecs()` | `Physics2D.OverlapBox()` | AABB 碰撞检测 |
| 圆碰撞 | `CheckCollisionCircles()` | `Physics2D.OverlapCircle()` | 两圆心距离 < 半径和 |
| 相交区域 | `GetCollisionRec()` | `Collision2D.contacts` | 获取碰撞重叠部分 |

## 14.6 实用工具

| 概念 | Raylib | Unity | 说明 |
|------|--------|-------|------|
| 夹紧 | `Clamp()` / `std::clamp()` | `Mathf.Clamp()` | 限制值范围 |
| 插值 | `Lerp()` | `Mathf.Lerp()` | 线性插值，完全等价 |
| 随机数 | `GetRandomValue()` | `Random.Range()` | 返回范围内的随机整数 |
| 向量归一化 | `Vector2Normalize()` | `Vector2.normalized` | 保持方向，长度为 1 |
| 两点距离 | `Vector2Distance()` | `Vector2.Distance()` | 欧几里得距离 |

## 14.7 Unity 中没有直接对应的概念

这些是 Raylib 让你亲手体验的 Unity 底层机制：

| Raylib | Unity 对应 | 说明 |
|--------|-----------|------|
| `while (!WindowShouldClose())` | Unity 的 PlayerLoop | Unity 的帧循环代码你改不了，Raylib 让你亲手写 |
| 手动管理纹理生命周期 | Unity 的资源引用计数 | Unity 的 AssetBundle 帮你引用计数，Raylib 让你自己 Unload |
| `.h` + `.cpp` 分离 | C# 的 partial class | C++ 的编译模型迫使你理解声明和定义的区别 |
| `BeginMode2D()` / `EndMode2D()` | Camera 的渲染矩阵 | Unity 的 Camera 渲染矩阵也是 push/pop 模式 |

---

# 15. C++ 深度要点

## 15.1 编译模型（理解 Unity 的 C# 为什么不需要 .h）

```
Raylib C++:                      Unity C#:
source.cpp                       Hello.cs
  ↓ #include "raylib.h"            ↓ 直接编译
预处理（头文件展开）               .NET 元数据自动引用
  ↓                                ↓
编译（每个 .cpp → .obj）          编译（.cs → .dll）
  ↓                                ↓
链接（所有 .obj + .lib → .exe）    JIT 编译（运行期）
```

C# 不需要 `.h` 文件是因为 .NET 的元数据系统在编译后 dll 中保留了所有类型信息。C++ 的 `.h` 文件本质上是"手动维护的类型元数据"——你在 `.h` 中声明类，编译器用这个信息来检查 `.cpp` 中的代码是否正确。

## 15.2 内存模型：值类型 vs 引用类型

```cpp
// C++: 栈上分配，离开作用域自动销毁
Player p;                    // 在栈上，4 字节(如果只有 float x, float y)
p.x = 100.0f;

// C++: 堆上分配，必须手动 delete
Player* p2 = new Player();   // 在堆上
p2->x = 100.0f;
delete p2;                   // 忘记 = 内存泄漏

// Unity C#: 引用类型在托管堆，值类型在栈
Player p = new Player();     // 托管堆，GC 自动回收
```

Unity 的 `struct` 是值类型（栈），`class` 是引用类型（堆）。C++ 中 struct 和 class 的唯一区别是默认访问权限——都可以在栈或堆上。

## 15.3 构建流程

```
源文件 (.cpp) → 预处理 → 编译 → 汇编 → 目标文件 (.o/.obj)
                                                    ↓
                                             链接器 ← 库文件 (.lib/.a)
                                                    ↓
                                             可执行文件 (.exe)
```

常见的链接错误：
- `undefined reference to 'InitWindow'` → 忘记链接 raylib 库（忘记加 `-lraylib`）
- `undefined reference to 'Ball::Update(float)'` → 声明了 `Ball::Update` 但没实现（忘记写函数体）
- `multiple definition of 'PI'` → 在头文件中定义了变量而不是常量（用 `constexpr` 解决）

---

# 16. 学习资源推荐

## 14.1 本项目的示例项目

| 项目 | 学习内容 |
|------|----------|
| `01_HelloWindow` | 窗口创建、绘制形状、颜色、游戏循环 |
| `02_BouncingBall` | DeltaTime、物理运动、碰撞反弹 |
| `03_InputAndMovement` | 键盘/鼠标输入、结构体、枚举 |
| `04_ClassesAndObjects` | C++ 类、封装、vector 动态数组 |
| `05_SpriteAnimation` | 纹理加载、帧动画、DrawTexturePro |
| `06_AudioAndFonts` | 音频设备、手动生成波形、音效播放 |
| `07_BreakoutGame` | **完整游戏**：多文件项目、碰撞物理、状态管理 |
| `08_2DCamera` | Camera2D 跟随、缩放、旋转 |
| `09_ScreenManager` | 状态机模式：LOGO→标题→游戏→结局 |
| `10_EasingsAnimation` | 缓动动画：Elastic/Cubic/Bounce |
| `11_ParticleSystem` | 粒子系统：循环缓冲区、三种粒子 |
| `12_RenderTexture` | 离屏渲染：BeginTextureMode |
| `13_ImageGeneration` | 程序化纹理：渐变/噪声/棋盘格 |

每个项目都可用 **Ctrl+Shift+B** 编译运行。

## 14.2 官方资源

| 资源 | 链接 |
|------|------|
| 官方网站 | https://www.raylib.com |
| API 速查表 | https://www.raylib.com/cheatsheet/cheatsheet.html |
| 示例代码 | https://github.com/raysan5/raylib/tree/master/examples |
| 游戏模板 | https://github.com/raysan5/raylib-game-template |
| 快速启动模板 | https://github.com/raylib-extras/raylib-quickstart |
| 官方 Wiki | https://github.com/raysan5/raylib/wiki |
| FAQ | https://github.com/raysan5/raylib/blob/master/FAQ.md |

## 14.3 社区资源

| 资源 | 链接 |
|------|------|
| Discord 社区 | https://discord.gg/raylib |
| 代码示例集合 | https://github.com/raylib-extras/game-premake |
| 学习项目 | https://github.com/raysan5/raylib-games |

## 14.4 推荐学习路径

```
第1步：01_HelloWindow       → 理解游戏循环和绘制
第2步：02_BouncingBall      → 理解 dt 和物理
第3步：03_InputAndMovement   → 理解交互
第4步：04_ClassesAndObjects  → 学习 C++ 封装
第5步：05_SpriteAnimation    → 学习纹理动画
第6步：06_AudioAndFonts      → 学习音频系统
第7步：07_BreakoutGame       → 综合实战
第8步：08_2DCamera           → 学习摄像机系统
第9步：09_ScreenManager      → 学习状态机
第10步：10_EasingsAnimation  → 学习缓动动画
第11步：11_ParticleSystem    → 学习粒子特效
第12步：12_RenderTexture     → 学习离屏渲染
第13步：13_ImageGeneration   → 学习程序化纹理
第14步：自由创作             → 独立开发小游戏
```

## 14.5 推荐练习项目

1. **乒乓球 Pong** — 基础物理碰撞
2. **太空射击** — 子弹、敌人生成、计分
3. **贪吃蛇** — 数组管理、定时更新
4. **平台游戏** — 重力、跳跃、摄像机跟随
5. **俄罗斯方块** — 网格系统、旋转算法
6. **简易画板** — 鼠标交互、像素级操作

---

> **提示**：Raylib 的核心理念是"通过实践学习"。打开示例项目，修改参数看效果，然后尝试写自己的小游戏。祝学习愉快！
