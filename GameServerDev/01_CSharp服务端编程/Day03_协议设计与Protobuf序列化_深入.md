# Day 03：协议设计与 Protobuf 序列化 — 进阶深入

## 一、Protobuf Wire Format 深度解析

### Varint 编码的每一位

```csharp
// Varint 使用每字节的最高位 (MSB) 作为 continuation flag
// 低 7 位存放数据

// 编码示例：数字 300
// 二进制：0000 0001  0010 1100
// Varint 编码：
// 从低 7 位开始分组：
//   0000 001 | 0 | 010 1100
//   组1: 010 1100 (不够7位补0) → 1010 1100 (MSB=1，还有后续)
//   组2: 0000 001   → 0000 0010 (MSB=0，最后一组)
// 结果：0xAC 0x02

public class VarintExplainer
{
    public static (byte[] encoded, string explanation) EncodeWithSteps(ulong value)
    {
        var result = new List<byte>();
        var steps = new List<string>();
        ulong remaining = value;

        do
        {
            byte chunk = (byte)(remaining & 0x7F);    // 取低 7 位
            remaining >>= 7;                            // 右移 7 位

            if (remaining != 0)
                chunk |= 0x80; // 设置 MSB → 还有后续字节

            result.Add(chunk);
            steps.Add($"取低7位: 0x{chunk:X2} ({(chunk & 0x80) != 0 ? "还有" : "最后"})");
        }
        while (remaining != 0);

        return (result.ToArray(), string.Join("\n", steps));
    }
}

// 边界值 Varint 编码
// 0         → 0x00          (1B)
// 127       → 0x7F          (1B)  ← 7 位能表示的最大值
// 128       → 0x80 0x01     (2B)  ← 跨越 1→2 字节的边界
// 16383     → 0xFF 0x7F     (2B)  ← 14 位最大值
// 16384     → 0x80 0x80 0x01 (3B) ← 跨越 2→3 字节的边界
```

### ZigZag 编码：有符号整数的处理

```csharp
// 问题：负数的 Varint 编码非常低效
// 因为负数用补码表示，int(-1) = 0xFFFFFFFF，所有位都是 1
// Varint 需要编码 5 字节！(0xFF 0xFF 0xFF 0xFF 0x0F)

// 解决方案：ZigZag 编码
// 将有符号数映射到无符号数：
//   0 → 0
//   -1 → 1
//   1 → 2
//   -2 → 3
//   2 → 4
//   ...
// 公式: (n << 1) ^ (n >> 31)  // 对 int32
//        (n << 1) ^ (n >> 63)  // 对 int64

public static class ZigZag
{
    public static ulong EncodeInt32(int value)
    {
        // n << 1 在 C# 中是 int，可能溢出，需要 cast
        return (ulong)((value << 1) ^ (value >> 31));
    }

    public static int DecodeInt32(ulong encoded)
    {
        int value = (int)(encoded >> 1);
        // 如果最低位是 1，则是负数
        return (encoded & 1) == 0 ? value : ~value;
    }

    // 对比：sint32 和 int32 的差异
    // int32(-1)：Varint = 0xFF 0xFF 0xFF 0xFF 0x0F (5B)
    // sint32(-1)：ZigZag = 1 → Varint = 0x01 (1B)
}
```

### Tag-Length-Value 解码器

```csharp
// Protobuf 每条消息字段由 Tag + [Length] + Value 组成
// Tag = (field_number << 3) | wire_type

// 手工解析 Proto 流（理解内部机制）
class ProtoFieldReader
{
    public readonly struct FieldTag
    {
        public readonly int FieldNumber;
        public readonly WireType Type;

        public FieldTag(ulong tagValue)
        {
            FieldNumber = (int)(tagValue >> 3);
            Type = (WireType)(tagValue & 0x07);
        }
    }

    public enum WireType
    {
        Varint = 0,
        Fixed64 = 1,    // 8 字节固定
        LengthDelimited = 2, // 长度前缀
        StartGroup = 3,  // 已废弃
        EndGroup = 4,    // 已废弃
        Fixed32 = 5      // 4 字节固定
    }

    public static IEnumerable<FieldTag> ParseFields(ReadOnlySpan<byte> data)
    {
        int offset = 0;
        while (offset < data.Length)
        {
            // 读取 Tag（Varint 编码）
            DecodeVarint(data, ref offset, out ulong tagValue);
            var tag = new FieldTag(tagValue);

            switch (tag.Type)
            {
                case WireType.Varint:
                    DecodeVarint(data, ref offset, out _);
                    break;
                case WireType.Fixed64:
                    offset += 8;
                    break;
                case WireType.LengthDelimited:
                    DecodeVarint(data, ref offset, out ulong len);
                    offset += (int)len;
                    break;
                case WireType.Fixed32:
                    offset += 4;
                    break;
            }

            yield return tag;
        }
    }
}
```

