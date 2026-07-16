# Day 20：聊天与社交系统 — 进阶深入

## 一、聊天消息存储选型

### 关系型 vs NoSQL vs 时序数据库

```
聊天消息存储的选择取决于查询模式：

+-------------------+------------------+------------------+------------------+
|                   | MySQL (InnoDB)   | MongoDB          | ClickHouse       |
+-------------------+------------------+------------------+------------------+
| 适合场景          | 小规模(<5万条/天) | 中等规模         | 大规模日志分析   |
| 写入速度          | 1000条/s         | 10000条/s        | 100000条/s       |
| 按玩家查聊天记录  | 支持（索引）     | 支持             | 不擅长           |
| 敏感词审计查询    | 支持但慢         | 一般             | 极快             |
| 数据过期删除      | DELETE 慢        | TTL 自动过期     | 分区删除极快     |
| 运维复杂度        | 低               | 中               | 高               |
+-------------------+------------------+------------------+------------------+
```

```csharp
public class ChatMessageStore
{
    // 方案1: MySQL 分表 + 冷热分离
    // 热数据存 Redis（每个玩家最近100条，TTL 1天）
    // 冷数据批量写入 ClickHouse（异步每5秒 flush 一次）
    public async Task SaveHybrid(ChatMessage msg)
    {
        // 热数据：Redis List
        string key = $"chat_history:{msg.FromPlayerId}";
        string json = JsonSerializer.Serialize(msg);
        await _redis.ListLeftPushAsync(key, json);
        await _redis.ListTrimAsync(key, 0, 99);
        await _redis.KeyExpireAsync(key, TimeSpan.FromDays(1));

        // 冷数据：异步批量写入 ClickHouse
        await _chatBuffer.WriteAsync(msg);
    }

    // 批量写入 ClickHouse（每5秒 flush 一次，最多1000条）
    public class ClickHouseChatBuffer
    {
        private readonly Channel<ChatMessage> _channel =
            Channel.CreateBounded<ChatMessage>(50000);

        public async Task WriteAsync(ChatMessage msg) =>
            await _channel.Writer.WriteAsync(msg);

        public async Task FlushLoop()
        {
            var batch = new List<ChatMessage>(1000);
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            while (await timer.WaitForNextTickAsync())
            {
                while (_channel.Reader.TryRead(out var msg) && batch.Count < 1000)
                    batch.Add(msg);

                if (batch.Count > 0)
                {
                    using var conn = new ClickHouseConnection(_connectionString);
                    await conn.OpenAsync();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO chat_logs (player_id, channel, content, created_at) VALUES" +
                        string.Join(",", batch.Select(m =>
                            $"({m.FromPlayerId}, {(int)m.Channel}, '{Escape(m.Content)}', '{m.Timestamp:yyyy-MM-dd HH:mm:ss}')"));
                    await cmd.ExecuteNonQueryAsync();
                    batch.Clear();
                }
            }
        }
    }
}
```

---

## 二、消息去重与顺序保证

### 消息 ID 和 ACK 机制

聊天系统的核心挑战：确保每条消息"不丢不重不乱序"

