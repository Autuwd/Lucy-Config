# Day 27：GM 工具与运营平台 — 进阶深入

## 一、RBAC 权限模型

### 权限模型设计

```
RBAC 四级权限模型（游戏运营场景）:

SuperAdmin（超级管理员）
  ├── 所有权限
  ├── 管理其他管理员
  └── 查看审计日志

Admin（管理员）
  ├── 封禁/解封玩家
  ├── 发放道具/货币
  ├── 修改玩家数据
  ├── 配置运营活动
  └── 发送全服公告

Operator（运营操作员）
  ├── 查询玩家信息
  ├── 禁言（不能封号）
  ├── 发送邮件
  └── 查看报表

Viewer（只读查看者）
  ├── 查看玩家信息
  ├── 查看统计报表
  └── 查看日志（不能操作）
```

```csharp
// 基于 Policy 的权限控制
public class PermissionPolicy
{
    public string Name { get; set; }
    public List<string> RequiredRoles { get; set; } = new();
    public bool RequireAllRoles { get; set; } = false;
}

// 权限定义（枚举 + 描述）
public static class GmPermissions
{
    public const string PlayerView = "player:view";
    public const string PlayerBan = "player:ban";
    public const string PlayerUnban = "player:unban";
    public const string PlayerMute = "player:mute";
    public const string PlayerKick = "player:kick";

    public const string ItemGrant = "item:grant";
    public const string ItemRemove = "item:remove";
    public const string CurrencyModify = "currency:modify";

    public const string MailSend = "mail:send";
    public const string MailMass = "mail:mass";

    public const string AnnounceBroadcast = "announce:broadcast";
    public const string AnnouncePopup = "announce:popup";

    public const string ActivityCreate = "activity:create";
    public const string ActivityToggle = "activity:toggle";

    public const string ServerRestart = "server:restart";
    public const string ServerMaintenance = "server:maintenance";

    public const string AdminCreate = "admin:create";
    public const string AuditView = "audit:view";
}

// 角色-权限映射
public class RolePermissionConfig
{
    public static Dictionary<string, List<string>> GetDefaultPermissions()
    {
        return new()
        {
            ["viewer"] = new()
            {
                GmPermissions.PlayerView,
                GmPermissions.AuditView
            },
            ["operator"] = new()
            {
                GmPermissions.PlayerView,
                GmPermissions.PlayerMute,
                GmPermissions.PlayerKick,
                GmPermissions.MailSend,
                GmPermissions.AnnounceBroadcast,
                GmPermissions.AnnouncePopup,
                GmPermissions.AuditView
            },
            ["admin"] = new()
            {
                GmPermissions.PlayerView,
                GmPermissions.PlayerBan,
                GmPermissions.PlayerUnban,
                GmPermissions.PlayerMute,
                GmPermissions.PlayerKick,
                GmPermissions.ItemGrant,
                GmPermissions.ItemRemove,
                GmPermissions.CurrencyModify,
                GmPermissions.MailSend,
                GmPermissions.MailMass,
                GmPermissions.AnnounceBroadcast,
                GmPermissions.AnnouncePopup,
                GmPermissions.ActivityCreate,
                GmPermissions.ActivityToggle,
                GmPermissions.ServerRestart,
                GmPermissions.ServerMaintenance,
                GmPermissions.AuditView
            },
            ["superadmin"] = new() // 拥有所有权限
        };
    }
}

// 权限检查过滤器
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var role = user.FindFirst("role")?.Value;
        var permissions = user.FindAll("permission").Select(c => c.Value).ToList();

        // superadmin 拥有所有权限
        if (role == "superadmin") return;

        if (!permissions.Contains(_permission))
        {
            Log.Warning("权限不足: 管理员 {User} 尝试 {Permission}",
                user.Identity.Name, _permission);
            context.Result = new ForbidResult();
        }
    }
}

// API 使用示例
[ApiController]
[Route("api/gm/player")]
public class PlayerManagementController : ControllerBase
{
    [HttpGet("{playerId}")]
    [RequirePermission(GmPermissions.PlayerView)]
    public async Task<ActionResult> GetPlayer(long playerId)
    {
        var player = await _playerService.GetPlayerDetail(playerId);
        return Ok(player);
    }

    [HttpPost("{playerId}/ban")]
    [RequirePermission(GmPermissions.PlayerBan)]
    public async Task<ActionResult> BanPlayer(long playerId, BanRequest req)
    {
        // 二次确认（防止误操作）
        if (!req.Confirmed)
            return BadRequest(new { Message = "请确认封禁操作" });

        await _playerService.BanPlayer(playerId, req.Duration, req.Reason, GetAdminId());
        return Ok(new { Message = "封禁成功" });
    }
}
```

