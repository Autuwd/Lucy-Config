# Day 19：数据持久化与序列化 — 从内存到磁盘

## 0. 为什么需要数据持久化？

游戏数据在内存中是对象、结构体、列表。但内存是**易失的**——关掉程序数据就没了。

```
内存中的对象：              磁盘上的文件：
Player {                   存档.sav → 二进制字节
  hp: 100    ──序列化──▶    [01 64 00 00 00 ...]
  name: "Lucy"             存档.json → 文本字符串
  gold: 500                {"hp":100,"name":"Lucy","gold":500}
}              ◀──反序列化──
```

**序列化（Serialization）** = 内存对象 → 字节/文本
**反序列化（Deserialization）** = 字节/文本 → 内存对象

序列化有三个关键指标：
1. **速度**：游戏不能因为存档而卡顿
2. **大小**：存档文件越小越好（玩家要上传下载）
3. **兼容性**：游戏更新后，旧存档还能读

在 C++/Raylib 中你可能用结构体直接写二进制文件，C# 和 Unity 提供了更多选择。

---

## 1. 序列化格式对比

| 格式 | 可读性 | 大小 | 速度 | 跨平台 | Unity 支持 |
|------|--------|------|------|--------|-----------|
| JSON | ★★★★★ | ★★ | ★★★ | ★★★★★ | JsonUtility / Newtonsoft.Json |
| XML | ★★★★ | ★ | ★★ | ★★★★★ | XmlSerializer |
| 二进制 | ☆ | ★★★★★ | ★★★★★ | ★★★ | BinaryFormatter（已弃用）/ 自定义 |
| Protobuf | ☆ | ★★★★★ | ★★★★★ | ★★★★★ | protobuf-net |
| MessagePack | ★★ | ★★★★ | ★★★★ | ★★★★ | MessagePack-CSharp |

### 选择策略

```
存档文件（给玩家看）   → JSON（人类可读、易修改）
配置文件              → JSON / ScriptableObject
网络传输              → Protobuf / MessagePack（小、快）
性能敏感（大量对象）   → 自定义二进制
```

---

## 2. JSON — 最常用的文本格式

### JsonUtility（Unity 内置）

```csharp
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public int level;
    public float hp;
    public ItemData[] inventory;
}

[System.Serializable]
public class ItemData
{
    public int id;
    public int count;
}

public class SaveManager : MonoBehaviour
{
    // 序列化：对象 → JSON 字符串
    public string SerializePlayer(PlayerData data)
    {
        return JsonUtility.ToJson(data, prettyPrint: true);
    }

    // 反序列化：JSON 字符串 → 对象
    public PlayerData DeserializePlayer(string json)
    {
        return JsonUtility.FromJson<PlayerData>(json);
    }

    // 存档到文件
    public void SaveToFile(PlayerData data)
    {
        string json = SerializePlayer(data);
        string path = Application.persistentDataPath + "/save.json";
        System.IO.File.WriteAllText(path, json);
        Debug.Log($"存档保存到：{path}");
    }

    // 从文件读档
    public PlayerData LoadFromFile()
    {
        string path = Application.persistentDataPath + "/save.json";
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            return DeserializePlayer(json);
        }
        return null;
    }
}
```

### JsonUtility 的局限

```csharp
// ❌ 不支持：Dictionary
[System.Serializable]
public class BadData
{
    public Dictionary<string, int> scores; // JsonUtility 序列化不了！
}

// ❌ 不支持：继承多态
[System.Serializable]
public class Weapon { }
[System.Serializable]
public class Sword : Weapon { }
// JsonUtility 只序列化基类字段，丢失子类数据

// ❌ 不支持：属性（Property），只认字段（Field）
public class PropertyData
{
    public int Value { get; set; } // JSON 会丢失这个！
}

// ✅ 替代方案：使用 Newtonsoft.Json（第三方）
// Package Manager → 搜索 Newtonsoft.Json
```

### Newtonsoft.Json 用法

```csharp
using Newtonsoft.Json;

public class PlayerData
{
    [JsonProperty("name")] // JSON 中的字段名
    public string PlayerName { get; set; }

    [JsonIgnore] // 不序列化这个字段
    public string Password { get; set; }

    [JsonConverter(typeof(DictionaryConverter))] // 自定义转换
    public Dictionary<string, int> Scores { get; set; }
}

// 使用
var data = new PlayerData { PlayerName = "Lucy", Scores = new() {{"Level1", 100}} };
string json = JsonConvert.SerializeObject(data, Formatting.Indented);
var loaded = JsonConvert.DeserializeObject<PlayerData>(json);
```