```csharp
public class ChatMessageDelivery
{
    private readonly SnowflakeIdGenerator _idGenerator = new(1);
    private readonly Dictionary<long, long> _lastClientSeqId = new();

    // 去重接收：客户端序列号递增，重复消息幂等返回
    public async Task<ChatResult> ReceiveMessage(long playerId, long clientSeqId, ChatMessage msg)
    {
        lock (_lastClientSeqId)
        {
            if (_lastClientSeqId.TryGetValue(playerId, out var lastSeq))
            {
                if (clientSeqId <= lastSeq) return ChatResult.SuccessDuplicate();
                if (clientSeqId - lastSeq > 100)
                    Log.Warning("消息序号跳跃: Player={Player}, Last={Last}, Cur={Cur}", playerId, lastSeq, clientSeqId);
            }
            _lastClientSeqId[playerId] = clientSeqId;
        }

        long msgId = _idGenerator.NextId();
        var delivered = new DeliveredMessage { MsgId = msgId, ClientSeqId = clientSeqId, FromPlayerId = playerId, Channel = msg.Channel, Content = msg.Content, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };

        await _chatStore.SaveHybrid(msg);
        await DispatchToChannel(delivered);
        return ChatResult.Success(msgId);
    }

    // 按频道分发：世界用 Pub/Sub，公会用 Stream，私聊可靠投递
    private async Task DispatchToChannel(DeliveredMessage msg)
    {
        switch (msg.Channel)
        {
            case ChatChannel.World:
                await _redis.PublishAsync("channel:world", JsonSerializer.Serialize(msg));
                break;
            case ChatChannel.Guild:
                await _redis.StreamAddAsync("stream:guild", new[] { new NameValueEntry("data", JsonSerializer.Serialize(msg)) }, maxLength: 10000);
                break;
            case ChatChannel.Whisper:
                await DeliverToPlayer(msg.ToPlayerId, msg);
                break;
        }
    }

    // 私聊带 ACK 重试，3次失败存离线
    private async Task DeliverToPlayer(long targetPlayerId, DeliveredMessage msg)
    {
        for (int retry = 0; retry < 3; retry++)
        {
            try
            {
                await SendToPlayer(targetPlayerId, MsgId.ChatMessage, msg);
                string ackKey = $"chat_ack:{msg.MsgId}:{targetPlayerId}";
                var ackTask = _redis.ListLeftPopAsync(ackKey);
                if (await Task.WhenAny(ackTask, Task.Delay(1000)) == ackTask && ackTask.Result.HasValue)
                    return;
            }
            catch { }
            await Task.Delay(100 * (retry + 1));
        }
        await StoreOfflineMessage(targetPlayerId, msg);
    }
}
```

### 离线消息同步

```csharp
public class OfflineMessageSync
{
    private readonly IDatabase _redis;

    // 存储离线消息（玩家不在线时）
    public async Task StoreOfflineMessage(long playerId, DeliveredMessage msg)
    {
        string key = $"offline_chat:{playerId}";
        await _redis.ListRightPushAsync(key,
            JsonSerializer.Serialize(msg));
        await _redis.KeyExpireAsync(key, TimeSpan.FromDays(7)); // 保留7天
    }

    // 玩家上线时拉取离线消息
    public async Task<List<DeliveredMessage>> FetchOfflineMessages(long playerId)
    {
        string key = $"offline_chat:{playerId}";
        var messages = new List<DeliveredMessage>();

        while (true)
        {
            var json = await _redis.ListLeftPopAsync(key);
            if (!json.HasValue) break;
            messages.Add(JsonSerializer.Deserialize<DeliveredMessage>(json));
        }

        return messages;
    }
}
```

---

## 三、富文本聊天

### 自定义标签系统

```csharp
public class RichTextChat
{
    // 聊天支持的自定义标签
    // [item:101]     → 显示为道具名称，点击查看
    // [hero:3]       → 显示为英雄名，点击查看
    // [emoji:123]    → 自定义表情
    // [player:10001] → @玩家
    // [url]...[/url] → 链接

    public class RichTag
    {
        public string Type { get; set; }   // item/hero/emoji/player
        public string Id { get; set; }
        public string DisplayText { get; set; }
        public string Color { get; set; }  // 显示颜色
    }

    // 解析富文本
    public string ParseRichText(string raw, long senderId)
    {
        // 匹配 [tag:value] 模式
        var regex = new Regex(@"\[(\w+):([^\]]+)\]");
        return regex.Replace(raw, match =>
        {
            var type = match.Groups[1].Value;
            var id = match.Groups[2].Value;

            switch (type)
            {
                case "item":
                    var item = GetItemDef(int.Parse(id));
                    return $"<color=#FFD700>{item.Name}</color>";

                case "hero":
                    var hero = GetHeroDef(int.Parse(id));
                    return $"<color=#00BFFF>{hero.Name}</color>";

                case "emoji":
                    return $"<sprite name=\"emoji_{id}\">";

                case "player":
                    return $"<color=#00FF00><link=player:{id}>{GetPlayerName(long.Parse(id))}</link></color>";

                default:
                    return match.Value;
            }
        });
    }

    // 检查聊天内容是否含有外链
    public bool ContainsExternalLink(string content) =>
        new Regex(@"(https?://|www\.)[^\s\]>]+", RegexOptions.IgnoreCase).IsMatch(content);

    // Emoji 表
    private readonly Dictionary<int, string> _emojiTable = new()
    {
        { 1, "😊" }, { 2, "😂" }, { 3, "❤️" }, { 4, "👍" },
        { 5, "🎉" }, { 6, "🔥" }, { 7, "💀" }, { 8, "😭" },
    };

    public string ReplaceEmoticons(string content) =>
        new Regex(@"\[emoji:(\d+)\]").Replace(content, m => _emojiTable.GetValueOrDefault(int.Parse(m.Groups[1].Value), "❓"));
}
```

