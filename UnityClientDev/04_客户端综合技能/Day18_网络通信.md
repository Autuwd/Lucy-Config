# Day 18：网络通信 — 从 Socket 到 UnityWebRequest

## 0. 为什么需要网络通信？

单机游戏是"一个人玩自己的数据"，网络游戏是"一群人共享同一份数据"。

```
单机游戏：
你的操作 → 修改本地数据 → 渲染到你的屏幕

网络游戏：
你的操作 → 发送到服务器 → 服务器广播给所有人 → 所有客户端更新
```

在 Raylib 中你可能没接触过网络编程，但客户端开发中网络是核心技能——你写的不是"游戏逻辑"，而是"客户端与服务器交互的逻辑"。

网络编程的核心问题：
1. **连接**：怎么找到服务器？
2. **协议**：用什么规则交流？
3. **序列化**：对象怎么变成字节流？
4. **延迟**：怎么让玩家感觉不到网络延迟？

---

## 1. 网络分层模型

### OSI 七层 vs TCP/IP 四层

游戏开发最常用的是**传输层**（TCP/UDP）：

```
应用层         ← 你的游戏协议（自定义消息）
传输层         ← TCP（可靠）/ UDP（不可靠）
网络层         ← IP 寻址（你的数据怎么找到服务器）
链路层         ← 物理传输（网线、WiFi）
```

**游戏客户端程序员关注的是：**
- 应用层：如何定义消息格式（协议设计）
- 传输层：用什么协议（TCP vs UDP）

### TCP/UDP 在操作系统层的实现

```
应用进程（你的游戏）
    │  socket(AF_INET, SOCK_STREAM, 0)  ← TCP
    │  socket(AF_INET, SOCK_DGRAM, 0)   ← UDP
    ▼
操作系统内核
    │  TCP: 维护发送/接收缓冲区、序列号、重传定时器
    │  UDP: 只有接收缓冲区，不确认不重传
    ▼
网卡 → 物理网络
```

**TCP 的可靠性是怎么实现的？**

```
发送方： seq=1, data="Hello"
         ↓ 启动定时器（RTO）
接收方： 收到 seq=1，回复 ACK=2
发送方： 收到 ACK，继续发 seq=2
如果超时没收 ACK：重传 seq=1

这就是"自动重传请求"（ARQ）——TCP 可靠性的基础
```

---

## 2. TCP vs UDP — 核心区别

```csharp
// TCP（面向连接，可靠）：
// 像打电话——先建立连接，按顺序通话，对方一定能听到
// 优点：数据不丢、不乱序
// 缺点：有延迟、重传机制可能导致"队头阻塞"

// UDP（无连接，不可靠）：
// 像对讲机——直接喊话，对方可能听不到
// 优点：低延迟、没有重传阻塞
// 缺点：可能丢包、乱序
```

### 游戏中的选择策略

| 数据类型 | 协议 | 原因 |
|---------|------|------|
| 登录/注册 | TCP | 必须可靠，丢了就登不上 |
| 聊天消息 | TCP | 不能丢，丢了玩家投诉 |
| 玩家位置 | UDP | 丢了就丢了，下一帧又发来了 |
| 射击判定 | UDP | 延迟比可靠更重要 |
| 道具拾取 | TCP | 必须确认拾取成功 |

### UDP Socket 示例（了解原理）

实时游戏（MOBA、FPS）的核心是 UDP，因为**位置数据不需要可靠**——旧的丢了，新的又来了。

```csharp
using System.Net.Sockets;
using System.Net;
using System.Text;

public class UdpClientExample
{
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;

    public void Connect(string ip, int port)
    {
        udpClient = new UdpClient();
        serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        // UDP 没有真正的"连接"，这里只是记录服务器地址
    }

    public void Send(byte[] data)
    {
        udpClient.Send(data, data.Length, serverEndPoint);
        // UDP Send 不会阻塞，不保证到达
    }

    public void StartReceive()
    {
        udpClient.BeginReceive(OnReceive, null);
    }

    private void OnReceive(IAsyncResult ar)
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        byte[] data = udpClient.EndReceive(ar, ref remoteEP);

        // 处理收到的数据...

        // 继续接收下一包
        udpClient.BeginReceive(OnReceive, null);
    }

    public void Close()
    {
        udpClient?.Close();
    }
}

// UDP 对比 TCP 在代码上的区别：
// TCP：        await stream.WriteAsync(data, 0, data.Length);  // 保证完整
// UDP：        udpClient.Send(data, data.Length, endPoint);    // 发完不管
// TCP 读取：   要循环直到收够指定字节数
// UDP 读取：   一次 Receive 就是一整条消息（不粘包）
```

