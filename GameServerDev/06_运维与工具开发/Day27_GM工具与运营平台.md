# Day 27：GM 工具与运营平台

## 一、GM 工具功能

### 核心功能

```
GM 工具 = 运营管理后台

1. 玩家管理
   - 查询玩家信息（等级/背包/充值/日志）
   - 封禁/解封账号
   - 发放道具/货币
   - 禁言/关小黑屋

2. 公告管理
   - 全服广播
   - 登录弹窗公告
   - 邮件群发

3. 运营活动
   - 配置活动（双倍经验/限时折扣）
   - 活动开关
   - 奖励发放

4. 服务器管理
   - 重启/维护
   - 日志查看
   - 性能监控

5. 数据查看
   - 在线人数
   - 充值记录
   - 新增用户
```

---

## 二、ASP.NET Core 管理后台

### 项目结构

```
AdminWeb/
├── Controllers/
│   ├── PlayerController.cs    # 玩家管理
│   ├── AnnounceController.cs  # 公告管理
│   ├── ActivityController.cs  # 活动管理
│   ├── ServerController.cs    # 服务器管理
│   └── AuthController.cs      # 管理员登录
├── Services/
│   ├── PlayerService.cs
│   ├── MailService.cs
│   └── ServerService.cs
├── Models/
├── Views/
├── Filters/
│   └── AdminAuthFilter.cs
└── Program.cs
```

### 管理员认证

```csharp
public class AdminAuthController : Controller
{
    [HttpPost("admin/login")]
    public async Task<IActionResult> Login(AdminLoginRequest req)
    {
        // 验证管理员账号密码
        var admin = await _db.QueryFirstOrDefaultAsync<AdminUser>(
            "SELECT * FROM admin_users WHERE username = @Username",
            new { req.Username });

        if (admin == null || !VerifyPassword(req.Password, admin.PasswordHash))
            return Unauthorized(new { Message = "账号或密码错误" });

        // 生成 JWT Token
        var token = GenerateJwtToken(admin);

        // 记录操作日志
        await _auditLogger.LogAudit(admin.Username, "LOGIN", 0, "管理员登录");

        return Ok(new { Token = token, Admin = admin.Username, Role = admin.Role });
    }

    private string GenerateJwtToken(AdminUser admin)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]));

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, admin.Username),
            new Claim(ClaimTypes.Role, admin.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "GameAdmin",
            audience: "AdminWeb",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// 管理权限过滤
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminAuthAttribute : Attribute, IAuthorizationFilter
{
    private readonly AdminRole _minRole;

    public AdminAuthAttribute(AdminRole minRole = AdminRole.Operator)
    {
        _minRole = minRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var role = Enum.Parse<AdminRole>(user.FindFirst(ClaimTypes.Role).Value);
        if (role < _minRole)
        {
            context.Result = new ForbidResult();
        }
    }
}

public enum AdminRole
{
    Viewer = 0,     // 查看者（只能看不能操作）
    Operator = 1,   // 操作员（能发邮件/禁言）
    Admin = 2,      // 管理员（能封号/发物品）
    SuperAdmin = 3  // 超级管理员（所有权限）
}
```

### 玩家管理 API

```csharp
[ApiController]
[Route("api/gm/player")]
[AdminAuth(AdminRole.Operator)]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;
    private readonly AuditLogger _auditLogger;

    // 查询玩家
    [HttpGet("{playerId}")]
    public async Task<ActionResult<PlayerDetail>> GetPlayer(long playerId)
    {
        var player = await _playerService.GetPlayerDetail(playerId);
        if (player == null)
            return NotFound();

        return Ok(player);
    }

    // 按名字搜索
    [HttpGet("search")]
    public async Task<ActionResult<List<PlayerSummary>>> SearchPlayers(
        [FromQuery] string name,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var players = await _playerService.SearchPlayers(name, page, pageSize);
        return Ok(players);
    }

    // 发放道具
    [HttpPost("{playerId}/items/grant")]
    [AdminAuth(AdminRole.Admin)]
    public async Task<ActionResult> GrantItem(long playerId,
        [FromBody] GrantItemRequest req)
    {
        var adminName = User.Identity.Name;

        await _auditLogger.LogAudit(adminName, "GRANT_ITEM",
            playerId,
            $"发放道具 {req.ItemId} x{req.Count} (原因: {req.Reason})");

        await _playerService.GrantItem(playerId, req.ItemId, req.Count);

        return Ok(new { Message = "发放成功" });
    }

    // 封禁玩家
    [HttpPost("{playerId}/ban")]
    [AdminAuth(AdminRole.Admin)]
    public async Task<ActionResult> BanPlayer(long playerId, [FromBody] BanRequest req)
    {
        var adminName = User.Identity.Name;

        await _auditLogger.LogAudit(adminName, "BAN_PLAYER",
            playerId,
            $"封禁 {req.Duration} 分钟，原因: {req.Reason}");

        await _playerService.BanPlayer(playerId, req.Duration, req.Reason);

        // 踢掉在线连接
        await _gatewayService.KickPlayer(playerId, "您已被封禁");

        return Ok(new { Message = "封禁成功" });
    }

    // 解封
    [HttpPost("{playerId}/unban")]
    [AdminAuth(AdminRole.Admin)]
    public async Task<ActionResult> UnbanPlayer(long playerId)
    {
        var adminName = User.Identity.Name;

        await _auditLogger.LogAudit(adminName, "UNBAN_PLAYER",
            playerId, "解封");

        await _playerService.UnbanPlayer(playerId);

        return Ok(new { Message = "解封成功" });
    }

    // 禁言
    [HttpPost("{playerId}/mute")]
    [AdminAuth(AdminRole.Operator)]
    public async Task<ActionResult> MutePlayer(long playerId, [FromBody] MuteRequest req)
    {
        var adminName = User.Identity.Name;

        await _auditLogger.LogAudit(adminName, "MUTE_PLAYER",
            playerId, $"禁言 {req.Duration} 分钟");

        await _playerService.MutePlayer(playerId, req.Duration);

        return Ok(new { Message = "禁言成功" });
    }

    // 查看背包
    [HttpGet("{playerId}/inventory")]
    public async Task<ActionResult<List<InventoryItem>>> GetInventory(long playerId)
    {
        var items = await _playerService.GetPlayerInventory(playerId);
        return Ok(items);
    }

    // 查看充值记录
    [HttpGet("{playerId}/recharges")]
    public async Task<ActionResult<List<RechargeRecord>>> GetRecharges(
        long playerId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var records = await _playerService.GetRechargeRecords(
            playerId, from ?? DateTime.Now.AddMonths(-1), to ?? DateTime.Now);
        return Ok(records);
    }
}
```

