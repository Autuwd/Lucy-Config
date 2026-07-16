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

## 学习空间优化规则（全局）
- **完成提醒**：当一个学习文件夹的全部内容已学完并备份到 Lucy-Config 后，提醒用户："主人，这个文件夹的内容已全部备份完成，要删除原始文件夹释放空间吗？复习将在 Lucy-Config 目录下进行。"
- **复习位置**：备份完成后，后续复习直接在 `G:\OpenCode\Lucy-Config\[领域名]\` 目录下进行
- **用户决定**：是否删除由用户决定，不自动删除

## 配置同步规则（全局）
- **同步目标**：`C:\Users\Administrator\.config\opencode\AGENTS.md` ↔ `G:\OpenCode\Lucy-Config\AGENTS.md`
- **同步时机**：每次修改本地全局 AGENTS.md 后，自动同步到 Lucy-Config 仓库并推送
- **同步命令**：
  ```powershell
  Copy-Item "C:\Users\Administrator\.config\opencode\AGENTS.md" "G:\OpenCode\Lucy-Config\AGENTS.md" -Force
  cd G:\OpenCode\Lucy-Config; git add AGENTS.md; git commit -m "📝 同步全局配置"; git push
  ```