### UDP 需要自己解决什么？

```
TCP 自动做的，UDP 全不管：
1. 丢包 → 自己检测 + 重传（如果需要的话）
2. 乱序 → 自己加序列号排序
3. 粘包 → UDP 不粘包（消息边界天然保留）

所以很多 FPS 游戏的做法是：
- 位置/旋转：纯 UDP，丢了不管
- 重要事件（拾取、伤害）：UDP + 自定义 ACK
```

---

## 3. UnityWebRequest — HTTP 请求

Unity 提供的 HTTP 客户端，用于 REST API、下载资源等：

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class APITest : MonoBehaviour
{
    IEnumerator Start()
    {
        // GET 请求
        using UnityWebRequest req = UnityWebRequest.Get("https://api.example.com/players/1");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = req.downloadHandler.text;
            Debug.Log($"收到数据：{json}");
        }
        else
        {
            Debug.LogError($"请求失败：{req.error}");
        }
    }
}
```

### POST 请求（发送 JSON）

```csharp
IEnumerator PostScore(int score)
{
    // 构造 JSON 数据
    string jsonData = JsonUtility.ToJson(new ScoreData { playerId = 1, score = score });

    using UnityWebRequest req = new UnityWebRequest("https://api.example.com/scores", "POST");
    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
    req.uploadHandler = new UploadHandlerRaw(bodyRaw);
    req.downloadHandler = new DownloadHandlerBuffer();
    req.SetRequestHeader("Content-Type", "application/json");

    yield return req.SendWebRequest();

    if (req.result == UnityWebRequest.Result.Success)
        Debug.Log("上传成功");
}

[System.Serializable]
public class ScoreData
{
    public int playerId;
    public int score;
}
```

### UnityWebRequest + async/await 模式

```csharp
using System.Threading.Tasks;

public class AsyncWebExample : MonoBehaviour
{
    // Unity 2020+ 支持 UnityWebRequest 的 async/await
    public async Task<string> FetchPlayerNameAsync(int playerId)
    {
        using UnityWebRequest req = UnityWebRequest.Get($"https://api.example.com/players/{playerId}");

        // SendWebRequest() 返回 UnityWebRequestAsyncOperation
        // 可以用 await 替代 yield return
        var operation = req.SendWebRequest();

        // 方法 1：用 CompletionSource 包装
        var tcs = new TaskCompletionSource<bool>();
        operation.completed += _ => tcs.SetResult(true);
        await tcs.Task;

        // 方法 2（推荐）：用 Unity 的 AsyncOperation 扩展
        await operation;  // 需要安装 UnityAsync 或 UniTask

        if (req.result == UnityWebRequest.Result.Success)
            return req.downloadHandler.text;

        throw new System.Exception(req.error);
    }
}
```

### 下载资源

```csharp
public IEnumerator DownloadAssetBundle(string url)
{
    using UnityWebRequest req = UnityWebRequestAssetBundle.GetAssetBundle(url);
    yield return req.SendWebRequest();

    if (req.result == UnityWebRequest.Result.Success)
    {
        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(req);
        GameObject prefab = bundle.LoadAsset<GameObject>("MyPrefab");
        Instantiate(prefab);
        bundle.Unload(false);
    }
}
```

### 超时控制

```csharp
// UnityWebRequest 默认没有超时，需要手动处理
IEnumerator RequestWithTimeout(string url, float timeout)
{
    using UnityWebRequest req = UnityWebRequest.Get(url);
    req.timeout = (int)timeout;  // UnityWebRequest 内置 timeout，秒为单位

    float startTime = Time.time;
    var op = req.SendWebRequest();

    while (!op.isDone)
    {
        if (Time.time - startTime > timeout)
        {
            req.Abort();
            Debug.LogError("请求超时");
            yield break;
        }
        yield return null;
    }

    if (req.result == UnityWebRequest.Result.Success)
        Debug.Log(req.downloadHandler.text);
}
```

---

## 4. C# Socket 底层编程（了解原理）

UnityWebRequest 是高级封装，底层是 Socket。理解 Socket 才能做实时游戏（MOBA、FPS）。

### TCP Socket 客户端

```csharp
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

