# Day 10：Docker 与容器化部署 — 进阶深入

## 一、多阶段 Dockerfile 生产优化

### 精细分阶段构建

```dockerfile
# 7 阶段: 依赖 → proto → 编译 → 测试 → 扫描 → 打包 → 运行

# === 阶段 0: 基础工具 ===
FROM ubuntu:22.04 AS base
RUN apt-get update && apt-get install -y \
    ca-certificates curl unzip \
    && rm -rf /var/lib/apt/lists/*

# === 阶段 1: Proto 生成 ===
FROM base AS proto
RUN curl -L -o /tmp/protoc.zip \
    https://github.com/protocolbuffers/protobuf/releases/download/v25.0/protoc-25.0-linux-x86_64.zip \
    && unzip /tmp/protoc.zip -d /usr/local && rm /tmp/protoc.zip
COPY proto/ /build/proto/
RUN protoc --csharp_out=/build/gen --grpc_out=/build/gen \
    --plugin=protoc-gen-grpc=/usr/local/bin/grpc_csharp_plugin \
    /build/proto/game.proto

# === 阶段 2: NuGet 恢复（利用 Docker 层缓存） ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
# 先复制 csproj 只触发一次 restore（csproj 不改则缓存命中）
COPY *.sln .
COPY src/GameServer.Api/*.csproj ./src/GameServer.Api/
COPY src/GameServer.Core/*.csproj ./src/GameServer.Core/
RUN dotnet restore

# === 阶段 3: 编译 ===
FROM restore AS build
WORKDIR /src
COPY . .
COPY --from=proto /build/gen/ ./src/Generated/
RUN dotnet publish -c Release -o /publish --no-restore \
    -p:DebugType=None -p:DebugSymbols=false

# === 阶段 4: 测试 ===
FROM build AS test
RUN dotnet test --no-build -c Release --logger:trx

# === 阶段 5: 运行镜像 ===
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime
RUN apk add --no-cache curl tzdata icu-libs \
    && adduser -D gameuser
WORKDIR /app
COPY --from=build /publish .
USER gameuser
EXPOSE 8888
HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8888/health || exit 1
ENTRYPOINT ["dotnet", "GameServer.Api.dll"]
```

### 缓存策略

```dockerfile
# 层缓存条件: 上一层的缓存 + 本层指令完全相同

# 最佳顺序（从不变到常变）:
# 1. 基础镜像（基本不变）
# 2. 系统包安装（偶尔变）
# 3. csproj 文件 + restore（依赖变动时才变）
# 4. 源码复制 + 编译（每次变）

# BuildKit 缓存挂载（不缓存到镜像层，减小体积）
# RUN --mount=type=cache,target=/root/.nuget/packages \
#     dotnet restore

# .dockerignore 减小构建上下文
# .git/ .vs/ bin/ obj/ *.log tests/ docs/

# 效果: 构建上下文从 500MB 降到 10MB，大幅提升构建速度
```

---

## 二、层（Layer）深入理解

```dockerfile
# 以下 Dockerfile 产生 4 层:
FROM ubuntu:22.04           # 层 1: 基础镜像
RUN apt-get update          # 层 2: 包索引
RUN apt-get install -y curl # 层 3: curl 本身
COPY app /app               # 层 4: 应用代码

# 只改 app 代码 → 层 1-3 缓存命中，只重建层 4 → 秒级
# 改 apt-get install → 层 2-4 都要重建

# 错误写法:
COPY . .                    # 复制全部（每次代码变动）
RUN dotnet restore          # 每次都要重新 restore！

# 正确写法:
COPY *.csproj .             # 只复制项目文件
RUN dotnet restore          # csproj 不变则不重新执行
COPY . .                    # 复制剩余代码
```

### 镜像大小分析

```bash
# 分析镜像各层大小
docker history game-server:latest
# IMAGE          CREATED       CREATED BY                  SIZE
# abc123         2 min ago    CMD ["dotnet" "GameServ…"]  0B
# def456         2 min ago    COPY --from=build /app /    89MB
# ...

# 找出不必要的文件:
# 1. 构建工具（sdk 镜像 1.8GB → runtime 镜像 ~200MB）
# 2. 包管理器缓存（apt/apk cache）
# 3. 测试代码和结果
# 4. .git 和文档

# 多阶段构建优势:
# build 阶段: mcr.microsoft.com/dotnet/sdk:8.0 (1.8GB)
# runtime 阶段: mcr.microsoft.com/dotnet/runtime:8.0-alpine (~100MB)
```

---

## 三、Volume vs Bind 挂载性能

| 挂载方式 | 性能 | 适用场景 |
|---------|------|---------|
| Docker 管理卷 | 原生速度 | 数据库、持久化数据 |
| 绑定挂载 | 原生速度(Linux) | 开发、配置文件 |
| tmpfs | 极快(内存) | 临时缓存 |
| NFS 卷 | 慢(网络IO) | 多节点共享 |