### 游戏内公告

```csharp
[ApiController]
[Route("api/gm/announce")]
[AdminAuth(AdminRole.Operator)]
public class AnnounceController : ControllerBase
{
    private readonly IDatabase _redis;
    private readonly ChatService _chatService;

    // 全服广播
    [HttpPost("broadcast")]
    public async Task<ActionResult> Broadcast([FromBody] BroadcastRequest req)
    {
        var adminName = User.Identity.Name;

        // 通过 Redis Pub/Sub 广播到所有服务器
        await _redis.PublishAsync("chat:world", JsonSerializer.Serialize(new
        {
            Type = "system_broadcast",
            Content = req.Content,
            Sender = "系统"
        }));

        await _auditLogger.LogAudit(adminName, "BROADCAST", 0,
            $"全服广播: {req.Content}");

        return Ok(new { Message = "广播已发送" });
    }

    // 设置登录弹窗公告
    [HttpPost("popup")]
    public async Task<ActionResult> SetPopupAnnouncement(
        [FromBody] PopupAnnouncementRequest req)
    {
        var announcement = new PopupAnnouncement
        {
            Title = req.Title,
            Content = req.Content,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            MinLevel = req.MinLevel
        };

        // 存入 Redis，玩家登录时读取
        string key = "announcement:popup";
        await _redis.StringSetAsync(key,
            JsonSerializer.Serialize(announcement),
            TimeSpan.FromDays(30));

        return Ok(new { Message = "公告设置成功" });
    }

    // 群发邮件
    [HttpPost("mail")]
    [AdminAuth(AdminRole.Admin)]
    public async Task<ActionResult> SendMassMail([FromBody] MassMailRequest req)
    {
        var adminName = User.Identity.Name;

        MailMessage mail = new MailMessage
        {
            Title = req.Title,
            Content = req.Content,
            Attachments = req.Attachments,
            ExpireAt = DateTime.Now.AddDays(30)
        };

        int sentCount = 0;

        if (req.TargetType == MailTarget.All)
        {
            // 发给所有玩家
            sentCount = await _mailService.SendToAllPlayers(mail);
        }
        else if (req.TargetType == MailTarget.LevelRange)
        {
            // 发给指定等级范围的玩家
            sentCount = await _mailService.SendToLevelRange(
                mail, req.MinLevel, req.MaxLevel);
        }
        else if (req.TargetType == MailTarget.LastLoginBefore)
        {
            // 发给很久没登录的（召回）
            sentCount = await _mailService.SendToInactivePlayers(
                mail, req.LastLoginBefore);
        }

        await _auditLogger.LogAudit(adminName, "MASS_MAIL", 0,
            $"群发邮件 ({req.TargetType})，共 {sentCount} 人");

        return Ok(new { Message = $"邮件已发送给 {sentCount} 名玩家" });
    }
}
```

---

## 三、活动配置

