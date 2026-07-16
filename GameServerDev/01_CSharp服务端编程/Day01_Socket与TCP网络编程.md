# Day 01：Socket 与 TCP 网络编程

## 一、TCP 协议基础

### 协议分层
TCP/IP 四层模型：应用层 → 传输层 → 网络层 → 网络接口层

TCP 位于**传输层**，提供**面向连接**、**可靠**、**基于字节流**的通信。

### TCP 三次握手与四次挥手
```
三次握手：
  Client → SYN        → Server
  Client ← SYN+ACK    ← Server
  Client → ACK        → Server

四次挥手：
  Client → FIN        → Server
  Client ← ACK        ← Server
  Client ← FIN        ← Server
  Client → ACK        → Server
```

### TCP 为什么可靠
- 确认应答 (ACK)
- 超时重传 (RTO)
- 流量控制 (滑动窗口)
- 拥塞控制 (慢启动/拥塞避免/快重传/快恢复)
- 数据校验 (校验和)
- 数据排序 (序列号)

---

## 二、Socket 是什么

Socket 是操作系统提供的网络编程接口，对 TCP/IP 协议的封装。在 C# 中对应 `System.Net.Sockets.Socket` 类。

### Socket 的工作模式

| 模式 | 方法 | 特点 | 适用场景 |
|------|------|------|---------|
| 同步阻塞 | Socket.Send/Receive | 简单直观，线程阻塞 | 小型工具，学习 |
| 异步 APM | Socket.BeginSend/EndSend | 回调机制，手动管理 | .NET Framework 遗留代码 |
| 异步事件 | SocketAsyncEventArgs | 高 IOCP 利用，零分配 | 高性能服务器 (推荐) |
| async/await | Socket.SendAsync (Task) | 语法简洁，开销略高 | 中等并发 |

---

## 三、同步阻塞服务器

### 基础 TCP 服务器

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;

class TcpServer
{
    private Socket _listenSocket;
    private List<Socket> _clients = new List<Socket>();

    public void Start(int port)
    {
        // 1. 创建 Socket
        _listenSocket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

        // 2. 绑定地址和端口
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, port);
        _listenSocket.Bind(endpoint);

        // 3. 开始监听（最大 pending 连接数）
        _listenSocket.Listen(10);

        Console.WriteLine($"服务器启动，监听端口 {port}");

        // 4. 循环接受客户端
        while (true)
        {
            Socket client = _listenSocket.Accept();
            Console.WriteLine($"客户端连接: {client.RemoteEndPoint}");
            _clients.Add(client);
            ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
        }
    }

    private void HandleClient(Socket client)
    {
        byte[] buffer = new byte[1024];
        try
        {
            while (true)
            {
                int bytesRead = client.Receive(buffer);
                if (bytesRead == 0) break; // 客户端断开

                string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"收到: {msg}");

                // 回显
                byte[] response = Encoding.UTF8.GetBytes($"服务器收到: {msg}");
                client.Send(response);
            }
        }
        catch (SocketException)
        {
            Console.WriteLine("客户端异常断开");
        }
        finally
        {
            client.Close();
            _clients.Remove(client);
        }
    }
}
```

### 基础 TCP 客户端

```csharp
using System.Net.Sockets;
using System.Text;

class TcpClient
{
    private Socket _socket;

    public void Connect(string ip, int port)
    {
        _socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

        _socket.Connect(ip, port);
        Console.WriteLine("连接成功");

        // 发送
        byte[] data = Encoding.UTF8.GetBytes("Hello Server!");
        _socket.Send(data);

        // 接收
        byte[] buffer = new byte[1024];
        int len = _socket.Receive(buffer);
        Console.WriteLine($"服务器回复: {Encoding.UTF8.GetString(buffer, 0, len)}");

        _socket.Close();
    }
}
```

### 同步模型的痛点
1. **Accept 阻塞** — 主线程卡在 Accept，无法做其他事
2. **Receive 阻塞** — 每个客户端一个线程，C10K 时线程爆炸
3. **线程开销大** — 线程切换成本 + 内存占用（默认 1MB 栈）

---

## 四、SocketAsyncEventArgs 高性能模型

这是 Windows IOCP (I/O Completion Ports) 在 .NET 中的封装，**推荐的高性能方案**。

### 核心机制
- 预分配 `SocketAsyncEventArgs` 对象池
- 通过**回调事件**通知完成，无需线程阻塞
- 利用 IOCP 内核对象实现真正的**异步 IO**
- 对象可复用，减少 GC 压力

### 高性能服务器框架

```csharp
using System.Net;
using System.Net.Sockets;

