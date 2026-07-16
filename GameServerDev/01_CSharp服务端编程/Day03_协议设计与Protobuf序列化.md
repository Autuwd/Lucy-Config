# Day 03：协议设计与 Protobuf 序列化

## 一、为什么需要协议

两台机器通过网络通信，需要约定数据的组织结构。协议就是**双方约定的数据格式**。

### 游戏协议的发展

```
原始阶段 → 固定结构体（memcpy）
发展阶段 → 文本协议（XML/JSON）
现代阶段 → 二进制协议（Protobuf/FlatBuffers）
专用阶段 → 自定义二进制协议（消息ID+长度+体）
```

### 协议设计三要素

```
1. 消息识别：这是哪个消息？
2. 消息长度：数据从哪里开始和结束？
3. 消息内容：数据如何组织和解析？
```

---

## 二、自定义二进制协议

### 经典协议头格式

```
偏移量  大小    说明
0       2      消息 ID (ushort)
2       4      消息体长度 (int)
6       N      消息体 (byte[])
```

### 完整编解码实现

```csharp
class MessageProtocol
{
    // 编码
    public static byte[] Encode(ushort msgId, byte[] body)
    {
        byte[] header = new byte[6];
        // 消息 ID (大端序)
        header[0] = (byte)(msgId >> 8);
        header[1] = (byte)(msgId);
        // 消息体长度 (大端序)
        int len = body.Length;
        header[2] = (byte)(len >> 24);
        header[3] = (byte)(len >> 16);
        header[4] = (byte)(len >> 8);
        header[5] = (byte)(len);

        byte[] packet = new byte[6 + len];
        Buffer.BlockCopy(header, 0, packet, 0, 6);
        Buffer.BlockCopy(body, 0, packet, 6, len);

        return packet;
    }

    // 解码（流式）
    public static bool TryDecode(byte[] buffer, int offset, int count,
        out ushort msgId, out byte[] body)
    {
        msgId = 0;
        body = null;

        if (count - offset < 6)
            return false; // 头部不足

        msgId = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);

        int length = (buffer[offset + 2] << 24) |
                     (buffer[offset + 3] << 16) |
                     (buffer[offset + 4] << 8) |
                     buffer[offset + 5];

        if (count - offset - 6 < length)
            return false; // 消息体不足

        body = new byte[length];
        Buffer.BlockCopy(buffer, offset + 6, body, 0, length);

        return true;
    }
}

// 使用
class PacketHandler
{
    private byte[] _buffer = new byte[4096];
    private int _offset = 0;

    public void OnReceive(byte[] data, int length)
    {
        // 数据追加到缓冲区
        if (_offset + length > _buffer.Length)
            Array.Resize(ref _buffer, _buffer.Length * 2);
        Buffer.BlockCopy(data, 0, _buffer, _offset, length);
        _offset += length;

        // 尝试解析每一帧
        while (true)
        {
            if (!MessageProtocol.TryDecode(_buffer, 0, _offset,
                    out ushort msgId, out byte[] body))
                break;

            // 处理消息
            DispatchMessage(msgId, body);

            // 移除已解析的数据
            int packetSize = 6 + body.Length;
            _offset -= packetSize;
            if (_offset > 0)
                Buffer.BlockCopy(_buffer, packetSize, _buffer, 0, _offset);
        }
    }
}
```

### 消息 ID 规划

```csharp
// 按功能模块分组
public enum MsgId : ushort
{
    // 基础 (0001-0999)
    Heartbeat         = 0x0001,
    HeartbeatResponse = 0x0002,
    Kick              = 0x0003,

    // 登录 (1000-1999)
    LoginRequest      = 0x1001,
    LoginResponse     = 0x1002,
    LogoutRequest     = 0x1003,
    RegisterRequest   = 0x1004,

    // 玩家 (2000-2999)
    PlayerInfo        = 0x2001,
    PlayerMove        = 0x2002,

    // 战斗 (3000-3999)
    FightRequest      = 0x3001,
    FightResponse     = 0x3002,
    SkillCast         = 0x3003,

    // 社交 (4000-4999)
    ChatMessage       = 0x4001,
    FriendRequest     = 0x4002,
}
```

---

## 三、Protobuf (Google Protocol Buffers)

### 为什么用 Protobuf

| 特性 | JSON | XML | Protobuf |
|------|------|-----|----------|
| 编码后大小 | 大 (~10KB) | 很大 (~30KB) | 小 (~3KB) |
| 编码速度 | 快 | 慢 | 极快 |
| 解码速度 | 快 | 慢 | 极快 |
| 类型安全 | 弱 (字符串数字) | 弱 | 强 (代码生成) |
| 版本兼容 | 弱 | 弱 | 强 (向前向后兼容) |
| 二进制安全 | 否 (需 Base64) | 否 (需 Base64) | 是 |
| 人类可读 | 是 | 是 | 否 |

### .proto 文件定义