```csharp
[ApiController]
[Route("api/gm/activity")]
[AdminAuth(AdminRole.Admin)]
public class ActivityController : ControllerBase
{
    private readonly IDatabase _redis;

    // 配置活动
    [HttpPost]
    public async Task<ActionResult> CreateActivity([FromBody] ActivityConfig config)
    {
        // 校验配置
        if (config.StartTime >= config.EndTime)
            return BadRequest("开始时间必须早于结束时间");

        var activityId = Guid.NewGuid().ToString();

        // 存入 Redis / MySQL
        string key = $"activity:{activityId}";
        await _redis.StringSetAsync(key,
            JsonSerializer.Serialize(config),
            TimeSpan.FromDays(365));

        // 添加到活动列表
        await _redis.SortedSetAddAsync("activities:active",
            activityId, config.StartTime.ToUnixTimeSeconds());

        return Ok(new { ActivityId = activityId });
    }

    // 开关活动
    [HttpPut("{activityId}/toggle")]
    public async Task<ActionResult> ToggleActivity(string activityId, [FromBody] bool enabled)
    {
        string key = $"activity:{activityId}";
        var data = await _redis.StringGetAsync(key);
        if (!data.HasValue)
            return NotFound();

        var config = JsonSerializer.Deserialize<ActivityConfig>(data);
        config.IsEnabled = enabled;

        await _redis.StringSetAsync(key, JsonSerializer.Serialize(config));

        // 通知游戏服务器活动状态变化
        await _redis.PublishAsync("activity:change", activityId);

        return Ok(new { ActivityId = activityId, Enabled = enabled });
    }
}

public class ActivityConfig
{
    public string Name { get; set; }
    public string Type { get; set; } // DoubleExp, DiscountShop, LoginReward
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Params { get; set; } = new(); // 活动参数
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; } = 999;
}

// 游戏服务器读取活动配置
public class ActivityService
{
    private readonly IDatabase _redis;

    public async Task<bool> IsActivityActive(string activityType)
    {
        var now = DateTimeOffset.UtcNow;

        // 扫描所有活动
        var activityIds = await _redis.SortedSetRangeByScoreAsync(
            "activities:active", 0, now.ToUnixTimeSeconds());

        foreach (var id in activityIds)
        {
            var data = await _redis.StringGetAsync($"activity:{id}");
            if (!data.HasValue) continue;

            var config = JsonSerializer.Deserialize<ActivityConfig>(data);
            if (config.Type == activityType && config.IsEnabled
                && now >= config.StartTime && now <= config.EndTime)
            {
                return true;
            }
        }

        return false;
    }

    // 获取活动参数（双倍经验倍率）
    public async Task<double> GetExpMultiplier(long playerId)
    {
        double multiplier = 1.0;

        if (await IsActivityActive("DoubleExp"))
        {
            multiplier *= 2.0;
        }

        // VIP 加成
        // 经验药水 Buff

        return multiplier;
    }
}
```

---

## 四、Webhook 与通知

```csharp
// 自动通知到企业微信/钉钉/Slack
public class AlertNotifier
{
    private readonly HttpClient _http;

    // 发送机器人通知
    public async Task NotifyAsync(string title, string message, AlertLevel level)
    {
        if (level >= AlertLevel.Warning)
        {
            await SendDingTalk(title, message);
            await SendWeChat(title, message);
        }
    }

    // 钉钉机器人
    private async Task SendDingTalk(string title, string message)
    {
        var payload = new
        {
            msgtype = "markdown",
            markdown = new
            {
                title = title,
                text = $"### {title}\n\n{message}"
            }
        };

        var json = JsonSerializer.Serialize(payload);
        await _http.PostAsync(_dingtalkWebhook,
            new StringContent(json, Encoding.UTF8, "application/json"));
    }

    // 企业微信机器人
    private async Task SendWeChat(string title, string message)
    {
        var payload = new
        {
            msgtype = "text",
            text = new
            {
                content = $"[{title}]\n{message}",
                mentioned_list = new[] { "@all" }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        await _http.PostAsync(_wechatWebhook,
            new StringContent(json, Encoding.UTF8, "application/json"));
    }
}
```

---

## 五、Sentry 异常监控

```csharp
// dotnet add package Sentry.AspNetCore

// Program.cs
builder.WebHost.UseSentry(o =>
{
    o.Dsn = "https://xxx@sentry.example.com/1";
    o.Environment = builder.Environment.EnvironmentName;
    o.TracesSampleRate = 0.1; // 10% 采样
    o.MaxRequestBodySize = Sentry.Extensibility.RequestSize.Always;
    o.SendDefaultPii = true;
});

// 自动捕获所有未处理异常
// 在 Sentry Dashboard 上可以查看：
// - 异常堆栈
// - 发生频率
// - 影响的玩家数
// - 浏览器/OS 分布
```

---

## 六、练习

1. **GM 后台**：用 ASP.NET Core 搭建一个玩家查询和管理界面
2. **审计日志**：实现 GM 操作的完整审计日志（谁，何时，对谁，做了什么）
3. **权限管理**：实现管理员角色分级（查看/操作/管理/超级管理员）
4. **活动配置**：实现从后台开启/关闭"双倍经验"活动
5. **通知集成**：配置钉钉/企业微信机器人，服务器异常时自动通知

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| GM 工具 | 运营人员管理游戏的后台系统 |
| JWT | 管理员登录认证 |
| 权限分级 | 查看者/操作员/管理员/超级管理员 |
| 审计日志 | 所有 GM 操作可追溯 |
| 活动配置 | 运营人员自助开/关活动 |
| Webhook | 异常通知到钉钉/企业微信 |
| Sentry | 实时异常监控和追踪 |