class AsyncTcpServer
{
    private Socket _listenSocket;
    private int _bufferSize = 1024;
    private int _maxClients = 10000;

    // 连接池（预分配）
    private BufferManager _bufferManager;
    private SocketAsyncEventArgsPool _eventArgsPool;
    private Dictionary<Socket, ClientState> _clients = new();

    public void Start(int port)
    {
        _bufferManager = new BufferManager(_maxClients * _bufferSize, _bufferSize);
        _eventArgsPool = new SocketAsyncEventArgsPool(_maxClients);

        // 预分配 accept 用的 EventArgs
        SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
        acceptArgs.Completed += OnAcceptCompleted;

        _listenSocket = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        _listenSocket.Listen(100);
        _listenSocket.AcceptAsync(acceptArgs);
    }

    private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            Socket client = e.AcceptSocket;
            Console.WriteLine($"客户端连接: {client.RemoteEndPoint}");

            // 准备接收数据
            SocketAsyncEventArgs receiveArgs = _eventArgsPool.Pop();
            if (receiveArgs != null)
            {
                _bufferManager.SetBuffer(receiveArgs); // 分配缓冲区
                receiveArgs.AcceptSocket = client;
                receiveArgs.Completed += OnIOCompleted;
                _clients[client] = new ClientState(receiveArgs);

                if (!client.ReceiveAsync(receiveArgs))
                    ProcessReceived(receiveArgs);
            }
        }

        // 继续 accept 下一个
        e.AcceptSocket = null;
        _listenSocket.AcceptAsync(e);
    }

    private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
    {
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Receive:
                ProcessReceived(e);
                break;
            case SocketAsyncOperation.Send:
                ProcessSent(e);
                break;
        }
    }

    private void ProcessReceived(SocketAsyncEventArgs e)
    {
        if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
        {
            // 处理收到的数据
            byte[] data = new byte[e.BytesTransferred];
            Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);
            // ... 解析消息 ...

            // 继续接收
            if (!e.AcceptSocket.ReceiveAsync(e))
                ProcessReceived(e);
        }
        else
        {
            // 客户端断开
            CloseClient(e);
        }
    }

    private void ProcessSent(SocketAsyncEventArgs e)
    {
        // 发送完成，复用 EventArgs
        e.AcceptSocket.ReceiveAsync(e);
    }

    private void CloseClient(SocketAsyncEventArgs e)
    {
        e.AcceptSocket.Close();
        _clients.Remove(e.AcceptSocket);
        _eventArgsPool.Push(e);
    }
}

// 缓冲管理器（预先分配一大块连续内存）
class BufferManager
{
    private byte[] _bufferBlock;
    private Stack<int> _freeIndexes;
    private int _bufferSize;

    public BufferManager(int totalBytes, int bufferSize)
    {
        _bufferBlock = new byte[totalBytes];
        _freeIndexes = new Stack<int>();
        _bufferSize = bufferSize;

        for (int i = 0; i < totalBytes; i += bufferSize)
            _freeIndexes.Push(i);
    }

    public bool SetBuffer(SocketAsyncEventArgs args)
    {
        if (_freeIndexes.Count > 0)
        {
            args.SetBuffer(_bufferBlock, _freeIndexes.Pop(), _bufferSize);
            return true;
        }
        return false;
    }

    public void FreeBuffer(SocketAsyncEventArgs args)
    {
        _freeIndexes.Push(args.Offset);
        args.SetBuffer(null, 0, 0);
    }
}

// SAEA 对象池
class SocketAsyncEventArgsPool
{
    private Stack<SocketAsyncEventArgs> _pool;

    public SocketAsyncEventArgsPool(int capacity)
    {
        _pool = new Stack<SocketAsyncEventArgs>(capacity);
    }