---

## 二、操作审计追踪

```csharp
// 完整的审计追踪系统
public class AuditTrailService
{
    // 每次 GM 操作生成审计记录
    public async Task RecordOperation(AuditRecord record)
    {
        // 1. 写 MySQL（主要存储）
        await _db.ExecuteAsync(
            @"INSERT INTO gm_audit_log
              (admin_id, admin_name, admin_ip, operation, resource_type,
               resource_id, detail, result, duration_ms, created_at)
              VALUES (@AdminId, @AdminName, @AdminIp, @Operation, @ResourceType,
               @ResourceId, @Detail, @Result, @DurationMs, @CreatedAt)",
            record);

        // 2. 写 Elasticsearch（快速查询）
        await _elasticsearch.IndexAsync(record,
            idx => idx.Index("gm-audit-logs"));

        // 3. 敏感操作（封禁/发物品）额外通知到值班群
        if (record.IsSensitive)
        {
            await _alertNotifier.NotifyAsync(
                $"GM 敏感操作: {record.AdminName} 执行了 {record.Operation}",
                record.Detail,
                AlertLevel.Warning);
        }

        // 4. 检测异常模式（某个操作频率异常）
        await DetectAbnormalPattern(record);
    }

    // 异常模式检测
    private async Task DetectAbnormalPattern(AuditRecord record)
    {
        // 检查同一个操作在短时间内的执行频率
        string key = $"audit_freq:{record.AdminId}:{record.Operation}:{DateTime.Now:yyyyMMddHH}";
        long count = await _redis.StringIncrementAsync(key);
        await _redis.KeyExpireAsync(key, TimeSpan.FromHours(1));

        if (count > 10) // 1 小时内同一操作超过 10 次
        {
            Log.Error("管理员 {Admin} 操作 {Op} 频率异常（{Count}次/小时）",
                record.AdminName, record.Operation, count);
            await _alertNotifier.NotifyAsync(
                "GM 操作异常",
                $"管理员 {record.AdminName} 在 1 小时内执行 {record.Operation} {count} 次",
                AlertLevel.Warning);
        }
    }
}

// 审计记录模型
public class AuditRecord
{
    public long Id { get; set; }
    public long AdminId { get; set; }
    public string AdminName { get; set; }
    public string AdminIp { get; set; }
    public string Operation { get; set; }      // BAN_PLAYER, GRANT_ITEM, SEND_MAIL
    public string ResourceType { get; set; }   // player, item, activity, server
    public string ResourceId { get; set; }     // 被操作的对象 ID
    public string Detail { get; set; }         // 操作详情（JSON）
    public string Result { get; set; }         // success / failure / pending
    public int DurationMs { get; set; }        // 执行耗时
    public DateTime CreatedAt { get; set; }
    public bool IsSensitive => Operation switch
    {
        "BAN_PLAYER" or "UNBAN_PLAYER" or "GRANT_ITEM" or
        "MODIFY_CURRENCY" or "MODIFY_PLAYER_DATA" => true,
        _ => false
    };
}
```

---

## 三、多维玩家搜索