public class TcpClientExample
{
    private TcpClient client;
    private NetworkStream stream;

    public async Task Connect(string ip, int port)
    {
        client = new TcpClient();
        await client.ConnectAsync(IPAddress.Parse(ip), port);
        stream = client.GetStream();
        Debug.Log("连接成功");
    }

    public async Task Send(byte[] data)
    {
        // 前 4 字节 = 消息长度（解决粘包）
        byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
        await stream.WriteAsync(lengthPrefix, 0, 4);
        await stream.WriteAsync(data, 0, data.Length);
    }

    public async Task<byte[]> Receive()
    {
        // 先读 4 字节长度
        byte[] lenBuffer = new byte[4];
        int read = 0;
        while (read < 4)
            read += await stream.ReadAsync(lenBuffer, read, 4 - read);

        int length = BitConverter.ToInt32(lenBuffer, 0);

        // 再读实际数据
        byte[] data = new byte[length];
        int received = 0;
        while (received < length)
            received += await stream.ReadAsync(data, received, length - received);

        return data;
    }

    public void Close()
    {
        stream?.Close();
        client?.Close();
    }
}
```

### 粘包问题详解

```
TCP 是流协议——没有消息边界
发送方发了两次：
[0x05][Hello][0x05][World]

在网络传输中可能被合并或拆分：
[0x05][Hello][0x05]       ← 半包
[World]                    ← 和上一个拼起来

解决方案——长度前缀法（实际项目标准做法）：
[消息长度 4字节][消息体 N字节]
[0x00000005][Hello][0x00000005][World]
                  ↑
        接收方：先读 4 字节得到长度 5
               再读 5 字节得到 "Hello"
               再读 4 字节得到长度 5
               再读 5 字节得到 "World"
```

### 断线重连

```csharp
public class ReconnectingClient
{
    private TcpClient client;
    private string ip;
    private int port;
    private bool isRunning;

    public async void Start(string ip, int port)
    {
        this.ip = ip;
        this.port = port;
        isRunning = true;

        while (isRunning)
        {
            try
            {
                await ConnectWithRetry();
                await MessageLoop();
            }
            catch (Exception e)
            {
                Debug.LogError($"连接断开：{e.Message}，5 秒后重连...");
                await Task.Delay(5000);
            }
        }
    }

    private async Task ConnectWithRetry()
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(ip, port);
                stream = client.GetStream();
                Debug.Log("重连成功");
                return;
            }
            catch
            {
                retryCount++;
                int delay = Mathf.Min(retryCount * 2, 30); // 指数退避，最大 30 秒
                Debug.Log($"第 {retryCount} 次重连失败，{delay} 秒后重试...");
                await Task.Delay(delay * 1000);
            }
        }
    }
}
```

---

## 5. 心跳机制

长时间没有数据交换，连接可能被中间路由器或防火墙断开。心跳（Heartbeat）解决这个问题。

```csharp
public class HeartbeatManager
{
    private TcpClientExample client;
    private float sendInterval = 5f;   // 每 5 秒发一次心跳
    private float timeoutLimit = 15f;  // 15 秒没收到心跳回复 = 断线
    private float lastSendTime;
    private float lastReceiveTime;

    public void Update(float deltaTime)
    {
        lastSendTime += deltaTime;

        // 发送心跳
        if (lastSendTime >= sendInterval)
        {
            SendHeartbeat();
            lastSendTime = 0;
        }

        // 检测超时
        if (Time.time - lastReceiveTime > timeoutLimit)
        {
            Debug.LogError("心跳超时，连接断开");
            Reconnect();
        }
    }

    private void SendHeartbeat()
    {
        // 心跳包 = 消息ID + 空内容
        byte[] heartbeatMsg = MessageBuilder.Create(MessageID.C2S_Heartbeat);
        client.Send(heartbeatMsg);
    }

