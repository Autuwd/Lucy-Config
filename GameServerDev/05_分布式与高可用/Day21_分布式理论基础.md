# Day 21：分布式理论基础

## 一、CAP 理论

### 定理

```
C (Consistency)    一致性：所有节点同时看到相同数据
A (Availability)   可用性：任何请求都能得到响应（非错误）
P (Partition Tolerance) 分区容错性：节点间通信中断时仍能工作

定理：分布式系统最多满足 CAP 中的两个
```

### CP vs AP

```
网络分区发生时：
  CP 系统：选择停止服务，等分区恢复再提供数据（ZooKeeper, Etcd）
  AP 系统：选择继续服务，等分区恢复后再同步（DinamoDB, Cassandra）

游戏服务器：通常是 AP
  - 玩家可以继续操作，即使数据暂时不同步
  - 分区间通信恢复后最终一致
```

### 游戏服务器的 CAP 取舍

```csharp
// 不同场景不同的取舍
class GameCapDecisions
{
    // 经济系统（扣钻石）：CP
    // 必须一致，不能出现多扣/少扣
    public async Task<bool> DeductDiamond(long playerId, int amount)
    {
        using var lua = _redis.ScriptEvaluateAsync(
            "if redis.call('HGET', KEYS[1], 'diamond') >= tonumber(ARGV[1]) then " +
            "  redis.call('HINCRBY', KEYS[1], 'diamond', -ARGV[1]); return 1; " +
            "else return 0; end",
            new RedisKey[] { $"player:{playerId}" },
            new RedisValue[] { amount });

        return (int)lua == 1;
    }

    // 好友列表：AP
    // 不要求实时一致，最终一致即可
    public Task AddFriendAsync(long playerId, long friendId)
    {
        // 先写本地，异步同步到其他节点
        _localCache.AddFriend(playerId, friendId);
        _syncQueue.Enqueue(new FriendSyncMsg(playerId, friendId));
        return Task.CompletedTask;
    }
}
```

---

## 二、一致性哈希

### 为什么需要一致性哈希

```
普通哈希：serverId = hash(key) % N
问题：N 变化时（加/减服务器），大部分 key 需要迁移

一致性哈希：将服务器映射到哈希环上，key 顺时针找最近的服务器
优点：加/减服务器只影响相邻节点
```

### 哈希环实现

```csharp
public class ConsistentHash<T>
{
    private readonly SortedDictionary<uint, T> _ring = new();
    private readonly HashAlgorithm _hash;
    private readonly int _virtualNodes; // 虚拟节点数

    public ConsistentHash(int virtualNodes = 100)
    {
        _hash = MD5.Create();
        _virtualNodes = virtualNodes;
    }

    // 添加节点（含虚拟节点）
    public void AddNode(T node, string nodeKey)
    {
        for (int i = 0; i < _virtualNodes; i++)
        {
            string virtualKey = $"{nodeKey}:{i}";
            uint hash = ComputeHash(virtualKey);
            _ring[hash] = node;
        }
    }

    // 移除节点
    public void RemoveNode(T node, string nodeKey)
    {
        for (int i = 0; i < _virtualNodes; i++)
        {
            string virtualKey = $"{nodeKey}:{i}";
            uint hash = ComputeHash(virtualKey);
            _ring.Remove(hash);
        }
    }

    // 获取 key 对应的节点
    public T GetNode(string key)
    {
        if (_ring.Count == 0)
            throw new InvalidOperationException("没有可用节点");

        uint hash = ComputeHash(key);

        // 找到第一个大于等于 hash 的节点
        // 如果到末尾还没找到，取第一个（形成环）
        var first = _ring.First;
        var target = _ring.FirstOrDefault(kv => kv.Key >= hash);

        return target.Equals(default) ? first.Value : target.Value;
    }

    private uint ComputeHash(string key)
    {
        byte[] input = Encoding.UTF8.GetBytes(key);
        byte[] hash = _hash.ComputeHash(input);
        return BitConverter.ToUInt32(hash, 0);
    }

    // 获取所有节点的 key 分布数量
    public Dictionary<T, int> GetDistribution()
    {
        var dist = new Dictionary<T, int>();
        foreach (var node in _ring.Values)
        {
            if (!dist.ContainsKey(node))
                dist[node] = 0;
            dist[node]++;
        }
        return dist;
    }
}

// 使用
class GameServerRouter
{
    private readonly ConsistentHash<string> _ring = new(virtualNodes: 200);

    public GameServerRouter()
    {
        _ring.AddNode("Game-Server-1", "gs1");
        _ring.AddNode("Game-Server-2", "gs2");
        _ring.AddNode("Game-Server-3", "gs3");
    }

    public string RoutePlayer(long playerId)
    {
        return _ring.GetNode(playerId.ToString());
    }

    public void OnServerAdded(string serverId)
    {
        _ring.AddNode(serverId, serverId);
        // 只需要迁移相邻节点的数据
    }

    public void OnServerRemoved(string serverId)
    {
        _ring.RemoveNode(serverId, serverId);
        // 该节点的数据被相邻节点接管
    }
}
```