```protobuf
syntax = "proto3";

package GameServer;

// 登录请求
message LoginRequest {
  string account = 1;
  string password = 2;
  int32 server_id = 3;
}

// 登录响应
message LoginResponse {
  int32 error_code = 1;
  string token = 2;
  PlayerInfo player = 3;
}

// 玩家信息
message PlayerInfo {
  int64 player_id = 1;
  string name = 2;
  int32 level = 3;
  int64 exp = 4;
  repeated Item items = 5;       // 数组
  map<int32, int32> stats = 6;  // 字典
  oneof equipment {              // 互斥字段
    Weapon weapon = 7;
    Armor armor = 8;
  }
}

message Item {
  int32 item_id = 1;
  int32 count = 2;
  bool equipped = 3;
}

message Weapon {
  int32 attack = 1;
  int32 speed = 2;
}

message Armor {
  int32 defense = 1;
  int32 durability = 2;
}
```

### Protobuf 序列化使用

```csharp
// 安装: dotnet add package Google.Protobuf

using Google.Protobuf;

// 构建消息
var loginReq = new LoginRequest
{
    Account = "player1",
    Password = "123456",
    ServerId = 1
};

// 序列化 (3-5倍于 JSON 的速度)
byte[] data = loginReq.ToByteArray();

// 反序列化
LoginRequest parsed = LoginRequest.Parser.ParseFrom(data);
Console.WriteLine(parsed.Account);

// 带消息协议头
var packet = MessageProtocol.Encode(
    (ushort)MsgId.LoginRequest,
    loginReq.ToByteArray());
```

### Protobuf 编码原理 (Varint)

Protobuf 的核心编码是 **Varint**（可变长整数）。

```
数字 1:   0000 0001   (1字节)
数字 300: 1010 1100  0000 0010   (2字节)
  → 去掉 MSB: 010 1100  000 0010
  → 小端重组: 000 0010  010 1100 = 256 + 44 = 300
```

```csharp
// Varint 编码实现
public static byte[] EncodeVarint(ulong value)
{
    var ms = new MemoryStream();
    do
    {
        byte byteVal = (byte)(value & 0x7F);
        value >>= 7;
        if (value != 0)
            byteVal |= 0x80; // 设置 MSB（继续位）
        ms.WriteByte(byteVal);
    } while (value != 0);
    return ms.ToArray();
}

public static ulong DecodeVarint(byte[] data, out int bytesRead)
{
    bytesRead = 0;
    ulong result = 0;
    int shift = 0;

    do
    {
        if (bytesRead >= data.Length)
            throw new InvalidDataException("数据不足");

        byte byteVal = data[bytesRead++];
        result |= (ulong)(byteVal & 0x7F) << shift;
        shift += 7;

        if ((byteVal & 0x80) == 0)
            break;
    } while (shift < 64);

    return result;
}
```

### Wire Type 编码

```
每个字段编码格式：
  Tag = (field_number << 3) | wire_type

Wire Type 定义：
  0: Varint      (int32/int64/uint32/bool/enum)
  1: 64-bit      (fixed64/double)
  2: Length-delimited (string/bytes/embedded messages/repeated)
  5: 32-bit      (fixed32/float)
```

### 版本兼容原理

```protobuf
// 旧版本
message PlayerInfo {
  string name = 1;
  int32 level = 2;
}

// 新版本（向后兼容）
message PlayerInfo {
  string name = 1;
  int32 level = 2;
  int64 exp = 3;       // 新增字段，旧代码忽略
  string title = 4;    // 新增字段
}

// 删除字段要留 reserved
message PlayerInfo {
  reserved 3;           // 保留字段号，防止复用
  reserved "exp";
  string name = 1;
  int32 level = 2;
}
```

---

## 四、其他序列化方案对比

### JSON (System.Text.Json)

```csharp
using System.Text.Json;

var player = new PlayerData { Name = "Alice", Level = 10 };
string json = JsonSerializer.Serialize(player);
var back = JsonSerializer.Deserialize<PlayerData>(json);

// 性能优化：源生成器
[JsonSerializable(typeof(PlayerData))]
partial class GameJsonContext : JsonSerializerContext { }

string fastJson = JsonSerializer.Serialize(player, GameJsonContext.Default.PlayerData);
```

### MessagePack (二进制 JSON)

```csharp
// dotnet add package MessagePack
using MessagePack;

[MessagePackObject]
public class PlayerData
{
    [Key(0)]
    public string Name { get; set; }
    [Key(1)]
    public int Level { get; set; }
}

byte[] data = MessagePackSerializer.Serialize(player);
var back = MessagePackSerializer.Deserialize<PlayerData>(data);
// 比 JSON 小 30%，快 2x
```

### MemoryPack (C# 专用)

```csharp
// dotnet add package MemoryPack
using MemoryPack;

[MemoryPackable]
public partial class PlayerData
{
    public string Name { get; set; }
    public int Level { get; set; }
}

byte[] data = MemoryPackSerializer.Serialize(player);
var back = MemoryPackSerializer.Deserialize<PlayerData>(data);
// 比 Protobuf 还快 2x，但只有 C# 用
```

### 性能对比 (100000 次序列化/反序列化)

