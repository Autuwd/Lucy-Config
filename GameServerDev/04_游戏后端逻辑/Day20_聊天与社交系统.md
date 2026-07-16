# Day 20：聊天与社交系统

## 一、聊天系统设计

### 聊天频道

```
世界频道     — 所有在线玩家都能看到
公会频道     — 同一公会的成员
队伍频道     — 同一队伍的成员
私聊         — 一对一
系统频道     — 服务器广播（不可手动发送）
```

### 频道管理实现

```csharp
public enum ChatChannel
{
    World = 0,
    Guild = 1,
    Team = 2,
    Whisper = 3,
    System = 4
}

public class ChatMessage
{
    public long FromPlayerId { get; set; }
    public string FromPlayerName { get; set; }
    public ChatChannel Channel { get; set; }
    public string Content { get; set; }
    public long ToPlayerId { get; set; } // 私聊目标
    public DateTime Timestamp { get; set; }
}

public class ChatService
{
    private readonly IDatabase _redis;
    private readonly ChatFilter _filter;
    private readonly RateLimiter _rateLimiter;

    // 发送聊天消息
    public async Task<ChatResult> SendMessage(ChatMessage msg)
    {
        // 1. 频限检查（世界频道 5 秒一条）
        if (!_rateLimiter.CheckLimit(msg.FromPlayerId, msg.Channel))
        {
            return ChatResult.Failed("发言过于频繁");
        }

        // 2. 敏感词过滤
        msg.Content = _filter.Filter(msg.Content);
        if (string.IsNullOrWhiteSpace(msg.Content))
        {
            return ChatResult.Failed("消息内容非法");
        }

        // 3. 根据频道分发
        switch (msg.Channel)
        {
            case ChatChannel.World:
                await BroadcastWorld(msg);
                break;

            case ChatChannel.Guild:
                await BroadcastGuild(msg);
                break;

            case ChatChannel.Team:
                await BroadcastTeam(msg);
                break;

            case ChatChannel.Whisper:
                await SendWhisper(msg);
                break;

            case ChatChannel.System:
                return ChatResult.Failed("玩家不能发送系统消息");
        }

        // 4. 记录聊天日志
        await LogChat(msg);

        return ChatResult.Success();
    }

    // 世界频道广播（通过 Redis Pub/Sub）
    private async Task BroadcastWorld(ChatMessage msg)
    {
        string json = JsonSerializer.Serialize(msg);

        // 发布到 Redis 频道
        await _redis.PublishAsync("chat:world", json);

        // 每个网关进程订阅了 "chat:world"
        // 收到后广播给所有连接到本网关的客户端
    }

    // 公会频道
    private async Task BroadcastGuild(ChatMessage msg)
    {
        // 从 Redis 获取公会在线成员列表
        var members = await _redis.SetMembersAsync($"guild_online:{msg.ToPlayerId}");

        string json = JsonSerializer.Serialize(msg);
        foreach (var memberId in members)
        {
            await RouteToPlayer(long.Parse(memberId), json);
        }
    }

    // 私聊
    private async Task SendWhisper(ChatMessage msg)
    {
        // 检查对方是否在线
        bool isOnline = await _redis.SetContainsAsync(
            "online:players", msg.ToPlayerId.ToString());

        if (!isOnline)
        {
            throw new BusinessException("对方不在线");
        }

        string json = JsonSerializer.Serialize(msg);

        // 发给目标
        await RouteToPlayer(msg.ToPlayerId, json);
        // 也发给自己（回显）
        await RouteToPlayer(msg.FromPlayerId, json);
    }

    // 路由到玩家（通过网关）
    private async Task RouteToPlayer(long playerId, string message)
    {
        // 通过 Redis Pub/Sub 发送到玩家绑定的网关
        await _redis.PublishAsync($"user:{playerId}", message);
    }
}
```

### Redis Pub/Sub 的局限性

```csharp
// Pub/Sub 的问题：
// 1. 消息不持久（消费者没连上就丢了）
// 2. 消费者断开后 catch-up 不了
// 3. 内存会积压（消费者慢了爆内存）

// 游戏服务器聊天场景：
// - 玩家不一定要收到离线时的聊天
// - 可以接受少量消息丢失
// Pub/Sub 足够用

// 如果要求可靠，用 Redis Stream 替代
```

---

## 二、敏感词过滤

### DFA 算法

