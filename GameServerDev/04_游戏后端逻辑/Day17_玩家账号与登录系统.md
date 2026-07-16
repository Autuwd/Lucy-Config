# Day 17：玩家账号与登录系统

## 一、登录流程

### 标准登录流程

```
Client                  LoginServer               GameServer          DB
  │                         │                        │                │
  │── 登录请求 ────────────→│                        │                │
  │  (account, password)    │                        │                │
  │                         │── 验证 ───────────────→│                │
  │                         │  (account, password)    │── 查询 ──────→│
  │                         │                        │←── 结果 ──────│
  │                         │←── Token ──────────────│                │
  │←── Token + ServerList ─│                        │                │
  │                         │                        │                │
  │── SelectServer ────────→│ (选择服务器)           │                │
  │  (token, serverId)      │── VerifyToken ───────→│                │
  │                         │                        │── LoadPlayer ─→│
  │                         │                        │←── PlayerData ─│
  │←── EnterGame ──────────│←── Session ───────────│                │
```

### 寄存器

```csharp
public class RegisterRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }
}

public class RegisterResponse
{
    public int ErrorCode { get; set; } // 0=成功
    public string Message { get; set; }
}

// 注册实现
public async Task<RegisterResponse> Register(RegisterRequest req)
{
    // 1. 检查用户名合法性
    if (req.Username.Length < 3 || req.Username.Length > 32)
        return new RegisterResponse { ErrorCode = 1001, Message = "用户名长度 3-32" };

    // 2. 检查用户名是否已存在
    var existing = await _db.QueryAsync<Account>(
        "SELECT id FROM accounts WHERE username = @Username",
        new { req.Username });

    if (existing.Any())
        return new RegisterResponse { ErrorCode = 1002, Message = "用户名已存在" };

    // 3. 密码加盐哈希
    string salt = GenerateSalt();
    string passwordHash = HashPassword(req.Password, salt);

    // 4. 创建账号
    await _db.ExecuteAsync(
        "INSERT INTO accounts (username, password_hash, salt) VALUES (@U, @H, @S)",
        new { U = req.Username, H = passwordHash, S = salt });

    return new RegisterResponse { ErrorCode = 0, Message = "注册成功" };
}

private string GenerateSalt()
{
    byte[] salt = new byte[16];
    RandomNumberGenerator.Fill(salt);
    return Convert.ToHexString(salt);
}

private string HashPassword(string password, string salt)
{
    using var pbkdf2 = new Rfc2898DeriveBytes(
        password,
        Encoding.UTF8.GetBytes(salt),
        100000,  // 迭代次数，越高越安全
        HashAlgorithmName.SHA256);
    return Convert.ToHexString(pbkdf2.GetBytes(32));
}
```

### 登录实现

```csharp
public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string ClientVersion { get; set; }
    public string DeviceId { get; set; }
}

public class LoginResponse
{
    public int ErrorCode { get; set; }
    public string Token { get; set; }
    public long ExpireTime { get; set; }
    public List<ServerInfo> Servers { get; set; }
}

public class ServerInfo
{
    public int ServerId { get; set; }
    public string Name { get; set; }
    public int Status { get; set; } // 0流畅 1繁忙 2维护
    public int PlayerCount { get; set; }
}

public async Task<LoginResponse> Login(LoginRequest req)
{
    // 1. 客户端版本检查
    if (req.ClientVersion != _expectedVersion)
        return new LoginResponse { ErrorCode = 2001, Message = "客户端版本过低" };

    // 2. 查询账号
    var account = await _db.QueryFirstOrDefaultAsync<Account>(
        "SELECT * FROM accounts WHERE username = @Username",
        new { req.Username });

    if (account == null)
        return new LoginResponse { ErrorCode = 2002, Message = "账号不存在" };

    // 3. 验证密码
    string hash = HashPassword(req.Password, account.Salt);
    if (hash != account.PasswordHash)
        return new LoginResponse { ErrorCode = 2003, Message = "密码错误" };

    // 4. 检查账号状态
    if (account.Status != AccountStatus.Normal)
        return new LoginResponse { ErrorCode = 2004, Message = "账号已被封禁" };

    // 5. 生成 Token
    string token = GenerateToken(account.Id);
    long expireTime = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds();

    // 将 Token 存入 Redis
    await _redis.StringSetAsync(
        $"token:{token}",
        account.Id.ToString(),
        TimeSpan.FromHours(2));

    // 6. 获取服务器列表
    var servers = await GetServerList();

    return new LoginResponse
    {
        ErrorCode = 0,
        Token = token,
        ExpireTime = expireTime,
        Servers = servers
    };
}

private string GenerateToken(long accountId)
{
    // 格式: base64(accountId + '.' + random + '.' + expire)
    long expire = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds();
    long random = Random.Shared.NextInt64();

    string raw = $"{accountId}.{random}.{expire}";
    string signature = ComputeHmac(raw, _secretKey);
    return $"{raw}.{signature}";
}

private string ComputeHmac(string data, string key)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
    byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    return Convert.ToHexString(hash).ToLower();
}
```