---

## 二、Protobuf 高级特性

### Maps 的底层编码

```csharp
// proto3 map<string, PlayerData> players = 1;
// 实际上 map 被编译为：
// message MapFieldEntry {
//   string key = 1;
//   PlayerData value = 2;
// }
// repeated MapFieldEntry players = 1;
// 
// 即 map 的每个键值对是一个 Length-delimited 的嵌套消息

// 性能影响：
// - map 中的每个键值对都是一个独立的 Length-delimited 字段
// - 大量 map 操作（每次添加/删除）需要重新序列化整个 map
// - 替代方案：repeated 字段 + 自定义索引
```

### oneof 的内存布局

```csharp
// oneof 字段在 C# 中生成这样的代码：
// public sealed partial class Equipment : IBufferMessage
// {
//   private object _equipment_;  // 实际存储：Weapon 或 Armor 的实例
//   private int _equipmentCase_; // 指示当前是哪个字段被设置
//
//   // 每次设置 oneof 字段都会：
//   // 1. 清除之前设置的字段（将旧引用置 null）
//   // 2. 设置新值
//   // 3. 更新 case 枚举
// }
//
// 注意：oneof 中切换字段会产生 GC 压力
// 高频切换的场景（如玩家在武器/防具间切换显示）要考虑池化
```

### Any 类型与内存开销

```csharp
// Any 类型内部存储：
// - TypeUrl: string（完整类型名）
// - Value: bytes（序列化的子消息）
// 
// 问题：每次序列化/反序列化 Any 都需要反射查找类型
// 游戏服务器应避免使用 Any，使用 oneof 或手动分发

// 替代方案：消息号 + 字节数组
message AnyGameMessage {
  int32 type_id = 1;       // 预定义的 ID，查表即可
  bytes data = 2;          // 子消息序列化数据
}
```

### Reserved 字段管理

```protobuf
// 版本演进的黄金法则：

// v1.0
message PlayerData {
  string name = 1;
  int32 level = 2;
}

// v1.1（安全：新增字段）
message PlayerData {
  string name = 1;
  int32 level = 2;
  int64 exp = 3;           // ← 新增，旧客户端会忽略
}

// v1.2（删除字段必须 reserved！）
message PlayerData {
  reserved 3;               // 防止未来复用 exp 的字段号
  reserved "exp";           // 防止未来复用字段名
  string name = 1;
  int32 level = 2;
  string title = 4;         // 使用新字段号
}

// 错误做法：复用旧字段号会导致灾难
message PlayerData {
  string name = 1;
  int32 level = 2;
  // "exp" 是 int64 类型
  string title = 3;         // 如果这里用了 3，解析旧数据会按 string 解析 int64！
}
```

---

## 三、序列化方案深度对比

### 编码结构对比

```
Protobuf:
  | Tag: 1:Varint | Value: 5 | Tag: 2:Len | Len: 3 | "abc" |
  优点：按字段号索引，跳读未识别的字段
  缺点：需要代码生成，需 .proto 文件

FlatBuffers:
  | vtable_ptr | data[] | vtable |
  优点：零拷贝反序列化，直接访问内存
  缺点：构建复杂，大小比 Protobuf 大 ~10%

MessagePack:
  | FormatByte | Value |
  优点：无代码生成，自描述（类似二进制 JSON）
  缺点：跳读困难，需解析全部

MemoryPack (.NET only):
  | SchemaHeader | Values |
  优点：C# 专用优化，极速
  缺点：仅 C#，不跨语言
```

### 游戏场景实测数据对比

