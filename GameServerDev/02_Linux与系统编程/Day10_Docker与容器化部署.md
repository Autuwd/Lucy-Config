# Day 10：Docker 与容器化部署

## 一、为什么游戏服务器用容器

| 问题 | 传统部署 | Docker 容器 |
|------|---------|-------------|
| 环境不一致 | "在我机器上能跑" | 镜像即环境，完全一致 |
| 资源隔离 | 进程相互影响 | 容器独立 CPU/内存 |
| 扩缩容 | 手动部署，慢 | 秒级启动，编排伸缩 |
| 版本管理 | 手动记录版本 | 镜像 Tag + Registry |
| 依赖管理 | 手动安装库 | Dockerfile 声明依赖 |
| 迁移 | 重新配置 | 镜像推送即迁移 |

---

## 二、Docker 核心概念

```
Image (镜像)       →  只读模板，包含 OS + 依赖 + 应用
   │ docker run
Container (容器)   →  镜像的运行实例，可读写层
   │ docker commit → 生成新镜像
Registry (仓库)    →  存储分发镜像 (Docker Hub / 私有仓库)

Dockerfile         →  描述如何构建镜像的脚本
docker-compose.yml →  定义多容器编排
```

---

## 三、Dockerfile 编写

### 游戏服务器 Dockerfile

```dockerfile
# 多阶段构建

# === 构建阶段 ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 先复制项目文件，利用缓存
COPY *.sln .
COPY GameServer/*.csproj ./GameServer/
RUN dotnet restore

# 复制源码并构建
COPY . .
RUN dotnet publish -c Release -o /app

# === 运行阶段 ===
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# 复制构建产物
COPY --from=build /app .

# 创建非 root 用户（安全）
RUN adduser --disabled-password --gecos '' gameuser
USER gameuser

# 暴露端口
EXPOSE 8888/tcp
EXPOSE 8889/udp

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -f http://localhost:8888/health || exit 1

# 启动
ENTRYPOINT ["dotnet", "GameServer.dll"]
```

### C++ 服务器 Dockerfile

```dockerfile
FROM ubuntu:22.04 AS build
RUN apt-get update && apt-get install -y \
    build-essential cmake libboost-all-dev \
    && rm -rf /var/lib/apt/lists/*

COPY . /src
WORKDIR /src/build
RUN cmake .. && make -j$(nproc)

FROM ubuntu:22.04 AS runtime
RUN apt-get update && apt-get install -y \
    libssl3 libboost-system1.74.0 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /src/build/server /app/server
EXPOSE 8888
ENTRYPOINT ["/app/server"]
```

### 最佳实践

```dockerfile
# 1. 合并 RUN 指令减少层数
RUN apt-get update && apt-get install -y \
    package1 package2 \
    && rm -rf /var/lib/apt/lists/*  # 清理缓存减少体积

# 2. .dockerignore 忽略无关文件
# .dockerignore:
#   bin/
#   obj/
#   .git/
#   *.log

# 3. 利用构建缓存（不常变的在前）
COPY packages.config .
RUN restore
COPY src/ .          # 经常变的在后

# 4. 多阶段构建（build 镜像大，runtime 镜像小）
# build 镜像可能 2GB，runtime 镜像仅 ~200MB

# 5. 非 root 运行
RUN adduser -D appuser
USER appuser
```

---

## 四、Docker 镜像优化

```bash
# 查看镜像大小
docker images
# REPOSITORY        TAG       IMAGE ID       SIZE
# game-server       1.0        abc123        198MB
# game-server       dev        def456        1.2GB  ← 太大了！

# 找出空间去向
docker history game-server:1.0
# IMAGE          CREATED      CREATED BY                  SIZE
# abc123         2 min ago    CMD ["dotnet" "GameServ…"]  0B
# ...            ...          COPY --from=build /app /    89MB
# ...

# 体积优化方法
# 1. 使用 alpine 基础镜像（~5MB vs ~200MB）
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine

# 2. 只复制需要的文件
COPY --from=build /app/publish /app

# 3. 清理包管理器缓存
RUN apk --no-cache add curl

# 4. 压缩镜像层
docker build --squash -t game-server:latest .
```

---

## 五、Docker Compose 编排

### 游戏服务器多服务编排

```yaml
# docker-compose.yml
version: '3.8'

services:
  # MySQL 数据库
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: root123
      MYSQL_DATABASE: game_db
    volumes:
      - mysql-data:/var/lib/mysql
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
    ports:
      - "3306:3306"
    networks:
      - game-net

  # Redis 缓存
  redis:
    image: redis:7-alpine
    volumes:
      - redis-data:/data
    ports:
      - "6379:6379"
    networks:
      - game-net

  # 游戏服务器
  game-server:
    build: .
    image: game-server:latest
    ports:
      - "8888:8888"
    environment:
      - DB_HOST=mysql
      - DB_PORT=3306
      - DB_USER=root
      - DB_PASSWORD=root123
      - REDIS_HOST=redis
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - mysql
      - redis
    restart: unless-stopped
    networks:
      - game-net

  # GM 管理后台
  admin:
    build: ./admin
    ports:
      - "5000:5000"
    depends_on:
      - mysql
    networks:
      - game-net

  # Prometheus 监控
  prometheus:
    image: prom/prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"
    networks:
      - game-net

  # Grafana 可视化
  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
    depends_on:
      - prometheus
    networks:
      - game-net

volumes:
  mysql-data:
  redis-data:

networks:
  game-net:
    driver: bridge
```