```csharp
// 高性能玩家搜索系统
public class PlayerSearchService
{
    // 多维度搜索接口
    public async Task<SearchResult> SearchPlayers(PlayerSearchQuery query)
    {
        var sql = new StringBuilder("SELECT * FROM players WHERE 1=1");
        var parameters = new DynamicParameters();

        // 按 UID 搜索（精确匹配）
        if (query.PlayerId.HasValue)
        {
            sql.Append(" AND player_id = @PlayerId");
            parameters.Add("PlayerId", query.PlayerId.Value);
        }

        // 按角色名搜索（模糊匹配 + 分词）
        if (!string.IsNullOrEmpty(query.PlayerName))
        {
            sql.Append(" AND player_name LIKE @PlayerName");
            parameters.Add("PlayerName", $"%{query.PlayerName}%");
        }

        // 按设备 ID 搜索（反作弊场景）
        if (!string.IsNullOrEmpty(query.DeviceId))
        {
            sql.Append(" AND device_id = @DeviceId");
            parameters.Add("DeviceId", query.DeviceId);
        }

        // 按 IP 搜索（查批量注册）
        if (!string.IsNullOrEmpty(query.IpAddress))
        {
            sql.Append(" AND last_ip = @IpAddress");
            parameters.Add("IpAddress", query.IpAddress);
        }

        // 按等级范围搜索
        if (query.MinLevel.HasValue)
        {
            sql.Append(" AND level >= @MinLevel");
            parameters.Add("MinLevel", query.MinLevel.Value);
        }
        if (query.MaxLevel.HasValue)
        {
            sql.Append(" AND level <= @MaxLevel");
            parameters.Add("MaxLevel", query.MaxLevel.Value);
        }

        // 按注册时间范围
        if (query.RegisterFrom.HasValue)
        {
            sql.Append(" AND create_time >= @RegisterFrom");
            parameters.Add("RegisterFrom", query.RegisterFrom.Value);
        }
        if (query.RegisterTo.HasValue)
        {
            sql.Append(" AND create_time <= @RegisterTo");
            parameters.Add("RegisterTo", query.RegisterTo.Value);
        }

        // 按最后登录时间（查流失玩家）
        if (query.LastLoginBefore.HasValue)
        {
            sql.Append(" AND last_login_time < @LastLoginBefore");
            parameters.Add("LastLoginBefore", query.LastLoginBefore.Value);
        }

        // 按充值状态
        if (query.IsPaying.HasValue)
        {
            sql.Append(query.IsPaying.Value
                ? " AND total_recharge > 0"
                : " AND total_recharge = 0");
        }

        // 按封禁状态
        if (query.IsBanned.HasValue)
        {
            sql.Append(query.IsBanned.Value
                ? " AND ban_until > NOW()"
                : " AND (ban_until IS NULL OR ban_until <= NOW())");
        }

        // 分页
        sql.Append(" ORDER BY player_id DESC LIMIT @Limit OFFSET @Offset");
        parameters.Add("Limit", query.PageSize);
        parameters.Add("Offset", (query.Page - 1) * query.PageSize);

        // 执行查询
        var players = await _db.QueryAsync<PlayerSummary>(sql.ToString(), parameters);

        // 异步搜索建议（基于玩家名的前缀匹配）
        if (!string.IsNullOrEmpty(query.PlayerName) && query.PlayerName.Length >= 2)
        {
            await IndexSearchSuggestions(query.PlayerName, players.Select(p => p.PlayerName));
        }

        return new SearchResult
        {
            Players = players.ToList(),
            TotalCount = await GetTotalCount(sql.ToString(), parameters),
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    // 搜索结果缓存（相同查询 30 秒内直接从缓存返回）
    private readonly IMemoryCache _cache;

    public async Task<SearchResult> SearchWithCache(PlayerSearchQuery query)
    {
        string cacheKey = $"search:{query.GetHashCode()}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return await SearchPlayers(query);
        });
    }
}

public class PlayerSearchQuery
{
    public long? PlayerId { get; set; }
    public string PlayerName { get; set; }
    public string DeviceId { get; set; }
    public string IpAddress { get; set; }
    public int? MinLevel { get; set; }
    public int? MaxLevel { get; set; }
    public DateTime? RegisterFrom { get; set; }
    public DateTime? RegisterTo { get; set; }
    public DateTime? LastLoginBefore { get; set; }
    public bool? IsPaying { get; set; }
    public bool? IsBanned { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

---

## 四、补偿系统

```csharp
// GM 补偿系统（发放道具/货币给玩家）
public class CompensationService
{
    private readonly IDbConnection _db;
    private readonly IProducer<string, byte[]> _kafka;