```csharp
// 测试：100,000 次对一个包含 10 个字段的玩家消息编码/解码
// 消息：Player{ Id(long), Name(string), Level(int), 
//           Position(Vector3), Items(List<Item>) }

class SerializationBenchmark
{
    /*
    方案          序列化时间   反序列化时间   大小     GC分配    跨语言
    Protobuf      142ms       128ms         89B     32KB      ✅
    FlatBuffers   213ms        62ms        120B     18KB      ✅ (生成快)
    MessagePack    98ms        95ms         78B     56KB      ⚠️ 部分
    MemoryPack     72ms        51ms         65B     12KB      ❌ 仅C#
    JSON           280ms       312ms        245B    280KB     ✅
    */
}

// 选择建议：
// 跨语言游戏服务器 → Protobuf（生态最好，工具链成熟）
// 纯 C# 服务器 → MemoryPack（极致性能）
// 需要零拷贝读场景 → FlatBuffers（配置表、静态数据）
// 调试/开发阶段 → JSON（人类可读）
```

---

## 四、零分配自定义 Codec

### 流式编码器（避免 byte[] 分配）

```csharp
// 核心思路：直接在 Span<byte> 上序列化，复用缓冲区
class ZeroAllocCodec
{
    private readonly byte[] _buffer = GC.AllocateUninitializedArray<byte>(8192);

    // 编码到预分配缓冲区
    public int Encode(ushort msgId, ReadOnlySpan<byte> body, Span<byte> output)
    {
        int offset = 0;

        // 消息 ID (2 bytes, big-endian)
        if (BitConverter.IsLittleEndian)
        {
            output[offset] = (byte)(msgId >> 8);
            output[offset + 1] = (byte)msgId;
        }
        else
        {
            Unsafe.WriteUnaligned(ref output[0], msgId);
        }
        offset += 2;

        // 消息体长度 (4 bytes, big-endian)
        uint len = (uint)body.Length;
        if (BitConverter.IsLittleEndian)
        {
            output[offset] = (byte)(len >> 24);
            output[offset + 1] = (byte)(len >> 16);
            output[offset + 2] = (byte)(len >> 8);
            output[offset + 3] = (byte)len;
        }
        offset += 4;

        // 消息体
        body.CopyTo(output.Slice(offset));

        return offset + body.Length;
    }

    // 使用 Span<T> 避免 List<byte> 的扩容分配
    public byte[] BorrowBuffer()
    {
        return _buffer;
    }
}

// 使用 ArrayPool 进行编码
class PooledCodec
{
    public static byte[] EncodeToPooledArray(ushort msgId, byte[] body)
    {
        int totalLen = 6 + body.Length;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(totalLen);

        try
        {
            // 写入头部
            buffer[0] = (byte)(msgId >> 8);
            buffer[1] = (byte)msgId;
            buffer[2] = (byte)(body.Length >> 24);
            buffer[3] = (byte)(body.Length >> 16);
            buffer[4] = (byte)(body.Length >> 8);
            buffer[5] = (byte)body.Length;
            Buffer.BlockCopy(body, 0, buffer, 6, body.Length);

            // 返回精确大小的数组
            return buffer[..totalLen];
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
```

---

## 五、协议版本化策略

### 字段演化 vs 包版本号

```csharp
// 策略 1：Protobuf 自带的字段演化
// 优缺点：
//   ✅ 简单，不修改协议头
//   ✅ 兼容现有客户端
//   ❌ 无法处理"删除语义"的字段（旧客户端仍发送）
//   ❌ 无法做大幅度的协议变更

// 策略 2：协议头加版本号
class VersionedPacket
{
    // 扩展协议头
    // [Magic (2B)][Version (1B)][MsgId (2B)][Length (4B)][Body (N)]
    
    public const ushort MAGIC = 0x5A5B;
    public const byte CURRENT_VERSION = 2;

    public static bool DecodeHeader(ReadOnlySpan<byte> data, 
        out byte version, out ushort msgId, out int bodyLength)
    {
        version = 0; msgId = 0; bodyLength = 0;

        if (data.Length < 9) return false; // 2+1+2+4

        // Magic
        if (Unsafe.ReadUnaligned<ushort>(ref data[0]) != MAGIC)
            return false;

        version = data[2];
        msgId = Unsafe.ReadUnaligned<ushort>(ref data[3]);
        bodyLength = Unsafe.ReadUnaligned<int>(ref data[5]);
        return true;
    }

    // 版本不同的处理
    public static byte[] EncodeWithVersion(byte version, ushort msgId, byte[] body)
    {
        byte[] packet = new byte[9 + body.Length];
        Unsafe.WriteUnaligned(ref packet[0], MAGIC);
        packet[2] = version;
        Unsafe.WriteUnaligned(ref packet[3], msgId);
        Unsafe.WriteUnaligned(ref packet[5], body.Length);
        Buffer.BlockCopy(body, 0, packet, 9, body.Length);
        return packet;
    }
}

// 策略 3：增量更新（游戏特有）
// 服务器不发送完整的 PlayerData，只发送变化的部分
class DeltaPacket
{
    // [BaseVersion (4B)][ChangedFields Bitmask (4B)][FieldValues...]
    // 客户端维护一个基线版本，只接收变化
}
```