```csharp
// DFA (Deterministic Finite Automaton)
// 将敏感词构建成 Trie 树，一次扫描检测所有敏感词

public class TrieNode
{
    public Dictionary<char, TrieNode> Children { get; } = new();
    public bool IsEnd { get; set; } // 是否敏感词结尾
    public string Word { get; set; } // 完整敏感词
}

public class ChatFilter
{
    private readonly TrieNode _root = new();

    // 构建敏感词树
    public void LoadWords(IEnumerable<string> sensitiveWords)
    {
        foreach (var word in sensitiveWords)
        {
            var current = _root;
            foreach (char c in word)
            {
                if (!current.Children.ContainsKey(c))
                    current.Children[c] = new TrieNode();
                current = current.Children[c];
            }
            current.IsEnd = true;
            current.Word = word;
        }
    }

    // 过滤（替换为 *）
    public string Filter(string text)
    {
        var chars = text.ToCharArray();
        int length = text.Length;

        for (int i = 0; i < length; i++)
        {
            var current = _root;

            for (int j = i; j < length; j++)
            {
                if (!current.Children.TryGetValue(char.ToLower(chars[j]), out current))
                    break;

                if (current.IsEnd)
                {
                    // 替换整段敏感词
                    for (int k = i; k <= j; k++)
                        chars[k] = '*';
                    i = j; // 跳到敏感词结尾
                    break;
                }
            }
        }

        return new string(chars);
    }
}
```

### 使用

```csharp
// 初始化（启动时加载）
var filter = new ChatFilter();
filter.LoadWords(new[]
{
    "敏感词1", "敏感词2",
    // 从数据库加载
});

// 实时检查
string cleaned = filter.Filter("靠靠靠这是个敏感词1测试");
// 输出: "****这是个***测试"
```

---

## 三、好友系统

```csharp
public class FriendService
{
    private readonly IDatabase _redis;
    private readonly DbConnection _db;

    // 发送好友申请
    public async Task<FriendResult> SendFriendRequest(long fromPlayerId, long toPlayerId)
    {
        // 1. 检查目标是否存在
        var target = await _db.QueryFirstOrDefaultAsync<PlayerData>(
            "SELECT id FROM players WHERE id = @Id", new { Id = toPlayerId });
        if (target == null)
            return FriendResult.Failed("玩家不存在");

        // 2. 检查是否已经是好友
        bool isFriend = await _db.QueryFirstAsync<int>(
            "SELECT COUNT(*) FROM friends WHERE player_id = @P AND friend_id = @F AND relationship = 0",
            new { P = fromPlayerId, F = toPlayerId }) > 0;
        if (isFriend)
            return FriendResult.Failed("已经是好友");

        // 3. 检查是否已经申请过
        bool hasRequested = await _db.QueryFirstAsync<int>(
            "SELECT COUNT(*) FROM friends WHERE player_id = @P AND friend_id = @F AND relationship = 2",
            new { P = fromPlayerId, F = toPlayerId }) > 0;
        if (hasRequested)
            return FriendResult.Failed("已发送过申请");

        // 4. 检查好友数量上限
        int friendCount = await _db.QueryFirstAsync<int>(
            "SELECT COUNT(*) FROM friends WHERE player_id = @Id AND relationship = 0",
            new { Id = fromPlayerId });
        if (friendCount >= _maxFriends)
            return FriendResult.Failed("好友已达上限");

        // 5. 插入申请记录
        await _db.ExecuteAsync(
            "INSERT INTO friends (player_id, friend_id, relationship) VALUES (@F, @T, 2)",
            new { F = fromPlayerId, T = toPlayerId });

        // 6. 通知对方
        if (await IsOnline(toPlayerId))
        {
            await NotifyNewFriendRequest(fromPlayerId, toPlayerId);
        }

        return FriendResult.Success();
    }

    // 同意好友申请
    public async Task<FriendResult> AcceptFriendRequest(long playerId, long requesterId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            // 更新申请记录为好友
            await conn.ExecuteAsync(
                "UPDATE friends SET relationship = 0 WHERE player_id = @R AND friend_id = @P AND relationship = 2",
                new { R = requesterId, P = playerId }, tx);

            // 插入反向关系
            await conn.ExecuteAsync(
                "INSERT INTO friends (player_id, friend_id, relationship) VALUES (@P, @R, 0)",
                new { P = playerId, R = requesterId }, tx);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        // 通知对方
        if (await IsOnline(requesterId))
        {
            await NotifyFriendAdded(playerId, requesterId);
        }

        return FriendResult.Success();
    }

    // 删除好友
    public async Task<FriendResult> RemoveFriend(long playerId, long friendId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            // 删除双向关系
            await conn.ExecuteAsync(
                "DELETE FROM friends WHERE (player_id = @P AND friend_id = @F) OR (player_id = @F AND friend_id = @P)",
                new { P = playerId, F = friendId }, tx);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return FriendResult.Success();
    }

    // 获取好友列表
    public async Task<List<FriendInfo>> GetFriendList(long playerId)
    {
        var friends = await _db.QueryAsync<FriendData>(
            "SELECT f.friend_id, f.intimacy, p.name, p.level, p.fight_power, p.last_login_at " +
            "FROM friends f JOIN players p ON f.friend_id = p.id " +
            "WHERE f.player_id = @Id AND f.relationship = 0",
            new { Id = playerId });

        var result = new List<FriendInfo>();
        foreach (var f in friends)
        {
            result.Add(new FriendInfo
            {
                PlayerId = f.friend_id,
                Name = f.name,
                Level = f.level,
                FightPower = f.fight_power,
                IsOnline = await IsOnline(f.friend_id),
                LastLoginAt = f.last_login_at
            });
        }

        return result;
    }

    // 检查是否在线（Redis 集合）
    private async Task<bool> IsOnline(long playerId)
    {
        return await _redis.SetContainsAsync("online:players", playerId.ToString());
    }
}

public class FriendInfo
{
    public long PlayerId { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public long FightPower { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
```