---

## 三、RPC 框架原理

### RPC 调用流程

```
Client                    Server
  │                         │
  │── 序列化请求 ──────────→│
  │  (method, params)       │  反序列化
  │                         │  查找方法
  │                         │  执行方法
  │←── 序列化响应 ──────────│  序列化结果
  │  反序列化               │
```

### 简单 RPC 实现

```csharp
// 服务接口定义
public interface IPlayerService
{
    Task<PlayerData> GetPlayer(long playerId);
    Task<bool> UpdatePlayer(PlayerData player);
}

// 服务端 RPC 处理
public class RpcServer
{
    private readonly Dictionary<string, Func<byte[], Task<byte[]>>> _handlers = new();
    private readonly IServiceProvider _services;

    public void RegisterService<T>(T service)
    {
        var methods = typeof(T).GetMethods();
        foreach (var method in methods)
        {
            string key = $"{typeof(T).Name}.{method.Name}";
            _handlers[key] = async (paramsBytes) =>
            {
                var args = DeserializeParams(method, paramsBytes);
                var result = method.Invoke(service, args);
                if (result is Task task)
                {
                    await task;
                    result = task.GetType().GetProperty("Result").GetValue(task);
                }
                return SerializeResult(result);
            };
        }
    }

    public async Task<byte[]> HandleRequest(string methodKey, byte[] paramsBytes)
    {
        if (_handlers.TryGetValue(methodKey, out var handler))
        {
            return await handler(paramsBytes);
        }
        throw new RpcException($"方法 {methodKey} 不存在");
    }

    // 网络接收循环
    public async Task Listen(int port)
    {
        using var socket = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
        socket.Listen(100);

        while (true)
        {
            var client = await socket.AcceptAsync();
            _ = HandleConnection(client);
        }
    }
}

// 客户端 RPC 代理
public class RpcClient : IPlayerService
{
    private readonly Socket _socket;
    private readonly uint _seqId;

    public async Task<PlayerData> GetPlayer(long playerId)
    {
        var request = new RpcRequest
        {
            MethodKey = "IPlayerService.GetPlayer",
            Params = new object[] { playerId },
            SeqId = Interlocked.Increment(ref _seqId)
        };

        byte[] responseBytes = await Call(request);
        var response = Deserialize<RpcResponse>(responseBytes);

        return response.Result as PlayerData;
    }

    private async Task<byte[]> Call(RpcRequest request)
    {
        byte[] data = Serialize(request);

        // 发送
        await _socket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);

        // 接收
        byte[] buffer = new byte[4096];
        int len = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

        return buffer[..len];
    }
}
```

### gRPC 实际使用

```csharp
// 游戏服务器间通信直接用 gRPC 框架
// 不需要自己实现 RPC
// .proto 文件定义接口，代码自动生成
```

---

## 四、服务发现

### 为什么需要服务发现