---

## 四、敏感词过滤进阶：AC 自动机

### DFA 的局限与 AC 自动机

```
DFA 问题：
  - 只能匹配前缀
  - 每次匹配失败要回退到 i+1 重新开始
  - 时间复杂度 O(n * m) 最坏

AC 自动机：
  - 一次扫描，不回溯
  - 构建失败指针（fail pointer）
  - 时间复杂度 O(n + m) 严格线性
  - 支持最大匹配！
```

```csharp
public class ACAutomaton
{
    public class AcNode
    {
        public Dictionary<char, AcNode> Children { get; set; } = new();
        public AcNode Fail { get; set; }
        public bool IsEnd { get; set; }
        public int Length { get; set; }
        public string Word { get; set; }
    }

    private readonly AcNode _root = new();

    // 构建 Trie + 失败指针（BFS）
    public void Build(List<string> sensitiveWords)
    {
        foreach (var word in sensitiveWords)
        {
            var current = _root;
            foreach (char c in word)
            {
                if (!current.Children.ContainsKey(c))
                    current.Children[c] = new AcNode();
                current = current.Children[c];
            }
            current.IsEnd = true;
            current.Word = word;
            current.Length = word.Length;
        }

        var queue = new Queue<AcNode>();
        foreach (var (_, node) in _root.Children)
        {
            node.Fail = _root;
            queue.Enqueue(node);
        }

        while (queue.Count > 0)
        {
            var parent = queue.Dequeue();
            foreach (var (c, child) in parent.Children)
            {
                var fail = parent.Fail;
                while (fail != null && !fail.Children.ContainsKey(c))
                    fail = fail.Fail;
                child.Fail = fail?.Children.GetValueOrDefault(c) ?? _root;
                if (child.Fail.IsEnd) { child.IsEnd = true; child.Word = child.Fail.Word; }
                queue.Enqueue(child);
            }
        }
    }

    // 一次扫描过滤
    public FilterResult Filter(string text)
    {
        var chars = text.ToCharArray();
        var current = _root;
        var hitWords = new HashSet<string>();

        for (int i = 0; i < text.Length; i++)
        {
            char c = char.ToLower(text[i]);
            while (current != _root && !current.Children.ContainsKey(c))
                current = current.Fail;

            if (current.Children.TryGetValue(c, out var next))
                current = next;

            if (current.IsEnd)
            {
                hitWords.Add(current.Word);
                for (int j = i - current.Length + 1; j <= i; j++)
                    chars[j] = '*';
            }
        }

        var level = hitWords.Any(IsSevereWord) ? FilterLevel.Block : FilterLevel.Replace;
        return new FilterResult
        {
            Filtered = new string(chars),
            HitWords = hitWords.ToList(),
            Level = level
        };
    }
}

public class FilterResult
{
    public string Filtered { get; set; }
    public List<string> HitWords { get; set; } = new();
    public FilterLevel Level { get; set; }
}

public enum FilterLevel { Replace, Block, Review }
```

---

## 五、好友推荐算法

### 基于共同好友和兴趣的推荐