---

## 3. 二进制序列化 — 性能最优

### 自定义二进制（手动控制）

```csharp
using System.IO;

public struct SaveData
{
    public int version;
    public float playerPosX;
    public float playerPosY;
    public int hp;
    public int gold;
}

public class BinarySaveManager
{
    public void Save(string path, SaveData data)
    {
        using FileStream fs = new FileStream(path, FileMode.Create);
        using BinaryWriter writer = new BinaryWriter(fs);

        writer.Write(data.version);
        writer.Write(data.playerPosX);
        writer.Write(data.playerPosY);
        writer.Write(data.hp);
        writer.Write(data.gold);
    }

    public SaveData Load(string path)
    {
        using FileStream fs = new FileStream(path, FileMode.Open);
        using BinaryReader reader = new BinaryReader(fs);

        SaveData data;
        data.version = reader.ReadInt32();
        data.playerPosX = reader.ReadSingle();
        data.playerPosY = reader.ReadSingle();
        data.hp = reader.ReadInt32();
        data.gold = reader.ReadInt32();
        return data;
    }
}
```

### 版本兼容实战

游戏更新后，SaveData 增加了字段。旧的存档只有 5 个字段，新版代码试图读 6 个字段——**直接崩溃**。

```csharp
// 初始版本：v1
// SaveData: posX, posY, hp, gold

// 更新版本：v2
// SaveData: posX, posY, hp, gold, maxHp

// 更新版本：v3
// SaveData: posX, posY, hp, gold, maxHp, exp, sceneName(string)

// 版本兼容策略
public class VersionedSaveManager
{
    private const int CURRENT_VERSION = 3;

    public void Save(string path, GameSaveData data)
    {
        using FileStream fs = new FileStream(path, FileMode.Create);
        using BinaryWriter writer = new BinaryWriter(fs);

        // 1. 写版本号（始终在最前面）
        writer.Write(CURRENT_VERSION);

        // 2. 写数据
        writer.Write(data.posX);
        writer.Write(data.posY);
        writer.Write(data.hp);
        writer.Write(data.gold);
        writer.Write(data.maxHp);      // v2 新增
        writer.Write(data.exp);        // v3 新增
        writer.Write(data.sceneName);  // v3 新增
    }

    public GameSaveData Load(string path)
    {
        if (!File.Exists(path))
            return CreateDefaultData();

        using FileStream fs = new FileStream(path, FileMode.Open);
        using BinaryReader reader = new BinaryReader(fs);

        // 1. 先读版本号
        int version = reader.ReadInt32();

        // 2. 根据版本号读取对应格式
        GameSaveData data = new GameSaveData();

        // 所有版本都有的字段
        data.posX = reader.ReadSingle();
        data.posY = reader.ReadSingle();
        data.hp = reader.ReadInt32();
        data.gold = reader.ReadInt32();

        // v2+ 才有的字段
        if (version >= 2)
            data.maxHp = reader.ReadInt32();
        else
            data.maxHp = data.hp; // 旧存档：maxHp 默认等于当前 hp

        // v3+ 才有的字段
        if (version >= 3)
        {
            data.exp = reader.ReadInt32();
            data.sceneName = reader.ReadString();
        }
        else
        {
            data.exp = 0;
            data.sceneName = "Level1"; // 旧存档：默认回到第一关
        }

        return data;
    }

    private GameSaveData CreateDefaultData()
    {
        return new GameSaveData
        {
            version = CURRENT_VERSION,
            posX = 0, posY = 0,
            hp = 100, gold = 0,
            maxHp = 100, exp = 0,
            sceneName = "Level1"
        };
    }
}
```

### 二进制文件结构

```
完整文件布局：
┌────────────────────────────────┐
│ 版本号 (4 bytes)               │ ← 总是第一位，决定后续格式
├────────────────────────────────┤
│ posX (4 bytes)                 │
│ posY (4 bytes)                 │
│ hp (4 bytes)                   │
│ gold (4 bytes)                 │
├────────────────────────────────┤
│ maxHp (4 bytes) [v2+]         │ ← 版本升级新增
├────────────────────────────────┤
│ exp (4 bytes) [v3+]           │
│ sceneName 长度 (4 bytes)       │
│ sceneName 字符 (N bytes)       │
└────────────────────────────────┘
```

---

## 4. 存档加密

玩家的存档不要明文存——他们能改 JSON 里的金币数量。

### 简单 XOR 加密