---

## 四、公会系统

```csharp
public class GuildService
{
    // 创建公会
    public async Task<GuildResult> CreateGuild(long founderId, string guildName)
    {
        // 1. 检查名字合法性
        if (guildName.Length < 2 || guildName.Length > 12)
            return GuildResult.Failed("公会名长度 2-12 字");

        // 2. 检查名字是否重复
        var existing = await _db.QueryFirstOrDefaultAsync<Guild>(
            "SELECT id FROM guilds WHERE name = @Name",
            new { Name = guildName });
        if (existing != null)
            return GuildResult.Failed("公会名已被使用");

        // 3. 检查创建者等级要求（30 级才能建公会）
        var founder = await GetPlayer(founderId);
        if (founder.Level < 30)
            return GuildResult.Failed("30 级才能创建公会");

        // 4. 检查是否已有公会
        if (founder.GuildId != null)
            return GuildResult.Failed("你已加入公会");

        // 5. 扣除创建消耗（500 钻石）
        // ...

        // 6. 创建公会
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            var guildId = await conn.QuerySingleAsync<long>(
                @"INSERT INTO guilds (name, owner_id, level, member_count)
                  VALUES (@Name, @Owner, 1, 1);
                  SELECT LAST_INSERT_ID();",
                new { Name = guildName, Owner = founderId }, tx);

            // 创建者成为会长
            await conn.ExecuteAsync(
                "UPDATE players SET guild_id = @GuildId, guild_role = 3 WHERE id = @PlayerId",
                new { GuildId = guildId, PlayerId = founderId }, tx);

            await tx.CommitAsync();

            return GuildResult.Success(guildId);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // 加入公会
    public async Task<GuildResult> JoinGuild(long playerId, long guildId)
    {
        var guild = await _db.QueryFirstOrDefaultAsync<Guild>(
            "SELECT * FROM guilds WHERE id = @Id", new { Id = guildId });
        if (guild == null)
            return GuildResult.Failed("公会不存在");

        if (guild.MemberCount >= guild.MaxMembers)
            return GuildResult.Failed("公会已满");

        var player = await GetPlayer(playerId);
        if (player.GuildId != null)
            return GuildResult.Failed("你已加入其他公会");

        await _db.ExecuteAsync(
            "UPDATE players SET guild_id = @GuildId, guild_role = 1 WHERE id = @PlayerId",
            new { GuildId = guildId, PlayerId = playerId });

        await _db.ExecuteAsync(
            "UPDATE guilds SET member_count = member_count + 1 WHERE id = @Id",
            new { Id = guildId });

        // 通知公会成员
        await BroadcastGuildChat(guildId, $"{player.Name} 加入了公会");

        return GuildResult.Success();
    }

    // 退出/踢出公会
    public async Task<GuildResult> LeaveGuild(long playerId)
    {
        var player = await GetPlayer(playerId);
        if (player.GuildId == null)
            return GuildResult.Failed("你未加入公会");

        if (player.GuildRole == GuildRole.Master)
        {
            // 会长不能退出，只能转让或解散
            return GuildResult.Failed("会长不能退出公会");
        }

        await _db.ExecuteAsync(
            "UPDATE players SET guild_id = NULL, guild_role = 0 WHERE id = @Id",
            new { Id = playerId });

        await _db.ExecuteAsync(
            "UPDATE guilds SET member_count = member_count - 1 WHERE id = @Id",
            new { Id = player.GuildId });

        return GuildResult.Success();
    }

    // 公会捐赠
    public async Task<bool> Donate(long playerId, int goldAmount)
    {
        var player = await GetPlayer(playerId);
        if (player.GuildId == null) return false;

        if (player.Gold < goldAmount) return false;

        // 扣金币
        player.Gold -= goldAmount;
        await UpdatePlayer(player);

        // 加公会资金
        await _db.ExecuteAsync(
            "UPDATE guilds SET funds = funds + @Amount WHERE id = @Id",
            new { Amount = goldAmount, Id = player.GuildId });

        // 加个人贡献
        await _db.ExecuteAsync(
            "UPDATE players SET guild_contribution = guild_contribution + @Amount WHERE id = @Id",
            new { Amount = goldAmount / 10, Id = playerId });

        return true;
    }

    // 公会技能（消耗贡献学习）
    public async Task<bool> LearnGuildSkill(long playerId, int skillId)
    {
        var guildSkillDef = GetGuildSkillDef(skillId);

        var playerData = await GetGuildMemberData(playerId);
        if (playerData.Contribution < guildSkillDef.CostContribution)
            return false;

        // 扣除贡献，习得技能
        await _db.ExecuteAsync(
            "UPDATE players SET guild_contribution = guild_contribution - @Cost WHERE id = @Id",
            new { Cost = guildSkillDef.CostContribution, Id = playerId });

        await _db.ExecuteAsync(
            "INSERT INTO player_guild_skills (player_id, skill_id, level) VALUES (@P, @S, 1) " +
            "ON DUPLICATE KEY UPDATE level = level + 1",
            new { P = playerId, S = skillId });

        return true;
    }
}

public enum GuildRole
{
    None = 0,
    Member = 1,      // 成员
    Officer = 2,     // 官员
    Master = 3       // 会长
}
```