    // 创建补偿任务
    public async Task<long> CreateCompensation(CreateCompensationRequest req, long adminId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            // 生成补偿编号
            string compId = $"COMP{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

            // 写入补偿主表
            await conn.ExecuteAsync(
                @"INSERT INTO compensation (comp_id, title, reason, admin_id, status, created_at)
                  VALUES (@CompId, @Title, @Reason, @AdminId, 'pending', NOW())",
                new { CompId = compId, req.Title, req.Reason, AdminId = adminId }, tx);

            // 写入补偿物品明细
            foreach (var item in req.Items)
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO compensation_items (comp_id, item_id, item_count, item_type)
                      VALUES (@CompId, @ItemId, @ItemCount, @ItemType)",
                    new { CompId = compId, item.ItemId, item.ItemCount, item.ItemType }, tx);
            }

            // 如果是定向补偿，写入目标玩家列表
            if (req.TargetPlayers?.Any() == true)
            {
                foreach (var pid in req.TargetPlayers)
                {
                    await conn.ExecuteAsync(
                        "INSERT INTO compensation_targets (comp_id, player_id) VALUES (@CompId, @Pid)",
                        new { CompId = compId, Pid = pid }, tx);
                }
            }

            await tx.CommitAsync();

            // 异步执行补偿
            _ = ExecuteCompensation(compId);

            return 0; // success
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // 执行补偿（异步后台任务）
    private async Task ExecuteCompensation(string compId)
    {
        try
        {
            var compensation = await GetCompensation(compId);
            var targetPlayers = await GetTargetPlayers(compId);

            int successCount = 0;
            int failCount = 0;

            // 批量执行（每批 100 人，使用消息队列）
            foreach (var batch in targetPlayers.Chunk(100))
            {
                var message = new CompensationMessage
                {
                    CompId = compId,
                    Players = batch.ToList(),
                    Items = compensation.Items
                };

                // 发送到 Kafka 由游戏服务器处理
                await _kafka.ProduceAsync("gm-compensations",
                    new Message<string, byte[]>
                    {
                        Key = compId,
                        Value = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message))
                    });

                successCount += batch.Length;
            }

            // 更新状态
            await _db.ExecuteAsync(
                @"UPDATE compensation
                  SET status = 'completed',
                      completed_at = NOW(),
                      success_count = @Success,
                      fail_count = @Fail
                  WHERE comp_id = @CompId",
                new { CompId = compId, Success = successCount, Fail = failCount });

            Log.Information("补偿 {CompId} 执行完成: 成功 {Success}, 失败 {Fail}",
                compId, successCount, failCount);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "补偿 {CompId} 执行失败", compId);
            await _db.ExecuteAsync(
                "UPDATE compensation SET status = 'failed' WHERE comp_id = @CompId",
                new { CompId = compId });
        }
    }
}

// 补偿任务类型
public enum CompensationTargetType
{
    SinglePlayer,       // 单个玩家
    MultiPlayer,        // 指定多个玩家
    AllPlayers,         // 全服玩家
    LevelRange,         // 等级范围
    DateRange,          // 注册时间范围
    RecentActive,       // 近期活跃
    PayingUsers         // 付费用户
}
```

---

## 五、活动管理与定时任务

```csharp
// 活动生命周期管理
public class ActivityManagementService
{
    private readonly IDatabase _redis;
    private readonly IScheduler _scheduler; // Quartz.NET