---

## 二、Token 验证

```csharp
class TokenValidator
{
    private readonly IDatabase _redis;
    private readonly string _secretKey;

    // 验证 Token（每次客户端连接时调用）
    public async Task<long?> ValidateToken(string token)
    {
        // 1. 验证签名
        string[] parts = token.Split('.');
        if (parts.Length != 4) return null;

        string data = $"{parts[0]}.{parts[1]}.{parts[2]}";
        string expectedSig = ComputeHmac(data, _secretKey);

        if (parts[3] != expectedSig) return null;

        // 2. 检查过期
        long expire = long.Parse(parts[2]);
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expire)
            return null;

        // 3. 检查 Redis 中的 Token 是否有效
        string tokenKey = $"token:{token}";
        var accountIdStr = await _redis.StringGetAsync(tokenKey);
        if (!accountIdStr.HasValue) return null;

        // 4. 延长过期时间（续期）
        await _redis.KeyExpireAsync(tokenKey, TimeSpan.FromHours(2));

        return long.Parse(accountIdStr);
    }

    // 踢掉指定账号的所有连接（禁止多开时用）
    public async Task KickAccount(long accountId)
    {
        // 加入黑名单（当前 Token 无效化）
        await _redis.StringSetAsync(
            $"kick:{accountId}",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            TimeSpan.FromHours(2));
    }
}
```

---

## 三、角色管理

### 角色创建

```csharp
public class CreateRoleRequest
{
    public string Name { get; set; }
    public int ClassId { get; set; } // 职业
    public int Gender { get; set; }  // 性别
}

public async Task<int> CreateRole(long accountId, int serverId, CreateRoleRequest req)
{
    // 1. 检查是否已达上限
    var count = await _db.QueryFirstAsync<int>(
        "SELECT COUNT(*) FROM players WHERE account_id = @A AND server_id = @S",
        new { A = accountId, S = serverId });

    if (count >= _maxRolesPerServer)
        throw new BusinessException("角色已达上限");

    // 2. 检查名字是否重复
    var existing = await _db.QueryFirstOrDefaultAsync<PlayerData>(
        "SELECT id FROM players WHERE name = @N AND server_id = @S",
        new { N = req.Name, S = serverId });

    if (existing != null)
        throw new BusinessException("名字已被使用");

    // 3. 创建角色
    var player = new PlayerData
    {
        AccountId = accountId,
        ServerId = serverId,
        Name = req.Name,
        ClassId = req.ClassId,
        Gender = req.Gender,
        Level = 1,
        Exp = 0,
        Gold = _initialGold,
        Status = PlayerStatus.Normal
    };

    // 事务创建角色和相关初始化
    using var conn = new MySqlConnection(_connectionString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    try
    {
        // 插入角色
        var sql = @"INSERT INTO players (account_id, server_id, name, class_id, gender, level, exp, gold)
                     VALUES (@AccountId, @ServerId, @Name, @ClassId, @Gender, @Level, @Exp, @Gold);
                     SELECT LAST_INSERT_ID();";
        player.Id = await conn.QuerySingleAsync<int>(sql, player, tx);

        // 创建初始背包（空）
        // 创建初始技能列表
        // 初始化新手任务

        await tx.CommitAsync();
        return player.Id;
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
}
```

