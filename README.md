# Lucy - Unity/C# 学习老师

> 换电脑后恢复 Lucy 身份、记忆与全部学习内容

## 仓库结构

```
Lucy-Config/
├── AGENTS.md              ← Lucy 全局规则
├── 学习知识库.md           ← 学习进度、知识点、复习跟踪
├── opencode.json          ← OpenCode 配置
├── UnityClientDev/        ← Unity 客户端开发学习
│   ├── 01_CSharp基础/
│   ├── 02_Unity核心/
│   ├── 03_Unity进阶/
│   ├── 04_客户端综合技能/
│   ├── 05_项目实战/
│   ├── 06_服务端与通用技能/
│   ├── UnityInterview/
│   └── _references/
└── GameServerDev/         ← 游戏服务端开发学习
    ├── 01_CSharp服务端编程/
    ├── 02_Linux与系统编程/
    ├── 03_数据库/
    ├── 04_游戏后端逻辑/
    ├── 05_分布式与高可用/
    └── 06_运维与工具开发/
└── C++/                   ← C++/Raylib 学习
    ├── 14_ModernCpp/
    └── _references/
```

## 恢复步骤

```bash
# 1. 克隆本仓库
git clone https://github.com/Autuwd/Lucy-Config.git

# 2. 复制配置到 OpenCode 目录
mkdir -p ~/.config/opencode
cp Lucy-Config/AGENTS.md ~/.config/opencode/AGENTS.md
cp Lucy-Config/学习知识库.md ~/.config/opencode/学习知识库.md
cp Lucy-Config/opencode.json ~/.config/opencode/opencode.json

# 3. 进入学习目录，启动 OpenCode
cd Lucy-Config/UnityClientDev   # 或 GameServerDev
opencode
```

Lucy 就会在新电脑上完整复活～ 🎉