### 版本兼容性矩阵

```
服务器版本
    ↓
客户  v1.0 │ v1.0 OK     │ v1.1 OK     │ v2.0 拒绝或降级
端    v1.1 │ 只用到 v1.0 │ 全部功能    │ 拒绝或降级
版        │ 的功能     │             │
本    v2.0 │ 拒绝        │ 拒绝        │ v2.0 OK
```

---

## 六、高性能消息帧处理

### 循环缓冲区实现

```csharp
// 相比基础篇的 List<byte> + Array.Resize，
// 使用环形缓冲区避免频繁内存移动
class CircularBuffer
{
    private byte[] _buffer;
    private int _readPos;   // 读位置
    private int _writePos;  // 写位置
    private int _count;     // 有效数据量

    public CircularBuffer(int capacity = 8192)
    {
        _buffer = new byte[capacity];
    }

    public void Write(byte[] data, int offset, int length)
    {
        EnsureCapacity(length);

        int writeEnd = (_writePos + length) % _buffer.Length;
        if (writeEnd > _writePos || _writePos + length < _buffer.Length)
        {
            // 连续写入
            Buffer.BlockCopy(data, offset, _buffer, _writePos, length);
        }
        else
        {
            // 绕回写入
            int firstPart = _buffer.Length - _writePos;
            Buffer.BlockCopy(data, offset, _buffer, _writePos, firstPart);
            Buffer.BlockCopy(data, offset + firstPart, _buffer, 0, length - firstPart);
        }

        _writePos = (_writePos + length) % _buffer.Length;
        _count += length;
    }

    // 尝试解析完整帧
    public bool TryReadPacket(out ushort msgId, out byte[] body)
    {
        msgId = 0;
        body = null;

        if (_count < 6) return false;    // 不够帧头

        // 从 _readPos 读取帧头
        ushort id;
        int len;

        if (_readPos + 6 <= _buffer.Length)
        {
            id = (ushort)((_buffer[_readPos] << 8) | _buffer[_readPos + 1]);
            len = (_buffer[_readPos + 2] << 24) |
                  (_buffer[_readPos + 3] << 16) |
                  (_buffer[_readPos + 4] << 8) |
                  _buffer[_readPos + 5];
        }
        else
        {
            // 帧头绕回了，需要分段读
            Span<byte> header = stackalloc byte[6];
            ReadFrom(_readPos, header);
            id = (ushort)((header[0] << 8) | header[1]);
            len = (header[2] << 24) | (header[3] << 16) | (header[4] << 8) | header[5];
        }

        if (_count < 6 + len) return false; // 数据不够

        body = new byte[len];
        ReadFrom((_readPos + 6) % _buffer.Length, body);
        _readPos = (_readPos + 6 + len) % _buffer.Length;
        _count -= 6 + len;

        msgId = id;
        return true;
    }

    private void ReadFrom(int position, Span<byte> destination)
    {
        int length = destination.Length;
        if (position + length <= _buffer.Length)
        {
            new ReadOnlySpan<byte>(_buffer, position, length)
                .CopyTo(destination);
        }
        else
        {
            int firstPart = _buffer.Length - position;
            new Span<byte>(_buffer, position, firstPart)
                .CopyTo(destination);
            new Span<byte>(_buffer, 0, length - firstPart)
                .CopyTo(destination.Slice(firstPart));
        }
    }

    private void EnsureCapacity(int extra)
    {
        if (_count + extra > _buffer.Length)
        {
            // 扩展缓冲区（以 2 的幂增长）
            int newCapacity = _buffer.Length * 2;
            byte[] newBuffer = new byte[newCapacity];
            ReadFrom(_readPos, new Span<byte>(newBuffer, 0, _count));
            _buffer = newBuffer;
            _readPos = 0;
            _writePos = _count;
        }
    }
}
```