    public void OnHeartbeatResponse()
    {
        lastReceiveTime = Time.time;
    }
}
```

---

## 6. Unity 中处理网络的最佳实践

### 不要在 Update 里发网络请求

```csharp
// ❌ 错误：每帧发请求
void Update()
{
    StartCoroutine(GetData());  // 每秒 60 次请求，服务器直接挂掉
}
```

### 不要在主线程做同步网络请求

```csharp
// ❌ 错误：阻塞主线程
void Start()
{
    client.Receive();  // 卡住直到收到数据，游戏假死
}
```

### 正确方式：回调 + 主线程队列

```csharp
public class NetworkManager : MonoBehaviour
{
    private Queue<System.Action> mainThreadActions = new();
    private TcpClientExample client;
    private object lockObj = new();

    async void Start()
    {
        client = new TcpClientExample();
        await client.Connect("127.0.0.1", 7777);

        // 启动后台接收循环
        _ = ReceiveLoopAsync();
    }

    void Update()
    {
        // 主线程：每帧处理所有待处理消息
        lock (lockObj)
        {
            while (mainThreadActions.Count > 0)
                mainThreadActions.Dequeue()?.Invoke();
        }
    }

    private async Task ReceiveLoopAsync()
    {
        while (true)
        {
            byte[] msg = await client.Receive();

            lock (lockObj)
            {
                mainThreadActions.Enqueue(() =>
                {
                    // 这里可以安全使用 Unity API
                    ProcessMessage(msg);
                });
            }
        }
    }

    private void ProcessMessage(byte[] msg)
    {
        // 解析消息 ID 和体
        ushort msgId = BitConverter.ToUInt16(msg, 0);
        byte[] body = new byte[msg.Length - 2];
        System.Array.Copy(msg, 2, body, 0, body.Length);

        // 派发
        dispatcher.Dispatch(msgId, body);
    }
}
```

---

## 7. 消息协议设计

### 完整协议结构

```
字节偏移：
[0-1]   消息 ID（ushort）
[2-3]   消息体长度（ushort）
[4-...] 消息体（序列化数据）
```

### 消息构建与解析

```csharp
public static class MessageBuilder
{
    // 构建消息
    public static byte[] Create(ushort msgId, byte[] body = null)
    {
        body ??= System.Array.Empty<byte>();
        byte[] packet = new byte[2 + 2 + body.Length];

        // 消息 ID
        BitConverter.GetBytes(msgId).CopyTo(packet, 0);
        // 消息体长度
        BitConverter.GetBytes((ushort)body.Length).CopyTo(packet, 2);
        // 消息体
        body.CopyTo(packet, 4);

        return packet;
    }

    // 解析消息（从完整包中）
    public static (ushort id, byte[] body) Parse(byte[] packet)
    {
        ushort msgId = BitConverter.ToUInt16(packet, 0);
        ushort bodyLength = BitConverter.ToUInt16(packet, 2);
        byte[] body = new byte[bodyLength];
        System.Array.Copy(packet, 4, body, 0, bodyLength);
        return (msgId, body);
    }
}
```

### 协议 ID 枚举与分发器

```csharp
public enum MessageID : ushort
{
    C2S_Login = 1001,
    S2C_LoginResult = 1002,
    C2S_Move = 2001,
    S2C_Position = 2002,
    C2S_Heartbeat = 3001,
    S2C_HeartbeatAck = 3002,
}

public class MessageDispatcher
{
    private Dictionary<ushort, System.Action<byte[]>> handlers = new();

    public void Register(ushort msgId, System.Action<byte[]> handler)
        => handlers[msgId] = handler;

    public void Register<T>(ushort msgId, System.Action<T> handler) where T : class
    {
        handlers[msgId] = (data) =>
        {
            T obj = Deserialize<T>(data);
            handler(obj);
        };
    }

    public void Dispatch(ushort msgId, byte[] data)
    {
        if (handlers.TryGetValue(msgId, out var handler))
            handler(data);
        else
            Debug.LogWarning($"未注册的消息：{msgId}");
    }

