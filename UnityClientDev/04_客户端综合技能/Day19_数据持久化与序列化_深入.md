# Day 19：数据持久化与序列化 — MessagePack、FlatBuffers、云存档与加密

## 0. 为什么需要更高级的序列化技术？

基础文档已经介绍了 JSON、二进制、Protobuf 的基础用法。但实际项目中你会遇到更复杂的需求：

```
基础方案解决不了的问题：
1. 性能瓶颈：JsonUtility 序列化 10 万个对象要几秒
2. 内存开销：JSON 字符串比二进制大 3~5 倍
3. 版本迁移：游戏上线半年后要改数据结构
4. 安全性：存档被玩家修改（改金币、改道具）
5. 云存档：数据要跨平台、跨设备同步
6. 热更新：旧的存档格式不能阻止新版本运行
```

高级序列化方案的选择取决于你的场景：

| 场景 | 推荐方案 | 原因 |
|------|---------|------|
| 网络消息（高速） | MessagePack | 比 Protobuf 更小更快，C# 集成好 |
| 配置数据（大量） | FlatBuffers | 零反序列化开销，直接内存读取 |
| 网络传输（标准） | Protobuf | 跨语言，工具链成熟 |
| 存档加密 | AES + HMAC | 安全标准，非自创算法 |
| 云存档 | REST API + 本地缓存 | 标准 Web 方案 |

---

## 1. MessagePack for C# 深入

### 为什么 MessagePack 比 JSON 快？

```
JSON 序列化一个玩家对象：
{"name":"Lucy","level":10,"hp":100}
→ 存储了字段名（"name" = 6 字节）
→ 数字存为文本（"10" = 2 字节）
→ 总共 38 字节

MessagePack 序列化相同对象：
0x81 0xA4 "Lucy" 0x0A 0x18 0x64
→ 字段用索引编号（1 字节）
→ 数字用二进制（1 字节）
→ 总共 11 字节（节省 70%）
```

### MessagePack-CSharp 基础

```csharp
// NuGet: MessagePack
using MessagePack;

[MessagePackObject]
public class PlayerData
{
    [Key(0)]
    public string Name { get; set; }

    [Key(1)]
    public int Level { get; set; }

    [Key(2)]
    public float Hp { get; set; }

    [Key(3)]
    [IgnoreMember]  // 忽略这个字段
    public string Password { get; set; }
}

// 序列化
PlayerData player = new() { Name = "Lucy", Level = 10, Hp = 100 };
byte[] msgpack = MessagePackSerializer.Serialize(player);

// 反序列化
PlayerData loaded = MessagePackSerializer.Deserialize<PlayerData>(msgpack);

// 直接序列化到 Stream（适合网络发送）
using MemoryStream ms = new();
MessagePackSerializer.Serialize(ms, player);
```

### 高级：Contractless 模式

```csharp
// Contractless 模式：不需要添加 [MessagePackObject] 和 [Key] 属性
// 适合快速原型但性能略低

public class NoAttrData
{
    public string Name { get; set; }
    public int Score { get; set; }
    public float[] Positions { get; set; }
}

// 使用 Contractless 解析器
var options = MessagePackSerializerOptions.Standard
    .WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);

byte[] data = MessagePackSerializer.Serialize(new NoAttrData
{
    Name = "Test",
    Score = 100,
    Positions = new[] { 1f, 2f, 3f }
}, options);

var obj = MessagePackSerializer.Deserialize<NoAttrData>(data, options);
```

### 高级：动态类型与字典

```csharp
// MessagePack 可以序列化复杂的动态结构
public class BattleEventData
{
    [Key(0)]
    public string EventType { get; set; }

    [Key(1)]
    public Dictionary<string, object> Parameters { get; set; }
    // ↑ 非常适合"通用事件"系统
}

// MessagePack 的 object 类型支持有限的原生类型：
// int, float, string, bool, byte[], List<object>, Dictionary<string, object>
// 自定义类型需要用 Typeless 模式

// Typeless 模式（序列化类型信息，类似 BinaryFormatter 但更快）
byte[] blob = MessagePackSerializer.Typeless.Serialize(anyObject);
object restored = MessagePackSerializer.Typeless.Deserialize(blob);
```