```csharp
public class FriendSuggestionService
{
    private readonly IDatabase _redis;

    // 好友推荐算法：混合协同过滤
    public async Task<List<FriendSuggestion>> GetSuggestions(
        long playerId, int maxResults = 20)
    {
        var player = await GetPlayer(playerId);
        var existingFriends = await GetFriendIds(playerId);
        var existingSet = existingFriends.ToHashSet();
        existingSet.Add(playerId); // 排除自己

        var candidates = new Dictionary<long, double>(); // playerId → score

    // 维度1: 共同好友（最高权重 ×3）
    var mutualFriendScores = await CalculateMutualFriendScore(playerId, existingFriends);
    foreach (var (pid, score) in mutualFriendScores)
        if (!existingSet.Contains(pid)) candidates[pid] = candidates.GetValueOrDefault(pid) + score * 3.0;

    // 维度2: 同公会（中等权重 ×1.5）
    if (player.GuildId > 0)
        foreach (var member in await GetGuildMembers(player.GuildId))
            if (!existingSet.Contains(member)) candidates[member] = candidates.GetValueOrDefault(member) + 1.5;

    // 维度3: 同等级段（低权重 ×0.5）
    foreach (var pid in await _db.QueryAsync<long>(
        "SELECT id FROM players WHERE level BETWEEN @Min AND @Max AND id != @Me LIMIT 200",
        new { Min = Math.Max(1, player.Level - 3), Max = player.Level + 3, Me = playerId }))
        if (!existingSet.Contains(pid)) candidates.TryAdd(pid, 0.5);

    return candidates.OrderByDescending(kv => kv.Value).Take(maxResults)
        .Select(kv => new FriendSuggestion { PlayerId = kv.Key, Score = kv.Value, MutualFriends = mutualFriendScores.GetValueOrDefault(kv.Key, 0) })
        .ToList();
    }

    // 共同好友计算（Redis Set 交集运算）
    private async Task<Dictionary<long, int>> CalculateMutualFriendScore(
        long playerId, List<long> existingFriends)
    {
        var result = new Dictionary<long, int>();
        if (existingFriends.Count == 0) return result;

        string tempKey = $"friend_suggest:{playerId}:temp";
        var batch = _redis.CreateBatch();
        foreach (var friendId in existingFriends.Take(20))
        {
            batch.SetCombineAndStoreAsync(SetOperation.Union, tempKey, tempKey, $"friends:{friendId}");
        }
        batch.KeyExpireAsync(tempKey, TimeSpan.FromMinutes(1));
        batch.Execute();

        var candidates = await _redis.SetMembersAsync(tempKey);
        var myFriendSet = existingFriends.ToHashSet();

        foreach (var candidate in candidates)
        {
            long cid = long.Parse(candidate);
            if (myFriendSet.Contains(cid) || cid == playerId) continue;
            long common = await _redis.SetCombineAndStoreAsync(SetOperation.Intersect,
                $"common:{playerId}:{cid}", $"friends:{playerId}", $"friends:{cid}");
            if (common > 0) result[cid] = (int)common;
        }
        await _redis.KeyDeleteAsync(tempKey);
        return result;
    }
}

public class FriendSuggestion
{
    public long PlayerId { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public double Score { get; set; }
    public int MutualFriends { get; set; }
    public string Reason { get; set; }
}
```

---

## 六、公会战与领土系统

### 公会领地管理