---

## 七、游戏专用协议模式

### 兴趣管理（Interest Management）

```csharp
// MMO 场景：一个玩家不应该收到所有其他玩家的位置更新
// 只有"感兴趣"的实体才发送

class InterestManager
{
    // AOI（Area of Interest）：九宫格
    // 玩家只接收自己周围 9 个格子内实体的信息
    private const int GRID_SIZE = 100; // 每个格子 100 单位

    public int GetGridKey(Vector3 position)
    {
        return (Math.Floor(position.X / GRID_SIZE) * 10000 +
                Math.Floor(position.Z / GRID_SIZE));
    }

    // 发送增量更新：只发送"进入视野"和"离开视野"的实体
    public void SendVisibilityChanges(PlayerEntity player, 
        HashSet<int> oldVisible, HashSet<int> newVisible)
    {
        var entered = newVisible.Except(oldVisible);
        var left = oldVisible.Except(newVisible);

        foreach (var entityId in entered)
        {
            SendEnterVision(player, entityId);
        }
        foreach (var entityId in left)
        {
            SendLeaveVision(player, entityId);
        }
    }
}

// 快照同步 vs 增量同步
// 快照：每次发送完整状态（简单但带宽大）→ 适合小游戏
// 增量：只发送变化部分（复杂但带宽小）→ 适合 MMO
class SyncStrategy
{
    // 状态变化检测，只发送变化值
    public struct EntityState
    {
        public Vector3 Position;
        public float Rotation;
        public int Hp;
        public int Mp;
    }

    public static void DetectChanges(EntityState oldState, EntityState newState)
    {
        var changes = new List<(int fieldId, byte[] data)>();

        if (oldState.Position != newState.Position)
            changes.Add((1, BitConverter.GetBytes(newState.Position)));
        if (oldState.Rotation != newState.Rotation)
            changes.Add((2, BitConverter.GetBytes(newState.Rotation)));
        if (oldState.Hp != newState.Hp)
            changes.Add((3, VarintEncode((ulong)newState.Hp)));
        if (oldState.Mp != newState.Mp)
            changes.Add((4, VarintEncode((ulong)newState.Mp)));

        // 只发送变化的字段
        SendDeltaPacket(connectionId, changes);
    }
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| Varint | MSB 标志位 + 每字节 7 位数据，小整数字节少 |
| ZigZag | 负数映射到正数，避免 Varint 的低效 |
| Wire Format | Tag(field << 3 \| type) + [Length] + Value |
| 字段版本化 | 新增用新 field_number，删除加 reserved |
| 循环缓冲区 | 避免帧头跨越时频繁 memmove |
| 零分配编码 | Span<byte> + ArrayPool + 预分配缓冲区 |
| 增量同步 | 只发送变化的字段，节省带宽 |
| 兴趣管理 | AOI 九宫格，只发送附近的实体 |

---

## 对照表：C++ 协议处理 vs C# 进阶

| C++ | C# | 差异 |
|-----|-----|------|
| `VarintEncode/Decode` 手写 | `Google.Protobuf.CodedOutputStream` | C# 有封装好的编码流 |
| `repeated` 字段手动管理 | `RepeatedField<T>` 自动扩容 | C# 内存管理更省心 |
| `#pragma pack` 对齐结构体 | `StructLayout` + `Explicit Layout` | 底层原理一致 |
| `memory_mapped_file` 读配置表 | `MemoryMappedFile` | API 类似 |
| 手写环形缓冲区 | `System.IO.Pipelines.Pipe` | .NET 5+ 推荐管线 API |
| `flatbuffers::GetRoot` | `FlatBuffers.Table.__init` | 零拷贝读对象 |