    public void Push(SocketAsyncEventArgs item)
    {
        if (item == null) throw new ArgumentNullException();
        lock (_pool) { _pool.Push(item); }
    }

    public SocketAsyncEventArgs Pop()
    {
        lock (_pool)
        {
            return _pool.Count > 0 ? _pool.Pop() : null;
        }
    }
}

class ClientState
{
    public SocketAsyncEventArgs ReceiveArgs { get; set; }
    public byte[] DataBuffer { get; set; }

    public ClientState(SocketAsyncEventArgs args)
    {
        ReceiveArgs = args;
        DataBuffer = new byte[4096];
    }
}
```

### SocketAsyncEventArgs 为什么快
1. **对象池复用** — 避免每次 IO 分配新对象
2. **预分配缓冲区** — 连续内存块，减少碎片
3. **IOCP 内核支持** — 完成端口模型，线程数 = CPU 核数
4. **减少用户态→内核态切换**

---

## 五、网络字节序

不同 CPU 架构的字节序不同（x86 小端，网络协议大端），通过网络时必须统一。

```csharp
// 主机字节序 → 网络字节序
short networkOrder = IPAddress.HostToNetworkOrder(hostOrder);
// 网络字节序 → 主机字节序
short hostOrder = IPAddress.NetworkToHostOrder(networkOrder);

// 手动处理
int bigEndian = (int)IPAddress.HostToNetworkOrder(littleEndian);
```

---

## 六、TCP Keep-Alive 与心跳

### TCP Keep-Alive (系统层)

```csharp
// 开启 Keep-Alive
Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
// 设置参数需要 IOCTL
uint keepAliveTime = 30000; // 30s 无数据开始探测
uint keepAliveInterval = 5000; // 每次探测间隔 5s
byte[] inValue = new byte[12];
BitConverter.GetBytes(keepAliveTime).CopyTo(inValue, 0);
BitConverter.GetBytes(keepAliveInterval).CopyTo(inValue, 4);
BitConverter.GetBytes(1).CopyTo(inValue, 8);
socket.IOControl(IOControlCode.KeepAliveValues, inValue, null);
```

### 应用层心跳 (推荐)

```csharp
class HeartbeatManager
{
    private Dictionary<Socket, DateTime> _lastHeartbeat = new();
    private int _timeout = 30000; // 30s 超时

    public void OnReceiveHeartbeat(Socket client)
    {
        lock (_lastHeartbeat)
            _lastHeartbeat[client] = DateTime.Now;
    }

    public void CheckTimeouts()
    {
        DateTime now = DateTime.Now;
        List<Socket> timeouts = new();

        lock (_lastHeartbeat)
        {
            foreach (var kv in _lastHeartbeat)
            {
                if ((now - kv.Value).TotalMilliseconds > _timeout)
                    timeouts.Add(kv.Key);
            }
        }

        foreach (var client in timeouts)
        {
            Console.WriteLine($"心跳超时，断开: {client.RemoteEndPoint}");
            client.Close();
            lock (_lastHeartbeat)
                _lastHeartbeat.Remove(client);
        }
    }
}
```

### 心跳 vs Keep-Alive

| | Keep-Alive | 应用层心跳 |
|--|-----------|-----------|
| 层级 | TCP 协议层 | 应用层 |
| 可控性 | 低（依赖 OS 参数） | 高（完全控制） |
| 检测速度 | 慢 (2h 默认) | 快 (可自定义) |
| 协议无关 | 是 | 否（需协议支持） |
| 建议 | 作为保底 | 主要检测手段 |

---

## 七、粘包与拆包

TCP 是**流式协议**，不维护消息边界。可能发生：
- **粘包**：多个小消息合并
- **拆包**：一个大消息被拆分

### 解决方案：消息头 + 消息体

```
[消息ID 2字节][消息长度 4字节][消息体 N字节]
```

```csharp
class PacketCodec
{
    public byte[] Encode(ushort msgId, byte[] body)
    {
        int length = body.Length;
        byte[] packet = new byte[2 + 4 + length];

        // 消息 ID (2字节)
        BitConverter.GetBytes(msgId).CopyTo(packet, 0);
        // 消息体长度 (4字节)
        BitConverter.GetBytes(length).CopyTo(packet, 2);
        // 消息体
        body.CopyTo(packet, 6);

        return packet;
    }