```csharp
public class GuildTerritorySystem
{
    // 地图上的领地格子
    public class Territory
    {
        public int TerritoryId { get; set; }
        public long OwnerGuildId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Level { get; set; }           // 领地等级
        public DateTime LastBattleTime { get; set; }
        public string Status { get; set; }       // peace/war/cooldown
    }

    // 公会对领地的宣战
    public async Task DeclareWar(long guildId, int territoryId)
    {
        var territory = await GetTerritory(territoryId);
        if (territory == null) throw new BusinessException("领地不存在");

        var guildTerritories = await GetGuildTerritories(guildId);
        bool isAdjacent = guildTerritories.Any(t =>
            Math.Abs(t.X - territory.X) + Math.Abs(t.Y - territory.Y) == 1);
        if (!isAdjacent && guildTerritories.Count > 0)
            throw new BusinessException("只能攻打相邻领地");
        if (territory.Status == "cooldown")
            throw new BusinessException("该领地处于休战期");

        var guild = await GetGuild(guildId);
        var ownerGuild = await GetGuild(territory.OwnerGuildId);
        if (ownerGuild != null && Math.Abs(guild.Level - ownerGuild.Level) > 3)
            throw new BusinessException("公会等级差距过大");

        int warCost = 10000 * (territory.Level + 1);
        if (guild.Funds < warCost) throw new BusinessException("公会资金不足");
        await DeductGuildFunds(guildId, warCost);

        await SaveGuildWar(new GuildWar
        {
            AttackerId = guildId, DefenderId = territory.OwnerGuildId,
            TerritoryId = territoryId, StartTime = DateTime.UtcNow.AddHours(2),
            Duration = TimeSpan.FromHours(1), State = "scheduled"
        });
        await NotifyGuildWar(guildId, $"已对领地 {territoryId} 宣战");
        if (territory.OwnerGuildId > 0)
            await NotifyGuildWar(territory.OwnerGuildId, $"领地 {territoryId} 受到宣战");
    }

    // 领地收益（每小时产出 + 属性加成）
    public async Task ApplyTerritoryBenefits(long guildId, int territoryId)
    {
        var territory = await GetTerritory(territoryId);
        await _redis.HashIncrementAsync($"guild_income:{guildId}", "gold", 1000 * territory.Level);
        await _redis.HashIncrementAsync($"guild_income:{guildId}", "exp", 500 * territory.Level);
        await _redis.HashSetAsync($"guild_buff:{guildId}", $"territory_{territoryId}",
            JsonSerializer.Serialize(new { AttackBonus = territory.Level * 0.02, DefenseBonus = territory.Level * 0.02 }));
    }
}
```

---

## 七、跨服聊天联邦

### 跨服消息路由

```csharp
public class CrossServerChatFederation
{
    // 跨服消息结构
    public class CrossServerMessage
    {
        public string MessageId { get; set; }
        public int SourceServerId { get; set; }
        public ChatChannel Channel { get; set; }
        public long PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int PlayerServerId { get; set; }
        public string Content { get; set; }
        public long Timestamp { get; set; }
    }

    // 方案1: 中心路由（简单，适合少量服务器）
    public class CentralRouter
    {
        private readonly IMessageQueue _mq;
        public async Task PublishCrossServer(CrossServerMessage msg) =>
            await _mq.PublishAsync("cross_server_chat", msg);

        public async Task Subscribe() =>
            await _mq.SubscribeAsync("cross_server_chat", async (CrossServerMessage msg) =>
            {
                if (msg.SourceServerId == _localServerId) return;
                long age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - msg.Timestamp;
                if (age > 60) return; // TTL 60秒
                await RouteToLocalChannel(msg);
            });
    }

    // 方案2: Gossip 协议（去中心化，适合大规模集群）
    public class GossipChatSync
    {
        private readonly List<int> _peerServers = new();
        private readonly Random _random = new();
        private readonly BloomFilter _seenMessages = new(1000000, 0.01);

        public async Task GossipSync()
        {
            var pending = await GetPendingCrossServerMessages();
            if (pending.Count == 0) return;
            foreach (var target in _peerServers.OrderBy(_ => _random.Next()).Take(3))
            {
                try { await SendMessagesToPeer(target, pending); }
                catch (Exception ex) { Log.Warning("Gossip 失败: {Error}", ex.Message); }
            }
        }
        public bool IsDuplicate(string messageId) {
            if (_seenMessages.Contains(messageId)) return true;
            _seenMessages.Add(messageId); return false;
        }
    }

    // 全局 UID: "服务器ID_玩家ID"
    public static string ToGlobalUid(int serverId, long playerId) => $"{serverId}_{playerId}";
    public static (int, long) ParseGlobalUid(string uid) {
        var p = uid.Split('_'); return (int.Parse(p[0]), long.Parse(p[1]));
    }
}
```

---

## 八、聊天审核与举报系统

### 自动化审核 + 人工审核队列