```yaml
services:
  game-server:
    volumes:
      - game-data:/app/data       # 持久化数据（Docker 管理）
      - ./config:/app/config:ro   # 配置文件（绑定挂载，只读）
    tmpfs:
      - /app/cache:size=100M      # 临时缓存（内存）

volumes:
  game-data:                      # 声明 Docker 管理卷
    driver: local

# 建议:
# - 日志: 绑定挂载（主机直接分析日志文件）
# - 数据库: Docker 卷（备份迁移方便）
# - 缓存: tmpfs（重启可丟，性能优先）
```

---

## 四、Docker 网络模式

### Bridge

```yaml
services:
  game-server:
    networks:
      - game-net
  mysql:
    networks:
      - game-net

networks:
  game-net:
    driver: bridge
# 自定义 bridge 提供 DNS 服务发现
# game-server 访问 mysql 直接用服务名
# 默认 bridge 没有 DNS，只能 IP 通信
```

### Host

```yaml
services:
  game-server:
    network_mode: host
# 容器直接用主机网络栈
# 优点: 延迟最低、无 NAT 开销
# 缺点: 端口不能映射、无网络隔离
# 适用: C++ 高性能游戏服务器、UDP 大量小包
```

### Overlay（跨主机）

```yaml
networks:
  game-overlay:
    driver: overlay
    attachable: true
# VXLAN 隧道跨主机通信，对容器透明
# K8s 默认使用 overlay 网络
# 三节点: game-server-1 可直接访问 node B 的 mysql
```

### 性能对比

| 模式 | 延迟 | 吞吐 | 隔离 |
|------|------|------|------|
| host | 最低 | 最高 | 无 |
| bridge | 中 | 中 | 端口隔离 |
| macvlan | 低 | 高 | 独立 MAC |
| overlay | 较高 | 较低 | 跨主机 |

---

## 五、Docker Compose Profiles 与 Health Check

### Profiles

```yaml
services:
  game-server:
    image: game-server:latest
    profiles: ["core"]
  mysql:
    image: mysql:8.0
    profiles: ["core"]
  redis:
    image: redis:7-alpine
    profiles: ["core"]
  prometheus:
    image: prom/prometheus
    profiles: ["monitor"]
  grafana:
    image: grafana/grafana
    profiles: ["monitor"]
  adminer:
    image: adminer
    profiles: ["dev"]
  load-test:
    build: ./load-test
    profiles: ["test"]

# 使用:
# docker compose --profile core up -d           # 核心服务
# docker compose --profile core,monitor up -d   # 核心+监控
# docker compose --profile '!dev' up -d         # 排除 dev
```

### Health Check 高级配置

```yaml
services:
  game-server:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8888/health"]
      interval: 15s       # 15 秒检查一次
      timeout: 5s         # 单次超时
      retries: 3          # 连续 3 次失败判为不健康
      start_period: 30s   # 启动后 30 秒才开始检查

  mysql:
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
      start_period: 60s   # MySQL 启动较慢

  # 依赖健康条件
  game-server:
    depends_on:
      mysql:
        condition: service_healthy
      redis:
        condition: service_healthy
    # 保证 mysql/redis 完全就绪后才启动游戏服务器
```

### C# 健康检查

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("OK"))
    .AddCheck<MemoryHealthCheck>("memory");

var app = builder.Build();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResultStatusCodes =
    {
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds
            })
        });
        await context.Response.WriteAsync(json);
    }
});

public class MemoryHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct)
    {
        var mem = GC.GetTotalMemory(false);
        var limit = 512L * 1024 * 1024;
        if (mem > limit)
            return Task.FromResult(
                HealthCheckResult.Unhealthy($"内存过高: {mem / 1024 / 1024}MB"));
        if (mem > limit * 0.8)
            return Task.FromResult(
                HealthCheckResult.Degraded($"内存偏高: {mem / 1024 / 1024}MB"));
        return Task.FromResult(
            HealthCheckResult.Healthy($"内存正常: {mem / 1024 / 1024}MB"));
    }
}
```

---

## 六、Kubernetes Pod Lifecycle

### Pod 阶段与容器状态

```
Pod phase: Pending → Running → Succeeded / Failed

容器状态:
  Waiting: ContainerCreating, CrashLoopBackOff, ImagePullBackOff
  Running: 正常运行
  Terminated: Completed(正常) / Error(异常)
```

### 生命周期钩子

```yaml
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: game-server
    lifecycle:
      preStop:
        exec:
          command:
            - /bin/sh
            - -c
            - |
              kill -15 $(pidof dotnet)   # 优雅退出
              sleep 30                   # 等最多 30 秒
              exit 0
```

### C# 优雅关闭

```csharp
var builder = WebApplication.CreateBuilder(args);
// 延长关闭超时（默认 5 秒不够）
builder.WebHost.UseShutdownTimeout(TimeSpan.FromSeconds(30));