    public List<(ushort msgId, byte[] body)> Decode(byte[] data, ref int offset)
    {
        List<(ushort, byte[])> messages = new();

        while (offset + 6 <= data.Length) // 至少 2+4 字节头
        {
            ushort msgId = BitConverter.ToUInt16(data, offset);
            int length = BitConverter.ToInt32(data, offset + 2);

            if (offset + 6 + length > data.Length)
                break; // 数据不够，等待下次

            byte[] body = new byte[length];
            Array.Copy(data, offset + 6, body, 0, length);

            messages.Add((msgId, body));
            offset += 6 + length;
        }

        return messages;
    }
}

// 接收缓冲区
class ReceiveBuffer
{
    private byte[] _buffer = new byte[4096];
    private int _offset = 0;

    public void Append(byte[] data, int length)
    {
        if (_offset + length > _buffer.Length)
            Array.Resize(ref _buffer, _buffer.Length * 2);
        Array.Copy(data, 0, _buffer, _offset, length);
        _offset += length;
    }

    public bool TryDecode(out ushort msgId, out byte[] body)
    {
        msgId = 0;
        body = null;

        if (_offset < 6) return false;

        int length = BitConverter.ToInt32(_buffer, 2);
        if (_offset < 6 + length) return false;

        msgId = BitConverter.ToUInt16(_buffer, 0);
        body = new byte[length];
        Array.Copy(_buffer, 6, body, 0, length);
        _offset -= 6 + length;
        Array.Copy(_buffer, 6 + length, _buffer, 0, _offset);

        return true;
    }
}
```

---

## 八、对比 C++ Winsock

```cpp
// C++ Winsock TCP 服务器 (同步)
WSADATA wsaData;
WSAStartup(MAKEWORD(2, 2), &wsaData);

SOCKET listenSock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
sockaddr_in addr = {};
addr.sin_family = AF_INET;
addr.sin_port = htons(8888);
addr.sin_addr.s_addr = INADDR_ANY;
bind(listenSock, (sockaddr*)&addr, sizeof(addr));
listen(listenSock, SOMAXCONN);

while (true) {
    SOCKET client = accept(listenSock, NULL, NULL);
    // 创建线程处理
    CreateThread(NULL, 0, ClientThread, (LPVOID)client, 0, NULL);
}

// IOCP 模型 (对应 C# SocketAsyncEventArgs)
HANDLE iocp = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
CreateIoCompletionPort((HANDLE)client, iocp, (ULONG_PTR)client, 0);
// 然后 GetQueuedCompletionStatus 等待通知
```

### 对照表

| C# | C++ (Windows) | 概念 |
|---|---------------|------|
| `Socket` | `SOCKET` | 套接字句柄 |
| `SocketAsyncEventArgs` | `OVERLAPPED + PER_IO_CONTEXT` | 异步 IO 上下文 |
| `Socket.Bind/Listen` | `bind/listen` | 绑定监听 |
| `AcceptAsync` | `AcceptEx` | 异步接受 |
| `SocketAsyncEventArgsPool` | 对象池/内存池 | 复用避免分配 |
| `BufferManager` | 预分配连续内存 | 零碎分配优化 |

---

## 九、练习

1. **基础 Echo 服务器**：实现同步 TCP Echo 服务器 + 客户端
2. **改进为异步**：将 Echo 服务器改为 SocketAsyncEventArgs 版本
3. **消息边界处理**：给以上服务器加上消息头的粘包处理
4. **连接管理器**：实现管理 10000 连接的简单框架，含自动断开超时连接
5. **Wireshark 抓包**：抓取三次握手/四次挥手/数据包，观察序列号和确认号

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| TCP | 面向连接、可靠、字节流 |
| Socket | 操作系统对网络协议的编程接口 |
| 同步模型 | 简单但线程爆炸，不适合高并发 |
| SocketAsyncEventArgs | IOCP 封装，高性能游戏服务器首选 |
| 粘包 | TCP 流式无边界，需要自定义协议分隔 |
| 心跳 | 应用层主动检测连接是否存活 |
| 网络字节序 | 大端序，不同平台通信必须转换 |
