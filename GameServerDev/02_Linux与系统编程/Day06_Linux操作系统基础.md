# Day 06：Linux 操作系统基础

## 一、为什么游戏服务器用 Linux

| 特性 | Linux | Windows Server |
|------|-------|---------------|
| 稳定性 | 数年起无需重启 | 需要定期补丁重启 |
| 性能 (epoll) | 万级连接轻松处理 | IOCP 也不错但成本高 |
| 资源占用 | 无 GUI，256MB 够跑 | 最少 2GB |
| 成本 | 免费 | 需要 License 费用 |
| 远程管理 | SSH 高效 | RDP 远程桌面 |
| 容器化 | Docker/K8s 原生支持 | WSL 兼容层 |

国内主流游戏服务器基本全是 **Linux + Docker** 部署。

---

## 二、SSH 与远程连接

### 基础 SSH

```bash
# 连接服务器
ssh root@192.168.1.100
ssh -p 2222 root@192.168.1.100  # 指定端口

# 密钥认证
ssh-keygen -t ed25519 -C "game-server-key"  # 生成密钥
ssh-copy-id root@192.168.1.100               # 复制公钥到服务器

# 安全配置 (/etc/ssh/sshd_config)
# PasswordAuthentication no   # 禁止密码登录
# PermitRootLogin no          # 禁止 root 直接登录
# Port 2222                   # 修改默认端口防扫描

# 免密执行命令
ssh root@192.168.1.100 "systemctl status game-server"

# 端口转发（本地 8888 → 远程 8888）
ssh -L 8888:localhost:8888 root@192.168.1.100

# SCP 文件传输
scp local.conf root@192.168.1.100:/etc/game-server/
scp -r logs/ root@192.168.1.100:/backup/logs/

# rsync 增量同步
rsync -avz --progress ./bin/ root@192.168.1.100:/opt/game-server/bin/
```

### Screen / Tmux (保持会话)

```bash
# screen
screen -S game-server           # 创建会话
# Ctrl+A, D 脱离
screen -r game-server           # 重新接入
screen -ls                      # 列出会话

# tmux (更推荐)
tmux new -s game-server         # 创建
tmux detach                     # Ctrl+B, D
tmux attach -t game-server      # 重新接入
tmux ls                         # 列出
```

---

## 三、文件系统与权限

### 目录结构

```bash
/           # 根目录
├── etc/        # 配置文件
├── var/        # 可变数据（日志、数据库）
│   └── log/    # 日志文件
├── opt/        # 可选软件（推荐游戏服务器放这里）
├── home/       # 用户目录
├── tmp/        # 临时文件（重启后清空）
└── data/       # （自定义）游戏数据目录
```

### 游戏服务器目录布局

```bash
/opt/game-server/
├── bin/            # 可执行文件
│   ├── server      # 主服务器程序
│   └── tools/      # 运维工具
├── config/         # 配置文件
│   ├── server.json
│   └── log.json
├── data/           # 运行时数据
│   ├── db/         # 数据库文件/备份
│   └── cache/      # 缓存文件
├── logs/           # 日志文件
│   ├── server-2026-07-15.log
│   └── error.log
└── scripts/        # 启动脚本
    ├── start.sh
    ├── stop.sh
    └── backup.sh
```

### 文件权限

```bash
# 权限格式: rwxr-xr-x (755)
#           用户 组 其他

chmod 755 server          # 可执行文件
chmod 644 server.json     # 配置文件（所有人可读，仅自己可写）
chmod 600 private.key     # 密钥文件（仅自己可读写）

chown game:game /opt/game-server/   # 改变拥有者
chown -R game:game logs/            # 递归修改

# 特殊权限
chmod +s server          # SUID（以文件所有者身份执行）
chmod +t logs/           # Sticky Bit（只有文件所有者可删除）
```

---

## 四、进程管理

```bash
# 查看进程
ps aux                       # 所有进程
ps aux | grep game-server    # 过滤
ps -ef --forest              # 树形显示父子关系
top                          # 动态进程监控
htop                         # top 的增强版（需安装）

# 进程信息
cat /proc/1234/status        # 进程 1234 的状态
cat /proc/1234/fd/           # 打开的文件描述符
cat /proc/1234/limits        # 进程资源限制

# 信号
kill -9 1234                 # SIGKILL - 强制杀死
kill -15 1234                # SIGTERM - 请求退出
kill -2 1234                 # SIGINT - Ctrl+C
kill -HUP 1234               # SIGHUP - 重载配置

# nohup (退出终端后继续运行)
nohup ./game-server > server.log 2>&1 &
```

### ULIMIT (进程资源限制)