    private T Deserialize<T>(byte[] data) where T : class
    {
        // 根据序列化方案实现
        // 可以用 Protobuf、JSON、或自定义二进制
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(
            System.Text.Encoding.UTF8.GetString(data));
    }
}
```

---

## 8. 协议 Buffers 与消息系统整合

### 完整消息管道

```csharp
// 定义 Protobuf 消息
[ProtoContract]
public class LoginRequest
{
    [ProtoMember(1)] public string Username { get; set; }
    [ProtoMember(2)] public string Password { get; set; }
}

[ProtoContract]
public class LoginResponse
{
    [ProtoMember(1)] public int Result { get; set; }
    [ProtoMember(2)] public string Token { get; set; }
}

// 发送消息的完整流程
public class GameClient
{
    private TcpClientExample tcpClient;
    private MessageDispatcher dispatcher = new();

    public async Task Login(string username, string password)
    {
        var request = new LoginRequest { Username = username, Password = password };

        // 1. 序列化
        byte[] body = SerializeProtobuf(request);

        // 2. 构建消息包（消息ID + 长度 + 体）
        byte[] packet = MessageBuilder.Create((ushort)MessageID.C2S_Login, body);

        // 3. 发送
        await tcpClient.Send(packet);
    }

    private byte[] SerializeProtobuf<T>(T obj)
    {
        using var ms = new System.IO.MemoryStream();
        ProtoBuf.Serializer.Serialize(ms, obj);
        return ms.ToArray();
    }

    // 接收处理
    private void OnReceiveLoginResult(byte[] data)
    {
        var response = DeserializeProtobuf<LoginResponse>(data);
        if (response.Result == 0)
            Debug.Log($"登录成功，Token={response.Token}");
    }

    private T DeserializeProtobuf<T>(byte[] data)
    {
        using var ms = new System.IO.MemoryStream(data);
        return ProtoBuf.Serializer.Deserialize<T>(ms);
    }
}
```

### 消息管道流程图

```
发送端：
C# 对象 → Protobuf 序列化 → 加消息ID → 加长度前缀 → Socket 发送

接收端：
Socket 接收 → 读长度前缀 → 读消息体 → 解析消息ID → Protobuf 反序列化 → C# 对象 → 分发
```

---

## 9. 练习

### 练习 1：UnityWebRequest 天气查询

```csharp
// 用 UnityWebRequest 调用公开 API（如 http://www.weather.com.cn/data/sk/101010100.html）
// 解析返回的 JSON，在 Console 输出温度
// 要求：处理超时、网络异常
```

### 练习 2：实现一个简易聊天客户端

```csharp
// 用 TCP Socket 实现（服务器可以用 Node.js 或 Python 快速搭一个）
// 功能：
// 1. 连接服务器
// 2. 发送聊天消息
// 3. 接收并显示其他玩家的消息
// 4. 断线重连
// 5. 心跳保活
```

### 练习 3：协议设计

```csharp
// 为以下多人在线游戏功能设计消息协议：
// - 玩家移动（客户端→服务器，每秒约 10 次）
// - 玩家位置同步（服务器→所有客户端，每秒约 20 次）
// - 发射子弹（客户端→服务器）
// - 子弹命中（服务器→所有客户端）
// 
// 要求：
// 1. 写出 MessageID 枚举
// 2. 写出每条消息的数据结构
// 3. 说明用 TCP 还是 UDP，为什么
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| TCP | 可靠但有延迟，适合登录、聊天 |
| UDP | 快但可能丢包，适合位置同步 |
| 粘包 | TCP 流式导致消息混在一起，用长度前缀解决 |
| 心跳 | 定期发空包，检测连接是否存活 |
| 断线重连 | 指数退避重试，防止服务器被打爆 |
| UnityWebRequest | Unity 的 HTTP 封装，支持 async/await |
| 协议 | 消息ID + 长度 + 序列化体，注册表模式分发 |
| Protobuf | 高效的二进制序列化，网络传输首选 |

**对比 Raylib：** Raylib 不涉及网络，但 TCP/UDP 是操作系统层的概念，和引擎无关。C# 的 Socket API 和 C++ 的 Berkeley Socket 本质相同（都是 `socket()` → `connect()` → `send()`/`recv()`）。Unity 的主要贡献是 UnityWebRequest 这个高级 HTTP 封装，以及协程模型方便异步处理。