var app = builder.Build();
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("服务器开始优雅关闭...");
    // 1. 停止接受新连接
    // 2. 通知在线玩家即将维护
    // 3. 保存玩家状态到数据库
    // 4. 等待正在进行的操作完成
    // 5. 关闭所有连接
});
```

---

## 七、K8s Service 类型

### ClusterIP

```yaml
apiVersion: v1
kind: Service
metadata:
  name: game-server-internal
spec:
  type: ClusterIP
  selector:
    app: game-server
  ports:
  - port: 8888
    targetPort: 8888
# 虚拟 IP，集群内部可访问
# 外部不可达（除非 ingress 或 kubectl proxy）
```

### NodePort

```yaml
spec:
  type: NodePort
  ports:
  - port: 8888
    targetPort: 8888
    nodePort: 30001
# 每个节点开放 30001 端口
# 外部通过 NodeIP:30001 访问
# 适合开发测试
```

### LoadBalancer

```yaml
spec:
  type: LoadBalancer
  selector:
    app: game-server
  ports:
  - port: 8888
    targetPort: 8888
# 云厂商自动创建外部负载均衡器
# 分配公网 IP，自动健康检查
```

### Headless Service（gRPC 直连）

```yaml
spec:
  clusterIP: None    # 关键: 不分配虚拟 IP
  selector:
    app: game-server
  ports:
  - port: 8888
# 直接返回所有后端 Pod IP
# 客户端自己做负载均衡
# 适合 gRPC 连接池场景
```

### WebSocket 会话保持

```yaml
spec:
  type: LoadBalancer
  sessionAffinity: ClientIP
  sessionAffinityConfig:
    clientIP:
      timeoutSeconds: 86400  # 24 小时
# 同一玩家 IP 始终发到同一 Pod
# WebSocket 游戏必须，不然连接断
```

---

## 八、CI/CD 完整管道

```yaml
# GitHub Actions: lint → test → build → staging → canary → production
name: Game Server CI/CD

on:
  push:
    branches: [main, develop]
    tags: ['v*']

env:
  REGISTRY: registry.example.com
  IMAGE_NAME: game-server

jobs:
  # ── 阶段 1: 代码分析 ──
  lint:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: dotnet format --verify-no-changes

  # ── 阶段 2: 测试 ──
  test:
    needs: lint
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: 8.0 }
      - run: dotnet test --configuration Release \
            --logger trx --collect:"XPlat Code Coverage"

  # ── 阶段 3: 构建推送 ──
  build:
    needs: test
    if: github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/tags/v')
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ secrets.REGISTRY_USER }}
          password: ${{ secrets.REGISTRY_PASSWORD }}
      - name: Build and Push
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: |
            ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:latest
            ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

  # ── 阶段 4: Staging ──
  deploy-staging:
    needs: build
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    environment: staging
    steps:
      - run: |
          kubectl set image deployment/game-server \
            game-server=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}

  # ── 阶段 5: Canary (10% 流量) ──
  deploy-canary:
    needs: deploy-staging
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    environment: canary
    steps:
      - run: |
          # Istio/Flagger 流量分割
          echo "Canary deploy (10% traffic)..."
          # 观察 15 分钟，自动回滚或全量

  # ── 阶段 6: Production ──
  deploy-production:
    needs: deploy-canary
    if: startsWith(github.ref, 'refs/tags/v')
    runs-on: ubuntu-latest
    environment: production
    steps:
      - run: |
          kubectl rollout restart deployment/game-server
          kubectl rollout status deployment/game-server --timeout=5m
```

### 部署策略对比

| 策略 | 风险 | 速度 | 适用 |
|------|------|------|------|
| Rolling Update | 低 | 慢 | 默认策略 |
| Blue-Green | 最低 | 快（瞬间切换） | 关键服务 |
| Canary | 极低 | 慢（逐步放量） | 新版本验证 |
| Recreate | 高（停机） | 最快 | 测试环境 |

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 多阶段构建 | 构建阶段大镜像 → 运行阶段小镜像 |
| Docker 层缓存 | 不变层放前、变化层放后，加速构建 |
| .dockerignore | 排除 .git/.vs 等减少构建上下文 |
| Volume vs Bind | 卷(持久化) vs 绑挂(开发调试) |
| tmpfs | 内存临时文件，性能极好 |
| Bridge vs Host | Bridge 隔离性好，Host 延迟最低 |
| Compose Profile | 按场景启动不同服务组合 |
| Health Check | 依赖就绪后才启动下游服务 |
| Pod Lifecycle | Pending→Running→Succeeded/Failed |
| PreStop 钩子 | 优雅关闭前执行清理 |
| Headless Service | gRPC 直连 Pod，不做负载均衡 |
| CI/CD 管道 | lint→test→build→staging→canary→production |