```bash
# 查看当前限制
ulimit -a
# core file size          (blocks, -c) 0
# data seg size           (kbytes, -d) unlimited
# open files                      (-n) 1024    ← 游戏服务器需要调大

# 临时修改 (当前会话)
ulimit -n 65535

# 永久修改 /etc/security/limits.conf
game soft nofile 65535
game hard nofile 65535
game soft nproc 65535
game hard nproc 65535

# 系统级最大文件数
cat /proc/sys/fs/file-max
sysctl -w fs.file-max=100000
# 永久: echo "fs.file-max=100000" >> /etc/sysctl.conf
```

---

## 五、网络工具

```bash
# netstat (查看端口占用)
netstat -tlnp                    # 所有监听中的 TCP 端口
netstat -tan | grep ESTABLISHED  # 已建立的连接数

# ss (netstat 的现代替代，更快)
ss -tlnp                         # 监听 TCP 端口
ss -s                            # 连接统计
ss -tn state established         # 已建立的连接

# lsof (查看打开的文件)
lsof -i :8888                    # 谁在使用端口 8888
lsof -p 1234                     # 进程 1234 打开的文件
lsof  -u game                     # 游戏用户打开的文件

# 网络测试
ping -c 5 baidu.com              # 连通性测试
curl -v http://127.0.0.1:8888    # HTTP 测试
telnet 127.0.0.1 8888            # TCP 连通测试
nc -vz 127.0.0.1 8888            # nc 端口测试
mtr 192.168.1.100                # 路由追踪 + ping

# tcpdump (抓包分析)
tcpdump -i eth0 port 8888        # 监听网卡 eth0 的 8888 端口
tcpdump -i any -nn port 8888     # 显示 IP 不解析域名
tcpdump -w capture.pcap          # 保存到文件（用 Wireshark 打开）
```

---

## 六、Shell 脚本

### 启动脚本模板

```bash
#!/bin/bash
# start.sh - 游戏服务器启动脚本
set -euo pipefail

APP_NAME="game-server"
APP_DIR="/opt/game-server"
CONFIG="$APP_DIR/config/server.json"
PID_FILE="$APP_DIR/$APP_NAME.pid"
LOG_DIR="$APP_DIR/logs"
BINARY="$APP_DIR/bin/$APP_NAME"

# 日志函数
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" >> "$LOG_DIR/startup.log"
}

# 检查是否已在运行
check_running() {
    if [ -f "$PID_FILE" ]; then
        pid=$(cat "$PID_FILE")
        if kill -0 "$pid" 2>/dev/null; then
            log "服务器已在运行 (PID: $pid)"
            exit 1
        fi
        rm -f "$PID_FILE"
    fi
}

# 启动前检查
pre_start_check() {
    # 检查配置文件
    if [ ! -f "$CONFIG" ]; then
        log "错误: 配置文件 $CONFIG 不存在"
        exit 1
    fi

    # 检查可执行文件
    if [ ! -x "$BINARY" ]; then
        log "错误: $BINARY 不可执行"
        exit 1
    fi

    # 创建日志目录
    mkdir -p "$LOG_DIR"

    # 检查端口占用
    PORT=$(cat "$CONFIG" | grep -o '"Port":[0-9]*' | grep -o '[0-9]*')
    if ss -tlnp | grep -q ":$PORT "; then
        log "错误: 端口 $PORT 已被占用"
        exit 1
    fi
}

# 启动
start() {
    check_running
    pre_start_check

    # 使用 nohup 启动
    nohup "$BINARY" --config "$CONFIG" >> "$LOG_DIR/$APP_NAME.log" 2>&1 &
    pid=$!
    echo $pid > "$PID_FILE"

    # 等待确认启动成功
    sleep 2
    if kill -0 "$pid" 2>/dev/null; then
        log "服务器启动成功 (PID: $pid, 端口: $PORT)"
    else
        log "服务器启动失败"
        exit 1
    fi
}

# 停止
stop() {
    if [ ! -f "$PID_FILE" ]; then
        log "服务器未在运行"
        exit 0
    fi

    pid=$(cat "$PID_FILE")
    log "正在停止服务器 (PID: $pid)..."

    # 先发 SIGTERM 优雅退出
    kill -15 "$pid" 2>/dev/null || true

    # 等待最多 10 秒
    for i in $(seq 1 10); do
        if ! kill -0 "$pid" 2>/dev/null; then
            rm -f "$PID_FILE"
            log "服务器已停止"
            return 0
        fi
        sleep 1
    done

    # 超过 10 秒强制杀死
    log "强制杀死服务器 (PID: $pid)"
    kill -9 "$pid" 2>/dev/null || true
    rm -f "$PID_FILE"
}

# 重启
restart() {
    stop
    sleep 2
    start
}

# 状态
status() {
    if [ -f "$PID_FILE" ]; then
        pid=$(cat "$PID_FILE")
        if kill -0 "$pid" 2>/dev/null; then
            echo "运行中 (PID: $pid)"
            echo "进程信息:"
            ps -p "$pid" -o pid,ppid,%cpu,%mem,rss,start,etime,cmd
        else
            echo "PID 文件存在但进程已死"
        fi
    else
        echo "未运行"
    fi
}

case "${1:-help}" in
    start)   start ;;
    stop)    stop ;;
    restart) restart ;;
    status)  status ;;
    *)
        echo "用法: $0 {start|stop|restart|status}"
        exit 1
        ;;
esac
```