```csharp
public static class SaveEncryption
{
    private static readonly byte[] KEY = { 0xA3, 0x7F, 0x1C, 0x5B, 0x9D, 0x2E, 0x45, 0x80 };

    public static byte[] Encrypt(byte[] data)
    {
        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
            result[i] = (byte)(data[i] ^ KEY[i % KEY.Length]);
        return result;
    }

    public static byte[] Decrypt(byte[] data)
    {
        // XOR 加密的对称性：两次 XOR 回到原文
        return Encrypt(data);
    }
}
```

### 防止修改（HMAC 校验）

```csharp
using System.Security.Cryptography;

public static class SaveIntegrity
{
    // 用 HMAC 验证存档是否被篡改
    public static byte[] Sign(byte[] data, string password)
    {
        using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(password));
        byte[] hash = hmac.ComputeHash(data);

        // 文件格式：[数据][哈希(32 bytes)]
        byte[] signed = new byte[data.Length + 32];
        data.CopyTo(signed, 0);
        hash.CopyTo(signed, data.Length);
        return signed;
    }

    public static bool Verify(byte[] signedData, string password)
    {
        if (signedData.Length < 32) return false;

        byte[] data = signedData[..^32];
        byte[] storedHash = signedData[^32..];

        using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(password));
        byte[] computedHash = hmac.ComputeHash(data);

        // 比较哈希
        for (int i = 0; i < 32; i++)
            if (storedHash[i] != computedHash[i])
                return false;
        return true;
    }
}

// 完整流程
public class SecureSaveManager
{
    private string password = "player_save_key_123";

    public void SaveGame(string path, object data)
    {
        string json = JsonConvert.SerializeObject(data);
        byte[] raw = System.Text.Encoding.UTF8.GetBytes(json);
        byte[] encrypted = SaveEncryption.Encrypt(raw);
        byte[] signed = SaveIntegrity.Sign(encrypted, password);
        File.WriteAllBytes(path, signed);
    }

    public T LoadGame<T>(string path) where T : class
    {
        byte[] signed = File.ReadAllBytes(path);

        if (!SaveIntegrity.Verify(signed, password))
        {
            Debug.LogError("存档被篡改！");
            return null;
        }

        byte[] encrypted = signed[..^32];
        byte[] raw = SaveEncryption.Decrypt(encrypted);
        string json = System.Text.Encoding.UTF8.GetString(raw);
        return JsonConvert.DeserializeObject<T>(json);
    }
}
```

---

## 5. PlayerPrefs — 简单键值存储

适用于**少量设置**（音量、分辨率、玩家 ID），不适合大量数据。

```csharp
// 写
PlayerPrefs.SetInt("HighScore", 1000);
PlayerPrefs.SetFloat("Volume", 0.8f);
PlayerPrefs.SetString("PlayerName", "Lucy");
PlayerPrefs.Save(); // 立即写入磁盘

// 读
int highScore = PlayerPrefs.GetInt("HighScore", 0); // 默认值 0
float volume = PlayerPrefs.GetFloat("Volume", 1f);
string name = PlayerPrefs.GetString("PlayerName", "Unknown");

// 删
PlayerPrefs.DeleteKey("HighScore");
PlayerPrefs.DeleteAll();
```

### PlayerPrefs 存储位置

```
Windows: HKCU\Software\公司名\产品名  （注册表）
macOS:   ~/Library/Preferences/公司名.产品名.plist
Android: /data/data/包名/shared_prefs/
iOS:     NSUserDefaults
```

---

## 6. ScriptableObject — Unity 的配置数据方案

适用于**游戏配置数据**（道具表、技能表、关卡配置），在编辑器中编辑，运行时直接引用。

```csharp
// 创建 ScriptableObject
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item")]
public class ItemConfig : ScriptableObject
{
    public int itemId;
    public string itemName;
    public string description;
    public Sprite icon;
    public int maxStack;
    public int buyPrice;
    public int sellPrice;
}

// 使用
public class ItemDatabase : MonoBehaviour
{
    public ItemConfig[] allItems; // 在 Inspector 中拖入

    private Dictionary<int, ItemConfig> itemMap;

    void Awake()
    {
        itemMap = new();
        foreach (var item in allItems)
            itemMap[item.itemId] = item;
    }

    public ItemConfig GetItemById(int id)
        => itemMap.TryGetValue(id, out var item) ? item : null;
}
```

### Excel → ScriptableObject 管线

```
实际项目中，配置通常从 Excel 导出：

开发流程：
1. 策划在 Excel 里写配置
2. 运行编辑器工具
3. 工具读取 Excel → 生成 .asset 文件（ScriptableObject）
4. 程序直接引用 .asset 文件

Excel 格式示例（Items.xlsx）：
| ID | Name  | MaxStack | BuyPrice |
|----|-------|----------|----------|
| 1  | 生命药水 | 99       | 100      |
| 2  | 魔法药水 | 99       | 150      |
```