| 方案 | 大小 | 序列化 | 反序列化 | 跨语言 |
|------|------|--------|---------|-------|
| JSON | 45B | 120ms | 140ms | 是 |
| Protobuf | 22B | 40ms | 35ms | 是 |
| MessagePack | 30B | 55ms | 50ms | 部分 |
| MemoryPack | 18B | 18ms | 15ms | 否 |

---

## 五、协议进阶设计

### 请求-响应模式

```csharp
public struct Packet
{
    public ushort MsgId;
    public uint SeqId;     // 序列号，关联请求和响应
    public byte[] Body;
}

class RpcManager
{
    private uint _nextSeqId = 1;
    private ConcurrentDictionary<uint, TaskCompletionSource<byte[]>> _pending = new();

    public async Task<LoginResponse> Call(LoginRequest req)
    {
        uint seqId = Interlocked.Increment(ref _nextSeqId);
        var tcs = new TaskCompletionSource<byte[]>();
        _pending.TryAdd(seqId, tcs);

        // 发送请求
        byte[] body = req.ToByteArray();
        Send(new Packet { MsgId = 0x1001, SeqId = seqId, Body = body });

        // 等待响应（超时 5s）
        using var cts = new CancellationTokenSource(5000);
        using (cts.Token.Register(() => tcs.TrySetCanceled()))
        {
            byte[] response = await tcs.Task;
            return LoginResponse.Parser.ParseFrom(response);
        }
    }

    public void OnResponse(Packet packet)
    {
        if (_pending.TryRemove(packet.SeqId, out var tcs))
            tcs.TrySetResult(packet.Body);
    }
}
```

### 批量消息

```csharp
// 多条小消息合并发送
class BatchedSender
{
    private List<byte[]> _pending = new();
    private Timer _flushTimer;
    private int _batchSize = 1024;
    private Socket _socket;

    public void Send(ushort msgId, byte[] body)
    {
        byte[] packet = MessageProtocol.Encode(msgId, body);
        lock (_pending)
        {
            _pending.Add(packet);
            if (GetPendingSize() >= _batchSize)
                Flush();
        }
    }

    private void Flush()
    {
        byte[][] batch;
        lock (_pending)
        {
            batch = _pending.ToArray();
            _pending.Clear();
        }

        // 将多条消息合并为一次 Send
        List<byte> combined = new List<byte>();
        foreach (var p in batch)
            combined.AddRange(p);

        _socket.Send(combined.ToArray());
    }

    private int GetPendingSize()
    {
        return _pending.Sum(p => p.Length);
    }
}
```

---

## 六、对比 C++ 协议处理

```cpp
// C++ Protobuf 使用
#include <google/protobuf/message.h>

PlayerInfo player;
player.set_player_id(1001);
player.set_name("Alice");
player.set_level(10);

string serialized;
player.SerializeToString(&serialized);

PlayerInfo parsed;
parsed.ParseFromString(serialized);

// 自定义协议（网络字节序）
#pragma pack(push, 1)
struct PacketHeader {
    uint16_t msg_id;    // 网络字节序
    uint32_t length;
};
struct LoginRequest {
    PacketHeader header;
    char account[32];
    char password[32];
};
#pragma pack(pop)

void send_login(SOCKET sock, const char* account, const char* pw) {
    LoginRequest req;
    req.header.msg_id = htons(0x1001);
    req.header.length = htonl(sizeof(LoginRequest) - sizeof(PacketHeader));
    strncpy_s(req.account, account, 32);
    strncpy_s(req.password, pw, 32);
    send(sock, (char*)&req, sizeof(req), 0);
}
```

### 对照表

| C# | C++ | 概念 |
|---|-----|------|
| `Protobuf-net` / `Google.Protobuf` | `protobuf` (libprotobuf) | 序列化库 |
| `ToByteArray()` | `SerializeToString()` | 序列化输出 |
| `Parser.ParseFrom()` | `ParseFromString()` | 反序列化输入 |
| `BitConverter.GetBytes()` + 大端 | `htons()` / `htonl()` | 网络字节序转换 |
| `Buffer.BlockCopy()` | `memcpy()` | 内存拷贝 |
| `MemoryStream` | `std::stringstream` | 流式写入 |

---

## 七、练习

1. **自定义协议实现**：写一个完整的消息帧编解码器，支持消息 ID + 长度 + 体
2. **Protobuf 定义**：定义一套 MMO 游戏的基本消息（登录、移动、聊天、战斗）
3. **编码对比**：用一个复杂结构体，分别用 JSON/Protobuf/MessagePack 编码，对比大小
4. **手工 Varint**：实现 Varint 编码和解码，验证 1/127/128/300/100000 这些边界值
5. **协议版本测试**：用新旧版本的 .proto 文件互相解析，验证前后兼容性

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 协议头 | 消息 ID + 长度，解决 TCP 粘包问题 |
| Varint | 小整数用少字节，Protobuf 压缩的核心 |
| Tag-Length-Value | Protobuf 每个字段的编码格式 |
| Wire Type | 告诉解析器该字段的数据类型 |
| 版本兼容 | 通过字段号+skip unknown 实现 |
| MemoryPack | C# 专用超快序列化，但不跨语言 |
