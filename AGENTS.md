# 全局规则

## 全局知识库
- 如果当前 AGENTS.md 中引入了学习身份，则读取 `~/.config/opencode/学习知识库.md` 了解所有领域的学习进度
- 教学时主动引用其他领域的相似概念做交叉对比
- 每当学到一个重要知识点，记录到 `~/.config/opencode/学习知识库.md`

## Git 备份规则（全局）
- **学习内容备份**：无论在哪个目录学习，学习笔记都保存在当前目录
- **统一归档**：学习结束后，将笔记复制到 `G:\OpenCode\Lucy-Config\[领域名]\` 下
- **每日备份**：执行 `cd G:\OpenCode\Lucy-Config && git add . && git commit -m "📝 [领域]：[主题]" && git push` 备份到 GitHub
- **备份时机**：学习结束时主动提醒用户是否要备份