    // 创建新活动
    public async Task CreateActivity(ActivityDefinition activity)
    {
        string activityId = Guid.NewGuid().ToString("N");
        activity.Id = activityId;

        // 1. 持久化到数据库
        await _db.ExecuteAsync(
            @"INSERT INTO activities (id, name, type, start_time, end_time, config, status)
              VALUES (@Id, @Name, @Type, @StartTime, @EndTime, @Config, @Status)",
            activity);

        // 2. 写入 Redis 缓存（游戏服务器读取）
        string redisKey = $"activity:{activityId}";
        await _redis.StringSetAsync(redisKey,
            JsonSerializer.Serialize(activity),
            activity.EndTime - DateTime.UtcNow + TimeSpan.FromDays(1));

        // 3. 添加到活动时间线
        await _redis.SortedSetAddAsync("activities:timeline",
            activityId, activity.StartTime.ToUnixTimeSeconds());

        // 4. 注册定时任务（活动自动开启/关闭）
        await _scheduler.ScheduleJob(new JobBuilder
        {
            JobType = typeof(ActivityStartJob),
            TriggerTime = activity.StartTime,
            JobData = new Dictionary<string, object> { ["activityId"] = activityId }
        });

        await _scheduler.ScheduleJob(new JobBuilder
        {
            JobType = typeof(ActivityEndJob),
            TriggerTime = activity.EndTime,
            JobData = new Dictionary<string, object> { ["activityId"] = activityId }
        });
    }

    // 运行时修改活动配置（热更新）
    public async Task UpdateActivityConfig(string activityId, Dictionary<string, object> newConfig)
    {
        string redisKey = $"activity:{activityId}";
        var data = await _redis.StringGetAsync(redisKey);
        if (!data.HasValue) return;

        var activity = JsonSerializer.Deserialize<ActivityDefinition>(data);
        activity.Config = newConfig;

        // 更新缓存
        await _redis.StringSetAsync(redisKey, JsonSerializer.Serialize(activity));

        // 广播变更通知（游戏服务器订阅）
        await _redis.PublishAsync("channel:activity_change",
            JsonSerializer.Serialize(new { activityId, action = "config_update" }));
    }
}

// 活动类型
public class ActivityDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ActivityType Type { get; set; }
    // DoubleExp, DiscountShop, LoginReward, TurnTable,
    // CumulativeRecharge, RankBattle, GuildWar
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public ActivityStatus Status { get; set; }
    public Dictionary<string, object> Config { get; set; } = new();
    public List<ActivityReward> Rewards { get; set; } = new();
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; } = 999;
    public List<int> TargetServers { get; set; } // null = 全服
}
```

---

## 六、公告系统

```csharp
// 多维度公告系统
public class AnnouncementService
{
    private readonly IDatabase _redis;
    private readonly ChatService _chat;

    // 发布公告
    public async Task PublishAnnouncement(Announcement ann)
    {
        string annId = Guid.NewGuid().ToString("N");

        var message = new
        {
            id = annId,
            ann.Type,
            ann.Title,
            ann.Content,
            ann.TargetLevel,
            ann.TargetServer,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ann.Priority
        };

        switch (ann.Type)
        {
            case AnnounceType.Marquee:
                // 跑马灯公告（滚动显示在屏幕顶部）
                await _redis.PublishAsync("announce:marquee",
                    JsonSerializer.Serialize(message));
                break;

            case AnnounceType.Popup:
                // 弹窗公告（玩家登录时弹出）
                // 存入 Redis，玩家登录时读取
                string popupKey = $"announce:popup:{annId}";
                await _redis.StringSetAsync(popupKey,
                    JsonSerializer.Serialize(message),
                    ann.EndTime - DateTime.UtcNow);
                // 加入活跃公告列表
                await _redis.SortedSetAddAsync("announce:popup:active",
                    annId, ann.Priority);
                break;

            case AnnounceType.Mail:
                // 邮件公告（发送到玩家邮箱）
                var mailMessage = new MailMessage
                {
                    Title = ann.Title,
                    Content = ann.Content,
                    Sender = "系统",
                    ExpireAt = DateTime.Now.AddDays(30)
                };
                if (ann.TargetType == MailTarget.All)
                    await _mailService.SendToAllPlayers(mailMessage);
                else if (ann.TargetType == MailTarget.Online)
                    // 只发给在线玩家
                    await SendToOnlinePlayers(mailMessage);
                break;

            case AnnounceType.Chat:
                // 聊天频道公告（显示在系统频道）
                await _chat.SendSystemMessage(ann.Content);
                break;
        }

        // 记录公告发布日志
        await _auditLogger.LogAudit(GetAdminId(), "PUBLISH_ANNOUNCE", 0,
            $"发布公告: {ann.Type} - {ann.Title}");
    }