### MessagePack 性能对比

```csharp
// 序列化 10,000 个对象的性能对比
// ┌──────────────┬──────────────┬──────────────┬──────────┐
// │ 方案          │ 序列化时间   │ 反序列化时间 │ 大小     │
// ├──────────────┼──────────────┼──────────────┼──────────┤
// │ JSON (NF)    │ 185ms        │ 220ms        │ 1.2MB    │
// │ Protobuf-net │ 95ms         │ 110ms        │ 420KB    │
// │ MessagePack  │ 72ms         │ 80ms         │ 380KB    │
// │ FlatBuffers  │ 55ms (构建)  │ 0ms (直接读) │ 350KB    │
// └──────────────┴──────────────┴──────────────┴──────────┘
```

---

## 2. FlatBuffers for Unity

### 核心概念

```
FlatBuffers 最独特的特点：零反序列化

普通序列化方案：
数据 → 序列化 → 字节数组 → 反序列化 → C# 对象（从字节复制到对象）
                                                    ↑ 这一步很慢

FlatBuffers：
数据 → 序列化 → 字节数组 → 直接读取字段（不创建 C# 对象！）
                                   ↑ 直接通过偏移量读内存
```

### 安装与 .fbs 定义

```
// 1. NuGet: FlatBuffers
// 2. 下载 flatc 编译器（https://github.com/google/flatbuffers）
// 3. 写 .fbs 文件

// player.fbs
namespace Game.Data;

table PlayerData {
    id: int;
    name: string;
    level: int = 1;
    hp: float = 100.0;
    is_online: bool = false;
    inventory: [Item];   // 数组
    stats: Stat;         // 内嵌 table
}

table Item {
    id: int;
    count: short;
}

table Stat {
    attack: float;
    defense: float;
    speed: float;
}

root_type PlayerData;
```

### 生成 C# 代码并使用

```
// 命令行生成 C# 代码：
flatc --csharp player.fbs

// 生成的文件：Game/Data/PlayerData.cs, Item.cs, Stat.cs
```

```csharp
using FlatBuffers;
using Game.Data;

public class FlatBuffersExample
{
    public byte[] BuildPlayerData()
    {
        var builder = new FlatBufferBuilder(1024);

        // 构建字符串
        StringOffset name = builder.CreateString("Lucy");

        // 构建内嵌 table
        var stats = Stat.CreateStat(builder, 100f, 50f, 3.5f);

        // 构建数组
        var itemOffsets = new Offset<Item>[2];
        itemOffsets[0] = Item.CreateItem(builder, 1, 10);
        itemOffsets[1] = Item.CreateItem(builder, 2, 5);
        VectorOffset inventory = builder.CreateVector(itemOffsets);

        // 构建根 table
        var player = PlayerData.CreatePlayerData(
            builder,
            1001,       // id
            name,       // name
            10,         // level
            100f,       // hp
            true,       // is_online
            inventory,  // inventory
            stats       // stats
        );

        builder.Finish(player.Value);
        return builder.SizedByteArray();
    }

    // 读取（零反序列化！）
    public void ReadPlayerData(byte[] buffer)
    {
        var player = PlayerData.GetRootAsPlayerData(new ByteBuffer(buffer));

        // 直接读字段（通过偏移量访问内存）
        int id = player.Id;                  // O(1)
        string name = player.Name;            // 直接读 UTF-8 字符
        int level = player.Level;
        float hp = player.Hp;

        // 读数组
        for (int i = 0; i < player.InventoryLength; i++)
        {
            var item = player.Inventory(i).Value;
            Debug.Log($"道具 {item.Id} × {item.Count}");
        }
    }
}
```

### FlatBuffers 的适用场景

```
✅ 适合：
- 大型配置表（怪物数据、道具数据、关卡配置）
- 频繁读取的网络数据包
- 需要直接内存映射的文件
- 对 GC 敏感的场景（零分配）

❌ 不适合：
- 小数据量（构建开销 > 读取收益）
- 需要频繁修改的数据（不可变）
- 嵌套层次太深（访问链长）
- 数据量极小（FlatBuffers 的偏移量表有额外开销）

对比 C++：FlatBuffers 在 C++ 中的用法几乎一样
（builder.CreateString → 直接读取字段指针）
```