### 角色选择

```csharp
// 选择角色进入游戏
public class SelectRoleRequest
{
    public string Token { get; set; }
    public int ServerId { get; set; }
    public long PlayerId { get; set; }
}

public async Task<EnterGameResponse> SelectRole(SelectRoleRequest req)
{
    // 1. 验证 Token
    long? accountId = await _tokenValidator.ValidateToken(req.Token);
    if (accountId == null)
        throw new AuthException("Token 无效");

    // 2. 验证角色属于该账号
    var player = await _db.QueryFirstOrDefaultAsync<PlayerData>(
        "SELECT * FROM players WHERE id = @Id AND account_id = @A",
        new { Id = req.PlayerId, A = accountId.Value });

    if (player == null)
        throw new BusinessException("角色不存在");

    // 3. 检查是否已经在线（踢掉旧连接）
    var onlineManager = GetOnlineManager();
    if (await onlineManager.IsOnline(req.PlayerId))
    {
        await onlineManager.KickPlayer(req.PlayerId, "新登录顶号");
    }

    // 4. 加载完整玩家数据到缓存
    await LoadPlayerToCache(player.Id);

    // 5. 记录登录
    await onlineManager.OnPlayerLogin(player.Id);
    await _db.ExecuteAsync(
        "UPDATE players SET last_login_at = NOW() WHERE id = @Id",
        new { Id = player.Id });

    // 6. 返回进入游戏所需数据
    return new EnterGameResponse
    {
        Player = player,
        ServerTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        SceneId = player.LastSceneId,
        Position = player.LastPosition
    };
}
```

---

## 四、玩家数据缓存

```csharp
class PlayerCache
{
    private readonly IDatabase _redis;

    // 玩家数据加载到 Redis
    public async Task LoadToCache(long playerId)
    {
        var batch = _redis.CreateBatch();

        // 加载玩家基本信息
        var player = await _db.QueryFirstAsync<PlayerData>(
            "SELECT * FROM players WHERE id = @Id", new { Id = playerId });

        var playerHash = new HashEntry[]
        {
            new("name", player.Name),
            new("level", player.Level),
            new("exp", player.Exp),
            new("gold", player.Gold),
            new("diamond", player.Diamond),
            new("fight_power", player.FightPower),
            new("scene_id", player.LastSceneId),
        };
        batch.HashSetAsync($"player:{playerId}", playerHash);
        batch.KeyExpireAsync($"player:{playerId}", TimeSpan.FromMinutes(30));

        // 加载背包
        var items = await _db.QueryAsync<InventoryItem>(
            "SELECT * FROM inventory WHERE player_id = @Id",
            new { Id = playerId });

        foreach (var item in items)
        {
            batch.HashSetAsync($"inventory:{playerId}", item.ItemId.ToString(),
                JsonSerializer.Serialize(item));
        }
        batch.KeyExpireAsync($"inventory:{playerId}", TimeSpan.FromMinutes(30));

        // 加载任务进度
        var quests = await _db.QueryAsync<QuestData>(
            "SELECT * FROM quests WHERE player_id = @Id AND status != 3",
            new { Id = playerId });

        foreach (var quest in quests)
        {
            batch.HashSetAsync($"quests:{playerId}", quest.QuestId.ToString(),
                JsonSerializer.Serialize(quest));
        }

        batch.Execute();
    }

    // 卸载玩家缓存（下线时）
    public async Task UnloadFromCache(long playerId)
    {
        // 1. 将内存中修改的数据写回 DB
        await FlushDirtyData(playerId);

        // 2. 删除缓存
        var batch = _redis.CreateBatch();
        batch.KeyDeleteAsync($"player:{playerId}");
        batch.KeyDeleteAsync($"inventory:{playerId}");
        batch.KeyDeleteAsync($"quests:{playerId}");
        batch.KeyDeleteAsync($"position:{playerId}");
        batch.Execute();
    }

    // 定时写回脏数据
    private readonly ConcurrentDictionary<long, PlayerDirtyData> _dirtyPlayers = new();

    public void MarkDirty(long playerId, string field)
    {
        _dirtyPlayers.AddOrUpdate(playerId,
            _ => new PlayerDirtyData { Fields = { field } },
            (_, data) => { data.Fields.Add(field); return data; });
    }

    public async Task FlushAllDirty()
    {
        foreach (var (playerId, dirty) in _dirtyPlayers)
        {
            if (dirty.Fields.Count == 0) continue;

            // 更新 DB
            foreach (var field in dirty.Fields)
            {
                var value = await _redis.HashGetAsync($"player:{playerId}", field);
                if (value.HasValue)
                {
                    await _db.ExecuteAsync(
                        $"UPDATE players SET {field} = @Value WHERE id = @Id",
                        new { Value = value.ToString(), Id = playerId });
                }
            }

            dirty.Fields.Clear();
        }
    }
}

class PlayerDirtyData
{
    public HashSet<string> Fields { get; set; } = new();
}
```