```
传统方式：配置文件写死 IP 和端口
问题：服务扩缩容时 IP 变化，需要手动改配置

服务发现：服务启动时注册自己，消费方通过名字查找
```

### Consul 实现

```csharp
// dotnet add package Consul

class ServiceRegistry
{
    private readonly ConsulClient _consul;

    public ServiceRegistry(string consulAddress = "http://127.0.0.1:8500")
    {
        _consul = new ConsulClient(c =>
        {
            c.Address = new Uri(consulAddress);
        });
    }

    // 注册服务
    public async Task Register(string serviceName, string host, int port, string healthUrl = null)
    {
        var registration = new AgentServiceRegistration
        {
            ID = $"{serviceName}-{host}-{port}",
            Name = serviceName,
            Address = host,
            Port = port,
            Check = new AgentServiceCheck
            {
                HTTP = healthUrl ?? $"http://{host}:{port}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(3),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30)
            },
            Tags = new[] { "game-server" }
        };

        await _consul.Agent.ServiceRegister(registration);
        Log.Information("服务 {ServiceName} 已注册到 Consul", serviceName);
    }

    // 注销服务
    public async Task Deregister(string serviceName, string host, int port)
    {
        string id = $"{serviceName}-{host}-{port}";
        await _consul.Agent.ServiceDeregister(id);
    }

    // 查询服务
    public async Task<List<ServiceInfo>> Discover(string serviceName)
    {
        var response = await _consul.Health.Service(serviceName, true);
        return response.Response.Select(s => new ServiceInfo
        {
            Id = s.Service.ID,
            Host = s.Service.Address,
            Port = s.Service.Port
        }).ToList();
    }

    // 监控服务变化
    public async Task WatchService(string serviceName, Action<List<ServiceInfo>> onChanged)
    {
        var lastIndex = 0UL;

        while (true)
        {
            var response = await _consul.Health.Service(
                serviceName, true,
                new QueryOptions { WaitIndex = lastIndex });

            if (response.LastIndex > lastIndex)
            {
                lastIndex = response.LastIndex;
                var services = response.Response
                    .Where(s => s.Checks.All(c => c.Status == HealthStatus.Passing))
                    .Select(s => new ServiceInfo
                    {
                        Id = s.Service.ID,
                        Host = s.Service.Address,
                        Port = s.Service.Port
                    })
                    .ToList();

                onChanged(services);
            }
        }
    }
}

public class ServiceInfo
{
    public string Id { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public string Address => $"{Host}:{Port}";
}
```

### ZooKeeper / Etcd 对比

| 特性 | Consul | ZooKeeper | Etcd |
|------|--------|-----------|------|
| 语言 | Go | Java | Go |
| 一致性 | Raft | ZAB | Raft |
| 健康检查 | 内置 HTTP/TCP | 手动实现 | 手动实现 |
| K-V 存储 | 有 | 有 | 有 (v3) |
| 服务发现 | 原生支持 | 需二次开发 | v3 支持 |
| 运维难度 | 中 | 高 | 中 |
| 游戏服务器 | 推荐 | 较少用 | 新兴方案 |

---

## 五、练习

1. **CAP 分析**：分析你熟悉的一个游戏功能属于 CP 还是 AP
2. **一致性哈希**：实现带虚拟节点的一致性哈希，验证加/减节点后 key 迁移比例
3. **RPC 调用**：用 gRPC 实现两个游戏服务器间的通信
4. **服务发现**：用 Consul 实现游戏服务器自动注册 + 客户端动态发现
5. **对比报告**：比较 Etcd / Consul / ZooKeeper 在游戏服务器场景的适用性

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| CAP | 分布式只能兼顾一致性和可用性中的两个 |
| 一致性哈希 | 加/减节点只影响少量 key 的分布 |
| 虚拟节点 | 解决真实节点数量少时的不均匀问题 |
| RPC | 远程过程调用，服务器间通信的基础 |
| 服务发现 | 服务启动注册，消费方通过名字查找 |
| Consul | 游戏服务器常用的服务发现+健康检查方案 |