### MemoryPack — Unity 新选择

```csharp
// MemoryPack 是 C# 专有的高性能序列化库
// 原理：直接内存复制（类似 C++ 的 memcpy）
// NuGet: MemoryPack

[MemoryPackable]
public partial class PlayerData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public float[] Positions { get; set; }
}

// 序列化
byte[] data = MemoryPackSerializer.Serialize(player);

// 反序列化
var loaded = MemoryPackSerializer.Deserialize<PlayerData>(data);

// 性能接近 BinaryReader 手动序列化
// 比 MessagePack 快 2~3 倍
```

---

## 3. Protobuf 高级特性

### oneof — 联合类型

```protobuf
syntax = "proto3";

message SkillResult {
    int32 skill_id = 1;

    // oneof：以下字段同时只能有一个有值
    oneof result_type {
        DamageInfo damage = 10;
        HealInfo heal = 11;
        BuffInfo buff = 12;
        StatusEffect status = 13;
    }
}

message DamageInfo {
    int32 value = 1;
    bool is_critical = 2;
    string damage_type = 3;
}

message HealInfo {
    int32 value = 1;
    string source = 2;
}
```

```csharp
// C# 中使用 oneof
var result = new SkillResult
{
    SkillId = 1001,
    Damage = new DamageInfo { Value = 50, IsCritical = true }
};

// 检查哪个字段被设置
switch (result.ResultTypeCase)
{
    case SkillResult.ResultTypeOneofCase.Damage:
        Debug.Log($"造成 {result.Damage.Value} 点伤害");
        break;
    case SkillResult.ResultTypeOneofCase.Heal:
        Debug.Log($"治疗 {result.Heal.Value} 点");
        break;
    case SkillResult.ResultTypeOneofCase.Buff:
        Debug.Log($"附加 Buff");
        break;
}
```

### map — 字典字段

```protobuf
message PlayerStats {
    // 字段名不能和 map 关键字冲突
    map<string, int32> stats = 1;  // key → value
    map<int32, int32> skill_cooldowns = 2;
}

// 序列化后非常紧凑：
// 每个 map 条目 = key 字段 + value 字段
```

### Any — 动态消息类型

```protobuf
// Any 可以存储任意 Protobuf 消息（类型擦除）
import "google/protobuf/any.proto";

message ServerMessage {
    int32 msg_id = 1;
    google.protobuf.Any payload = 2;  // 可以存任何消息
}

// 需要注册类型才能序列化/反序列化
```

```csharp
// C# 中使用 Any
var msg = new ServerMessage
{
    MsgId = 2001,
    Payload = Any.Pack(new LoginRequest { Username = "Lucy" })
};

// 解包
if (msg.Payload.Is(LoginRequest.Descriptor))
{
    var login = msg.Payload.Unpack<LoginRequest>();
    Debug.Log(login.Username);
}
```

### Protobuf 的陷阱

```
1. map 不能包含 message 作为 key（只支持标量类型）
2. oneof 不能是 repeated
3. 字段编号 1~15 用 1 字节，16~2047 用 2 字节
   → 高频字段用 1~15
4. 删除的字段用 reserved 保留编号
5. 不要改变字段类型（int32 → uint64 会解析错误）
```

---

## 4. BinaryFormatter 的陷阱

### 为什么不推荐 BinaryFormatter

```csharp
// .NET 官方已经标记 BinaryFormatter 为过时/危险

// ❌ 危险用法
BinaryFormatter formatter = new();

// 反序列化时可能执行任意代码！
// 攻击者可以构造恶意数据执行系统命令
using FileStream fs = File.OpenRead("save.dat");
PlayerData data = (PlayerData)formatter.Deserialize(fs);
// 如果 save.dat 被篡改 → 可能会执行恶意代码

// ❌ 问题 2：版本兼容差
// 改了类的命名空间 → 反序列化失败
// 改了字段名 → 反序列化失败
// 删除字段 → 反序列化失败

// ❌ 问题 3：性能差
// 使用反射，比手动序列化慢 10~50 倍
```