    // 公告定时任务（预约公告）
    public async Task ScheduleAnnouncement(Announcement ann, DateTime publishTime)
    {
        // 存储到待发布队列
        string key = $"announce:scheduled:{ann.Id}";
        await _redis.StringSetAsync(key, JsonSerializer.Serialize(ann),
            publishTime - DateTime.UtcNow + TimeSpan.FromHours(1));

        // Quartz 定时任务到时间自动发布
    }
}

public class Announcement
{
    public string Id { get; set; }
    public AnnounceType Type { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public int Priority { get; set; } = 0; // 越高越优先
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; } = 999;
    public List<int> TargetServers { get; set; }
    public MailTarget TargetType { get; set; } = MailTarget.All;
}

public enum AnnounceType
{
    Marquee,     // 跑马灯
    Popup,       // 弹窗
    Mail,        // 邮件
    Chat         // 聊天频道
}
```

---

## 七、Sentry 错误追踪集成

```csharp
// Program.cs 配置
builder.WebHost.UseSentry(options =>
{
    options.Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
    options.Environment = builder.Environment.EnvironmentName;

    // 采样配置
    options.TracesSampleRate = 0.1;         // 10% APM 采样
    options.ProfilesSampleRate = 0.05;      // 5% 性能分析采样

    // 附加游戏上下文
    options.BeforeSend = sentryEvent =>
    {
        // 添加服务器标识
        sentryEvent.SetTag("server_id", Environment.GetEnvironmentVariable("SERVER_ID"));
        sentryEvent.SetTag("server_type", "game-server");
        return sentryEvent;
    };

    // 性能监控
    options.MaxRequestBodySize = RequestSize.Always;
    options.SendDefaultPii = true;
});

// 手动捕获业务异常
public class SentryGameIntegration
{
    private readonly IHub _sentryHub;

    public async Task CapturePlayerException(long playerId, string action, Exception ex)
    {
        // 附加玩家上下文
        SentrySdk.ConfigureScope(scope =>
        {
            scope.SetTag("player_id", playerId.ToString());
            scope.SetExtra("action", action);
            scope.SetExtra("server_uptime", (DateTime.UtcNow - _startTime).ToString());
            scope.SetExtra("online_players", MetricsRegistry.OnlinePlayers.Value);
        });

        // 捕获异常
        var sentryId = SentrySdk.CaptureException(ex);

        Log.Error(ex, "玩家 {PlayerId} 操作 {Action} 异常 (Sentry: {SentryId})",
            playerId, action, sentryId);
    }

    // 支持游戏内反馈
    public async Task SendPlayerFeedback(long playerId, string feedback)
    {
        SentrySdk.CaptureMessage($"玩家反馈: {feedback}",
            scope =>
            {
                scope.SetTag("player_id", playerId.ToString());
                scope.SetLevel(SentryLevel.Info);
            });
    }
}

// Sentry Dashboard 关注的游戏指标:
// 1. 异常率趋势（每小时比较）
// 2. Top 10 报错接口
// 3. 受影响的玩家数量（Deduplicate by player_id）
// 4. 崩溃版本分布（按版本号分组）
// 5. 服务器延迟与错误相关性
```

---

## 八、事件响应手册 (Runbook)

```yaml
# 事件响应流程（Playbook）

# P0 事件：游戏服务器全服宕机

# 1. 发现阶段
#    告警: 在线人数骤降为 0，或 P99 > 10s
#    确认: 检查 Prometheus → 确认是全部还是部分

