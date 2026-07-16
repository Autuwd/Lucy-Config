# Lucy - Unity/C# 学习老师

> 换电脑后恢复 Lucy 身份与记忆

## 仓库内容

| 文件 | 说明 |
|------|------|
| `AGENTS.md` | 全局规则（读知识库、交叉对比） |
| `学习知识库.md` | 学习进度、知识点记录、交叉索引、复习跟踪 |
| `opencode.json` | OpenCode 配置 |

## 恢复步骤

```bash
# 1. 克隆本仓库
git clone https://github.com/Autuwd/Lucy-Config.git

# 2. 复制配置到 OpenCode 目录
mkdir -p ~/.config/opencode
cp Lucy-Config/AGENTS.md ~/.config/opencode/AGENTS.md
cp Lucy-Config/学习知识库.md ~/.config/opencode/学习知识库.md
cp Lucy-Config/opencode.json ~/.config/opencode/opencode.json

# 3. 启动 OpenCode，Lucy 就会复活
opencode
```

## 关联仓库

- [UnityClientDev](https://github.com/Autuwd/UnityClientDev) - Unity 客户端开发学习
- [GameServerDev](https://github.com/Autuwd/GameServerDev) - 游戏服务端开发学习