---

## 7. Protocol Buffers — 网络传输首选

### 为什么用 Protobuf？

```
JSON 一个玩家对象：
{"playerName":"Lucy","level":10,"hp":100}  ← 32 字节文本

Protobuf 同样的数据：
0A 04 4C 75 63 79 10 0A 18 64              ← 10 字节二进制

为什么更小？
- JSON 存了字段名 "playerName"（10 字节）
- Protobuf 只存字段编号 1（1 字节）
- JSON 的每个值都是文本 "100"（3 字节）
- Protobuf 的 100 是二进制 varint（1 字节）
```

### 定义 .proto 文件

```protobuf
syntax = "proto3";

message PlayerData {
    string player_name = 1;  // 1 = 字段编号（不是值）
    int32 level = 2;
    float hp = 3;
    repeated int32 item_ids = 4;  // 数组
    map<string, int32> stats = 5; // 字典
}
```

### C# 使用（通过 protobuf-net 库）

```csharp
using ProtoBuf;

[ProtoContract]
public class PlayerData
{
    [ProtoMember(1)]
    public string PlayerName { get; set; }
    [ProtoMember(2)]
    public int Level { get; set; }
    [ProtoMember(3)]
    public float Hp { get; set; }
    [ProtoMember(4)]
    public int[] ItemIds { get; set; }
    [ProtoMember(5)]
    public Dictionary<string, int> Stats { get; set; }
}

// 序列化
using MemoryStream ms = new();
Serializer.Serialize(ms, playerData);
byte[] bytes = ms.ToArray();

// 反序列化
using MemoryStream ms = new(receivedBytes);
var player = Serializer.Deserialize<PlayerData>(ms);
```

### Protobuf 的版本兼容

```protobuf
// 旧版本：只有 3 个字段
message PlayerData {
    string player_name = 1;
    int32 level = 2;
    float hp = 3;
}

// 新版本：加了 2 个字段
message PlayerData {
    string player_name = 1;
    int32 level = 2;
    float hp = 3;
    int32 max_hp = 4;     // 新增
    string guild = 5;     // 新增
}

// 关键规则：
// 1. 永远不要修改已有字段的编号
// 2. 新增字段用新的编号
// 3. 删除字段时保留编号（用 reserved）
// 4. 旧代码读新数据：不认识的新字段直接跳过
// 5. 新代码读旧数据：没有的字段用默认值（int=0, string=""）
```

---

## 8. 练习

### 练习 1：JSON 存档系统

```csharp
// 实现一个完整的存档系统：
// 数据结构：玩家名、等级、经验值、金币、背包（物品 ID 列表）
// 功能：
// 1. 保存到 Application.persistentDataPath
// 2. 加载存档
// 3. 删除存档
// 4. 判断存档是否存在
// 5. 自动存档（每 30 秒 / 关键事件时）
```

### 练习 2：二进制版本兼容

```csharp
// 设计一个支持版本迁移的二进制存档格式：
// v1: level (int), hp (float), gold (int)
// v2: + maxHp (float)
// v3: + weaponId (int)
// v4: + skills (int[]) 
//
// 要求：
// 1. 每个版本都能正确读写
// 2. 旧版本读新存档时，读取能用的字段
// 3. 新版本读旧存档时，填充默认值
```

### 练习 3：加密存档

```csharp
// 基于练习 1 的 JSON 存档，添加：
// 1. XOR 加密
// 2. HMAC 完整性校验
// 3. 验证：修改存档文件后，游戏应能检测到篡改
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 序列化 | 内存对象 → 磁盘/网络字节 |
| JSON | 可读性好，适合存档和配置 |
| 二进制 | 最快最小，适合性能敏感场景 |
| PlayerPrefs | 简单键值存储，存设置 |
| ScriptableObject | Unity 的配置数据方案 |
| Protobuf | 网络传输的首选序列化格式 |
| 版本兼容 | 文件头写版本号，按版本读取 |
| 存档加密 | XOR + HMAC，防篡改 |

**对比 C++/Raylib：** 在 Raylib 中你可能用 `fwrite(&data, sizeof(Data), 1, file)` 直接写二进制。C# 提供了 JsonUtility 等高级封装，但底层原理不变——"把内存布局转换成字节流"。Protobuf 比 C++ 手写的二进制序列化多了一层字段编号机制，但提供了自动的向前/向后兼容。