```csharp
public class ChatModerationSystem
{
    // 举报消息（防滥用：每人每天限10次举报）
    public async Task ReportMessage(long reporterId, long targetPlayerId, long messageId, string reason)
    {
        var reportCount = await _redis.StringIncrementAsync($"report_count:{reporterId}:{DateTime.UtcNow:yyyyMMdd}");
        await _redis.KeyExpireAsync($"report_count:{reporterId}:{DateTime.UtcNow:yyyyMMdd}", TimeSpan.FromDays(1));
        if (reportCount > 10) throw new BusinessException("今日举报次数已达上限");

        bool alreadyReported = await _redis.SetContainsAsync("reported_messages", messageId.ToString());
        if (alreadyReported) return;
        await _redis.SetAddAsync("reported_messages", messageId.ToString());

        await _db.ExecuteAsync("INSERT INTO report_logs (reporter_id, target_id, message_id, reason, created_at) VALUES (@R, @T, @M, @Reason, NOW())",
            new { R = reporterId, T = targetPlayerId, M = messageId, Reason = reason });

        await CheckAutoAction(targetPlayerId);
    }

    // 自动处理：按举报次数阶梯处罚
    private async Task CheckAutoAction(long playerId)
    {
        int recentReports = await _db.QueryFirstAsync<int>(
            "SELECT COUNT(*) FROM report_logs WHERE target_id = @T AND created_at > DATE_SUB(NOW(), INTERVAL 24 HOUR)", new { T = playerId });
        if (recentReports >= 30) await BanPlayer(playerId, TimeSpan.FromDays(1), "被大量玩家举报");
        else if (recentReports >= 10) await MutePlayer(playerId, TimeSpan.FromHours(1), "被多名玩家举报");
    }

    public async Task MutePlayer(long playerId, TimeSpan duration, string reason)
    {
        await _redis.StringSetAsync($"muted:{playerId}", reason, duration);
        await SendToPlayer(playerId, MsgId.MutedNotification, new { Until = DateTimeOffset.UtcNow.Add(duration).ToUnixTimeSeconds(), Reason = reason });
    }

    public async Task<bool> CheckMuted(long playerId) =>
        (await _redis.StringGetAsync($"muted:{playerId}")).HasValue;

    // 人工审核队列
    public class ManualReviewQueue
    {
        private readonly Channel<ReviewItem> _reviewChannel = Channel.CreateBounded<ReviewItem>(10000);
        public async Task SubmitForReview(ChatMessage msg, FilterResult filterResult) =>
            await _reviewChannel.Writer.WriteAsync(new ReviewItem { MessageId = msg.Id, PlayerId = msg.FromPlayerId, Content = msg.Content, HitWords = filterResult.HitWords });

        public async Task<List<ReviewItem>> GetPendingReviews(int count = 20)
        {
            var items = new List<ReviewItem>();
            for (int i = 0; i < count && _reviewChannel.Reader.TryRead(out var item); i++) items.Add(item);
            return items;
        }

        public async Task ProcessReviewResult(long messageId, ReviewAction action, string note)
        {
            await _db.ExecuteAsync("UPDATE chat_logs SET review_status = @S, reviewer_note = @N WHERE id = @Id",
                new { Id = messageId, S = (int)action, N = note });
            if (action == ReviewAction.BanPlayer)
            {
                var msg = await GetMessage(messageId);
                await BanPlayer(msg.FromPlayerId, TimeSpan.FromDays(3), note);
            }
        }
    }

    public enum ReviewAction { Approve, Delete, WarnPlayer, MutePlayer, BanPlayer }
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 消息存储 | Redis热数据+ClickHouse冷数据，MongoDB适合中等规模 |
| 去重与顺序 | 客户端序列号递增+去重，全局消息ID+ACK重试 |
| 富文本标签 | [item:id] [hero:id] 自定义标签，解析后显示颜色和链接 |
| AC自动机 | 失败指针+一次扫描，比DFA更高效的敏感词过滤 |
| 好友推荐 | 共同好友交集(Redis Set)+同公会+同等级段加权评分 |
| 公会领土 | 相邻领地宣战，占领积分结算，领地每小时产出收益 |
| 跨服聊天联邦 | 中央路由或Gossip协议，Bloom Filter去重 |
| 审核与举报 | 举报次数阈值自动禁言/封禁，可疑消息送人工审核队列 |