### 替代方案

```
BinaryFormatter → 用 BinaryReader/Writer 手动序列化
或 MemoryPack（类似 memcpy 速度）
或 Protobuf/MessagePack（带版本兼容）
```

---

## 5. 存档加密深入

### AES 加密（推荐替代 XOR）

```csharp
using System.Security.Cryptography;

public class AesSaveEncryption
{
    // AES 是对称加密标准，比 XOR 安全得多
    private static readonly byte[] KEY = Convert.FromBase64String(
        "A1b2C3d4E5f6G7h8I9j0K1l2M3n4O5p6=");  // 32 字节 = 256 位
    private static readonly byte[] IV = Convert.FromBase64String(
        "Q1w2E3r4T5y6U7i8=");  // 16 字节

    public static byte[] Encrypt(byte[] data)
    {
        using Aes aes = Aes.Create();
        aes.Key = KEY;
        aes.IV = IV;

        using MemoryStream output = new();
        using CryptoStream crypto = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
        crypto.Write(data, 0, data.Length);
        crypto.FlushFinalBlock();
        return output.ToArray();
    }

    public static byte[] Decrypt(byte[] encrypted)
    {
        using Aes aes = Aes.Create();
        aes.Key = KEY;
        aes.IV = IV;

        using MemoryStream input = new(encrypted);
        using CryptoStream crypto = new(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using MemoryStream output = new();
        crypto.CopyTo(output);
        return output.ToArray();
    }
}
```

### 存档完整性校验

```
存档文件结构：
┌────────────────────────────┐
│ Magic Number (4 bytes)     │ ← 文件类型标识 "SAVE"
├────────────────────────────┤
│ Version (4 bytes)          │ ← 存档格式版本
├────────────────────────────┤
│ Encrypted Data             │ ← AES 加密的数据
├────────────────────────────┤
│ HMAC-SHA256 (32 bytes)     │ ← 校验签名
└────────────────────────────┘

校验流程：
1. 读取 HMAC → 计算数据部分的 HMAC
2. 如果 HMAC 不匹配 → 存档被篡改
3. 匹配 → 解密并使用数据
```

```csharp
public class IntegritySaveManager
{
    private const uint MAGIC = 0x45564153; // "SAVE"
    private const int CURRENT_VERSION = 3;
    private readonly HMACSHA256 hmac = new(Encoding.UTF8.GetBytes("your-secret-key"));

    public void SaveSecure<T>(string path, T data)
    {
        // 1. 序列化
        byte[] raw = MessagePackSerializer.Serialize(data);

        // 2. 加密
        byte[] encrypted = AesSaveEncryption.Encrypt(raw);

        // 3. 构建文件
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        writer.Write(MAGIC);
        writer.Write(CURRENT_VERSION);
        writer.Write(encrypted.Length);
        writer.Write(encrypted);

        // 4. 计算并写入 HMAC
        byte[] fileContent = ms.ToArray();
        byte[] hash = hmac.ComputeHash(fileContent);
        writer.Write(hash);

        // 5. 写入磁盘
        File.WriteAllBytes(path, ms.ToArray());
    }

    public T LoadSecure<T>(string path) where T : class
    {
        byte[] fileContent = File.ReadAllBytes(path);

        // 分离 HMAC
        byte[] hash = fileContent[^32..];
        byte[] content = fileContent[..^32];

        // 验证完整性
        byte[] computedHash = hmac.ComputeHash(content);
        if (!CompareBytes(hash, computedHash))
        {
            Debug.LogError("存档被篡改或损坏");
            return null;
        }

        using MemoryStream ms = new(content);
        using BinaryReader reader = new(ms);

        // 解析文件头
        uint magic = reader.ReadUInt32();
        int version = reader.ReadInt32();
        int encryptedLen = reader.ReadInt32();

        if (magic != MAGIC)
        {
            Debug.LogError("不是有效的存档文件");
            return null;
        }

        // 版本兼容
        if (version > CURRENT_VERSION)
        {
            Debug.LogError("存档版本过高（来自新版本客户端）");
            return null;
        }

        // 解密
        byte[] encrypted = reader.ReadBytes(encryptedLen);
        byte[] raw = AesSaveEncryption.Decrypt(encrypted);

        // 反序列化
        return MessagePackSerializer.Deserialize<T>(raw);
    }

    private bool CompareBytes(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        // 恒定时间比较（防止时序攻击）
        int diff = 0;
        for (int i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
```