### 常用命令

```bash
# 启动所有服务
docker-compose up -d

# 查看日志
docker-compose logs -f game-server

# 重启某个服务
docker-compose restart game-server

# 扩缩容（游戏服务器 3 个实例）
docker-compose up -d --scale game-server=3

# 停止并清理
docker-compose down -v  # -v 清理 volumes
```

---

## 六、Kubernetes 基础

### 为什么可能需要 K8s

```yaml
# Docker Compose 适合单机开发/测试
# K8s 适合生产环境的集群管理

# K8s 核心能力：
# - 自动调度：容器分布到多个节点
# - 自动伸缩：根据 CPU/内存/自定义指标
# - 服务发现：内部 DNS 解析
# - 滚动更新：零停机更新
# - 自愈：容器挂了自动重启
```

### 游戏服务器 Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: game-server
spec:
  replicas: 3
  selector:
    matchLabels:
      app: game-server
  template:
    metadata:
      labels:
        app: game-server
    spec:
      containers:
      - name: server
        image: registry.example.com/game-server:1.0
        ports:
        - containerPort: 8888
        env:
        - name: DB_HOST
          value: mysql-service
        - name: POD_NAME
          valueFrom:
            fieldRef:
              fieldPath: metadata.name
        resources:
          requests:
            cpu: "2"
            memory: "4Gi"
          limits:
            cpu: "4"
            memory: "8Gi"
        livenessProbe:
          httpGet:
            path: /health
            port: 8888
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          tcpSocket:
            port: 8888
          initialDelaySeconds: 5
          periodSeconds: 10
```

### Service (负载均衡)

```yaml
apiVersion: v1
kind: Service
metadata:
  name: game-server-service
spec:
  type: ClusterIP  # 内部访问
  selector:
    app: game-server
  ports:
  - port: 8888
    targetPort: 8888
---
# 对外暴露
apiVersion: v1
kind: Service
metadata:
  name: game-server-external
spec:
  type: LoadBalancer  # 云厂商 LB
  selector:
    app: game-server
  ports:
  - port: 8888
    targetPort: 8888
```

---

## 七、CI/CD 流程

### GitHub Actions 自动构建并推送 Docker 镜像

```yaml
# .github/workflows/docker-build.yml
name: Build and Push Docker Image

on:
  push:
    branches: [main]
    tags: ['v*']

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4

    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v3

    - name: Login to Docker Registry
      uses: docker/login-action@v3
      with:
        registry: registry.example.com
        username: ${{ secrets.REGISTRY_USER }}
        password: ${{ secrets.REGISTRY_PASSWORD }}

    - name: Extract version
      id: version
      run: echo "version=${GITHUB_REF#refs/tags/v}" >> $GITHUB_OUTPUT

    - name: Build and Push
      uses: docker/build-push-action@v5
      with:
        context: .
        push: true
        tags: |
          registry.example.com/game-server:latest
          registry.example.com/game-server:${{ steps.version.outputs.version }}

    - name: Deploy to Server
      run: |
        ssh root@game-server "cd /opt/deploy && docker-compose pull && docker-compose up -d"
```

---

## 八、对比 Windows 部署

| 概念 | Docker (Linux) | Windows Server |
|------|---------------|---------------|
| 容器化 | Docker, Podman | Docker Desktop, Windows Containers |
| 编排 | K8s, Compose | K8s (Windows Node) |
| 镜像系统 | 分层文件系统 (UnionFS) | Docker 分层 + NTFS |
| 私有仓库 | Harbor, Registry | Docker Registry |
| 构建 | Dockerfile | Dockerfile (可能不同 base) |
| 服务发现 | K8s Service | Consul, Nacos |
| 监控 | Prometheus + Grafana | Prometheus, Zabbix |
| 日志 | ELK, Loki | ELK, Graylog |
| CI/CD | GitHub Actions, GitLab CI | Azure DevOps |

---

## 九、练习

1. **Dockerfile 编写**：为假想的游戏服务器写一个多阶段构建 Dockerfile
2. **Docker Compose**：用 Compose 启动 MySQL + Redis + 游戏服务器
3. **镜像优化**：构建一个镜像，用 `docker history` 分析各层大小，尝试减到最小
4. **K8s 部署**：在 minikube 上部署游戏服务器 Deployment + Service
5. **CI/CD**：配置 GitHub Actions 自动化构建和部署

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| Dockerfile | 描述镜像构建步骤的脚本 |
| 多阶段构建 | 构建镜像大，运行镜像小 |
| .dockerignore | 排除不必要的文件进镜像 |
| Docker Compose | 单机多容器编排 |
| K8s | 生产级容器编排平台 |
| livenessProbe | K8s 存活检查，挂了自动重启 |
| CI/CD | 自动构建+测试+部署流水线 |