---

## 五、防沉迷

```csharp
// 中国游戏防沉迷要求
class AntiAddictionService
{
    private readonly IDatabase _redis;

    // 检查登录时间限制
    public async Task<bool> CanLogin(long playerId)
    {
        // 获取实名信息
        var realName = await GetRealNameInfo(playerId);
        if (realName == null)
        {
            // 未实名 → 限制
            return false;
        }

        if (realName.IsMinor)
        {
            // 未成年人
            // 周五/六/日 20:00-21:00 可玩 1 小时
            // 法定节假日
            var now = DateTime.Now;
            if (!IsAllowedTime(now))
                return false;

            // 检查已玩时长
            string playTimeKey = $"playtime:{playerId}:{now:yyyyMMdd}";
            long playedMinutes = await _redis.StringIncrementAsync(playTimeKey);

            if (playedMinutes > 60)
                return false; // 已超 1 小时

            return true;
        }

        // 成年人无限制
        return true;
    }

    // 单日累计时长
    public async Task<int> GetPlayedMinutes(long playerId)
    {
        string key = $"playtime:{playerId}:{DateTime.Now:yyyyMMdd}";
        var value = await _redis.StringGetAsync(key);
        return value.HasValue ? int.Parse(value) : 0;
    }

    // 充值限制
    public async Task<bool> CanRecharge(long playerId, decimal amount)
    {
        var realName = await GetRealNameInfo(playerId);
        if (realName == null) return false;

        if (realName.IsMinor)
        {
            // 8-16 岁：单次 ≤ 50 元，月累计 ≤ 200 元
            // 16-18 岁：单次 ≤ 100 元，月累计 ≤ 400 元
            string monthKey = $"recharge:{playerId}:{DateTime.Now:yyyyMM}";
            decimal monthlyTotal = await GetMonthlyRecharge(monthKey);

            int maxSingle = realName.Age < 16 ? 50 : 100;
            int maxMonthly = realName.Age < 16 ? 200 : 400;

            if (amount > maxSingle)
                return false;
            if (monthlyTotal + amount > maxMonthly)
                return false;
        }

        return true;
    }
}
```

---

## 六、练习

1. **注册流程**：实现完整的注册 + 明文密码加密流程
2. **Token 验证**：实现 JWT 风格的 Token 生成和验证
3. **角色创建**：实现角色创建的事务（至少 3 张表同时写入）
4. **数据缓存**：实现 Cache-Aside 模式的玩家缓存
5. **防沉迷**：实现未成年人时长限制检查

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 密码安全 | 加盐哈希（PBKDF2/bcrypt），绝不存明文 |
| Token | 无状态认证，HMAC 签名防篡改，Redis 管理有效期 |
| 角色创建 | 事务保证多表一致性（角色+背包+技能） |
| 数据缓存 | Redis 缓存热数据，定期写回 DB |
| 脏数据标记 | 只写回修改过的字段，减少 DB 写入量 |
| 防沉迷 | 未成年人时长+时段+充值三重限制 |