### 密钥管理策略

```
不要把密钥写死在代码里！

常见方案：
1. 密钥分片：把密钥拆成 3 份，拼接得到完整密钥
2. 设备绑定：用 SystemInfo.deviceUniqueIdentifier 生成密钥
3. 服务器下发：登录时从服务器获取密钥（最安全）
4. 白箱加密：用代码混淆 + 白箱 AES 库（防逆向）
```

---

## 6. 云存档集成模式

### 架构

```
┌──────────┐      ┌──────────┐      ┌──────────┐
│  本地存档  │      │ 云存档服务 │      │  备用存档  │
│  (最快)   │ ◀──▶ │  (安全)  │ ◀──▶ │  (容错)  │
└──────────┘      └──────────┘      └──────────┘
     ↓                 ↓                 ↓
  快速读写           跨设备同步         损坏恢复
```

### Unity 实现

```csharp
using Newtonsoft.Json;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public class CloudSaveManager : MonoBehaviour
{
    private const string SAVE_API = "https://api.example.com/save";
    private string authToken;  // 登录后获取

    // 三层保存策略
    public async UniTask SaveGame(PlayerSaveData data)
    {
        // 1. 本地快速保存（立刻生效）
        SaveLocal(data);

        // 2. 云存档（异步，不阻塞）
        _ = SaveCloudAsync(data);

        // 3. 本地备份（容错）
        SaveBackup(data);
    }

    public async UniTask<PlayerSaveData> LoadGame()
    {
        // 1. 尝试加载本地存档
        PlayerSaveData local = LoadLocal();
        if (local != null) return local;

        // 2. 本地坏了 → 云存档
        PlayerSaveData cloud = await LoadCloudAsync();
        if (cloud != null) return cloud;

        // 3. 云存档也坏了 → 备份
        PlayerSaveData backup = LoadBackup();
        return backup;  // 可能为 null（全新开始）
    }

    private void SaveLocal(PlayerSaveData data)
    {
        // 使用 AES + HMAC 加密保存
        byte[] raw = MessagePackSerializer.Serialize(data);
        byte[] encrypted = AesSaveEncryption.Encrypt(raw);
        File.WriteAllBytes(GetSavePath("local.sav"), encrypted);
    }

    private async UniTask SaveCloudAsync(PlayerSaveData data)
    {
        // 添加冲突解决用的时间戳
        data.lastSaveTime = DateTime.UtcNow.ToBinary();

        byte[] raw = MessagePackSerializer.Serialize(data);
        byte[] encrypted = AesSaveEncryption.Encrypt(raw);

        using UnityWebRequest req = new UnityWebRequest(SAVE_API, "POST");
        req.uploadHandler = new UploadHandlerRaw(encrypted);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", $"Bearer {authToken}");

        await req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log("云存档成功");
    }

    // 冲突解决（使用"最后写入"策略）
    public async UniTask<PlayerSaveData> ResolveConflict(
        PlayerSaveData localData, PlayerSaveData cloudData)
    {
        // 简单的方案：取时间戳最新的
        if (localData.lastSaveTime > cloudData.lastSaveTime)
            return localData;
        return cloudData;

        // 复杂方案：按字段合并（不丢任何数据）
        // 比如经验值取大的，道具列表取并集
    }
}

[System.Serializable]
public class PlayerSaveData
{
    public string playerName;
    public int level;
    public int exp;
    public int gold;
    public float posX, posY, posZ;
    public long lastSaveTime;
    public string saveVersion;  // 游戏版本号
}
```

---

## 7. 数据版本化与迁移

### 完整迁移系统