# 2. 响应阶段
#    - 值班人员确认事件（5 分钟内）
#    - 发布维护公告（GM 工具 → 全服弹窗）
#    - 拉起事件响应群（企业微信/钉钉）

# 3. 诊断阶段
#    - 检查最近变更（Git 最近 24 小时提交）
#    - 检查日志 (Kibana: level:Error)
#    - 检查 Sentry (最近异常)
#    - 检查数据库 (慢查询 / 连接数)

# 4. 恢复阶段
#    - 快速回滚: 切换到蓝环境 / 回退版本
#    - 紧急修复: 热更新配置 / 改 feature flag
#    - 重启服务: 分批重启（先网关，后场景）

# 5. 复盘阶段
#    - 根因分析文档 (5 Whys)
#    - 措施跟踪 (Action Items)
#    - 更新 Runbook

# 常用命令速查:
#   kubectl rollout undo deployment/game-server    # 回滚
#   kubectl get pods --all-namespaces | grep Crash  # 查看崩溃
#   kubectl logs -f pod/game-server-xxx --tail=100  # 查看日志
#   curl http://game-server:8888/healthz            # 健康检查
#   redis-cli -h <host> ping                        # Redis 连通性
```

```csharp
// 自动化事件响应
public class AutoIncidentResponse
{
    private readonly IncidentStorage _storage;

    // 自动创建事件
    public async Task<IncidentRecord> CreateIncident(string alertName, string detail, string severity)
    {
        var incident = new IncidentRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            AlertName = alertName,
            Detail = detail,
            Severity = severity,
            Status = IncidentStatus.Open,
            CreatedAt = DateTime.UtcNow,
            AutoDiagnosis = await RunAutoDiagnosis()
        };

        await _storage.Save(incident);

        // 自动通知值班人员
        await NotifyOnCall(incident);

        return incident;
    }

    private async Task<DiagnosisResult> RunAutoDiagnosis()
    {
        var result = new DiagnosisResult();

        // 自动检查
        result.LastDeployTime = await GetLastDeployTime();
        result.RecentErrors = await GetRecentErrorCount(TimeSpan.FromMinutes(5));
        result.DbConnectivity = await CheckDbConnection();
        result.RedisConnectivity = await CheckRedisConnection();
        result.MemoryUsage = await GetMemoryUsage();
        result.CpuUsage = await GetCpuUsage();

        // 生成初步诊断
        if (!result.DbConnectivity)
            result.SuspectedCause = "数据库连接异常";
        else if (!result.RedisConnectivity)
            result.SuspectedCause = "Redis 连接异常";
        else if (result.MemoryUsage > 95)
            result.SuspectedCause = "内存耗尽";
        else if (result.RecentErrors > 1000)
            result.SuspectedCause = "大量错误，可能由代码变更导致";

        // 建议操作
        result.SuggestedAction = result.SuspectedCause switch
        {
            "数据库连接异常" => "1. 检查 DB 服务器状态\n2. 检查连接池配置\n3. 检查网络",
            "内存耗尽" => "1. 重启服务器\n2. 检查内存泄漏\n3. 考虑扩容",
            _ => "1. 检查日志定位根因\n2. 考虑回滚最近变更"
        };

        return result;
    }
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| RBAC | 基于角色的权限模型，Viewer/Operator/Admin/SuperAdmin |
| 权限粒度 | 细粒度到每个 API 操作，superadmin 拥有全部权限 |
| 审计日志 | 三路写入（MySQL + ES + 通知）保证可追溯 |
| 多维搜索 | UID/名字/设备/IP/等级/充值/封禁状态组合查询 |
| 补偿系统 | 异步批量发放道具，Kafka 驱动游戏服务器执行 |
| 活动管理 | 生命周期管理 + 定时开启关闭 + 运行时热更新 |
| 公告系统 | 跑马灯/弹窗/邮件/聊天 四种公告类型 |
| Sentry | 错误追踪 + 玩家上下文 + 版本分布 |
| Runbook | P0 事件的标准化响应流程和命令速查 |