---

## 五、消息限流

```csharp
public class RateLimiter
{
    private readonly IDatabase _redis;

    public bool CheckLimit(long playerId, ChatChannel channel)
    {
        string key = $"ratelimit:chat:{playerId}:{channel}";

        // 滑动窗口限流
        // 世界频道：5 秒 1 条
        // 公会频道：2 秒 1 条
        // 私聊：1 秒 2 条

        int maxCount = channel switch
        {
            ChatChannel.World => 1,
            ChatChannel.Guild => 1,
            ChatChannel.Whisper => 2,
            _ => 5
        };

        int windowSeconds = channel switch
        {
            ChatChannel.World => 5,
            ChatChannel.Guild => 2,
            ChatChannel.Whisper => 1,
            _ => 1
        };

        // 使用 Redis Sorted Set 实现滑动窗口
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long windowStart = now - windowSeconds * 1000;

        // 移除窗口外的时间戳
        _redis.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);

        // 统计窗口内的请求数
        long count = _redis.SortedSetLengthAsync(key).Result;

        if (count >= maxCount)
            return false;

        // 添加当前请求
        _redis.SortedSetAddAsync(key, Guid.NewGuid().ToString(), now);
        _redis.KeyExpireAsync(key, TimeSpan.FromSeconds(windowSeconds + 1));

        return true;
    }
}
```

---

## 六、练习

1. **聊天系统**：实现世界频道 + 私聊的完整流程
2. **敏感词过滤**：实现 DFA 算法的敏感词过滤
3. **好友系统**：实现好友添加/删除/列表功能
4. **公会系统**：实现公会创建/加入/退出
5. **滑动窗口限流**：实现 Redis 滑动窗口限流器

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 聊天频道 | 世界/公会/队伍/私聊各有不同的广播范围 |
| Pub/Sub | Redis 的聊天消息分发机制 |
| DFA 过滤 | Trie 树一次扫描替换所有敏感词 |
| 好友关系 | 双向关系，MySQL 存持久数据 |
| 公会 | 强社交系统，会长/官员/成员三层权限 |
| 滑动窗口 | Redis ZSet 实现的时间窗口限流 |