### 备份脚本

```bash
#!/bin/bash
# backup.sh - 数据库和日志备份
set -euo pipefail

BACKUP_DIR="/backup/$(date +%Y%m%d)"
mkdir -p "$BACKUP_DIR"

# 备份日志（压缩归档）
tar -czf "$BACKUP_DIR/logs.tar.gz" /opt/game-server/logs/

# 备份数据库
mysqldump -u root -p game_db > "$BACKUP_DIR/game_db.sql"

# 备份配置文件
cp -r /opt/game-server/config "$BACKUP_DIR/config"

# 清理 7 天前的备份
find /backup -maxdepth 1 -type d -mtime +7 -exec rm -rf {} \;

echo "备份完成: $BACKUP_DIR"
```

---

## 七、Systemd 服务管理

### Service 文件

```ini
# /etc/systemd/system/game-server.service
[Unit]
Description=Game Server
After=network.target mysql.service redis.service
Wants=mysql.service redis.service

[Service]
Type=simple
User=game
Group=game
WorkingDirectory=/opt/game-server
ExecStart=/opt/game-server/bin/server --config /opt/game-server/config/server.json
ExecReload=/bin/kill -HUP $MAINPID
Restart=on-failure
RestartSec=5
LimitNOFILE=65535
LimitNPROC=65535

# 日志管理
StandardOutput=append:/opt/game-server/logs/out.log
StandardError=append:/opt/game-server/logs/error.log

# 安全
PrivateTmp=true
NoNewPrivileges=true

[Install]
WantedBy=multi-user.target
```

### Systemd 常用命令

```bash
systemctl daemon-reload              # 重新加载 service 文件
systemctl start game-server          # 启动
systemctl stop game-server           # 停止
systemctl restart game-server        # 重启
systemctl status game-server         # 状态
systemctl enable game-server         # 开机自启
systemctl disable game-server        # 取消开机自启

journalctl -u game-server            # 查看日志
journalctl -u game-server -f         # 实时跟踪日志
journalctl -u game-server --since "1 hour ago"  # 最近 1 小时
```

---

## 八、对比 Windows 管理

| Linux | Windows | 用途 |
|-------|---------|------|
| `ssh` | `winrm`/RDP | 远程管理 |
| `ps aux` | `tasklist` | 查看进程 |
| `kill` | `taskkill` | 杀死进程 |
| `top/htop` | 任务管理器 | 动态监控 |
| `netstat -tlnp` | `netstat -ano` | 查看端口 |
| `ss` | `netstat` + `Get-NetTCPConnection` | 连接统计 |
| `systemctl` | `sc` / `Start-Service` | 服务管理 |
| `nohup &` | `Start-Process -NoNewWindow` | 后台运行 |
| `chmod 755` | `icacls` | 文件权限 |
| `nproc`/`ulimit -n` | `Get-CimInstance Win32_ComputerSystem` | 资源限制 |

---

## 九、练习

1. **SSH 配置**：在自己云服务器上配置密钥登录 + 禁止密码 + 改端口
2. **启动脚本**：为假设的游戏服务器写一个完整的 start/stop/status 脚本
3. **Systemd 服务**：将游戏服务器配置为 systemd 服务并设为开机自启
4. **抓包分析**：用 tcpdump 抓取游戏客户端的通信包，用 Wireshark 分析
5. **Shell 自动化**：写一个批量部署脚本，将二进制从本地 SCP 到 3 台服务器

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| SSH | 远程连接 Linux 的标准方式 |
| PS | 进程管理三板斧：ps/top/kill |
| netstat/ss | 端口和网络连接诊断 |
| ULIMIT | 服务器必须调大 open files |
| Shell 脚本 | 运维自动化的基础 |
| Systemd | 现代 Linux 的服务管理标准 |