```csharp
// 设计目标：游戏从 1.0 升级到 3.0，所有中间版本的存档都能读

// 版本接口
public interface IDataMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    byte[] Migrate(byte[] data);
}

// 逐版本迁移链
public class DataMigrator
{
    private readonly SortedDictionary<int, IDataMigration> migrations = new();
    private const int CURRENT_DATA_VERSION = 5;

    public void RegisterMigration(IDataMigration migration)
    {
        migrations[migration.FromVersion] = migration;
    }

    public byte[] MigrateToCurrent(byte[] data, int fromVersion)
    {
        int currentVersion = fromVersion;

        while (currentVersion < CURRENT_DATA_VERSION)
        {
            if (migrations.TryGetValue(currentVersion, out var migration))
            {
                data = migration.Migrate(data);
                currentVersion = migration.ToVersion;
                Debug.Log($"数据迁移：v{migration.FromVersion} → v{migration.ToVersion}");
            }
            else
            {
                Debug.LogError($"没有找到从 v{currentVersion} 的迁移路径");
                return null;
            }
        }

        return data;
    }
}

// 具体迁移示例：v1 → v2（新增 maxHp 字段）
public class V1ToV2Migration : IDataMigration
{
    public int FromVersion => 1;
    public int ToVersion => 2;

    public byte[] Migrate(byte[] data)
    {
        using MemoryStream ms = new(data);
        using BinaryReader reader = new(ms);

        // v1 格式：name(string), level(int), hp(int), gold(int)
        string name = reader.ReadString();
        int level = reader.ReadInt32();
        int hp = reader.ReadInt32();
        int gold = reader.ReadInt32();

        // v2 格式：新增 maxHp
        using MemoryStream output = new();
        using BinaryWriter writer = new(output);

        writer.Write(name);
        writer.Write(level);
        writer.Write(hp);
        writer.Write(gold);
        writer.Write(hp);  // v2 新增 maxHp，旧存档用当前 hp 作为 maxHp

        return output.ToArray();
    }
}
```

### 迁移测试策略

```
每次数据迁移必须写单元测试：

1. 正向迁移：v1 → v2 → v3 → ... → vN 全部能转
2. 所有字段在新格式中正确
3. 丢失的字段用合理的默认值

测试用例：
// v1 存档 → 迁移到最新 → 检查 name, level, hp 是否正确
// v3 存档 → 迁移到最新 → 不重复执行 v1→v2 的迁移
// 最新版本 → 不执行迁移 → 直接反序列化
```

---

## C++/Raylib 对比

| 概念 | C++ 实现 | Unity/C# 实现 |
|------|---------|---------------|
| 二进制序列化 | memcpy + fwrite | MemoryPack / BinaryWriter |
| JSON | nlohmann/json | JsonUtility / Newtonsoft.Json |
| Protobuf | protobuf-cpp | protobuf-net / Google.Protobuf |
| MessagePack | msgpack-c | MessagePack-CSharp |
| FlatBuffers | flatbuffers-cpp | FlatBuffers C# 绑定 |
| AES 加密 | OpenSSL / Crypto++ | System.Security.Cryptography |
| HMAC | OpenSSL HMAC | System.Security.Cryptography.HMACSHA256 |
| 云存档 | 无标准方案 | UnityWebRequest |

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| MessagePack | 二进制 JSON，比 JSON 快 3 倍，比 Protobuf 小 |
| FlatBuffers | 零反序列化开销，直接读内存 |
| Protobuf oneof | 联合类型，同时只有一种值 |
| Protobuf Any | 通用容器，存任意消息类型 |
| BinaryFormatter | 危险！可能被远程代码执行 |
| AES 加密 | 对称加密标准，替代 XOR |
| HMAC 校验 | 防篡改，与加密配合使用 |
| 云存档 | 本地 + 云端 + 备份的三层策略 |
| 数据迁移 | 逐版本链式迁移，每个版本一个 migrator |
| 版本兼容 | 写版本号 → 按版本读取 → 迁移到最新 |

**对比 C++：** 序列化机制在所有语言中思路一致——"内存对象 ↔ 字节流"。C# 的序列化库在语法上更简洁（Attribute 声明替代手动编解码），但核心原理（自描述字段编号、varint 编码、偏移量表）是跨语言通用的。
