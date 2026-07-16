# Day 17：玩家账号与登录系统 — 进阶深入

## 一、OAuth 2.0 / OpenID Connect 流程

### 标准授权码流程（适用第三方登录）

游戏客户端通常需要通过"外部浏览器"或 SDK 完成 OAuth 流程：

```
用户端 → 游戏服 → 第三方（授权码流程）：
  1. 请求第三方登录 → 游戏服构造授权URL返回
  2. 打开浏览器/SDK 登录，用户授权
  3. 第三方返回 auth_code → 游戏服
  4. 游戏服换 access_token → 获取用户信息
  5. 登录成功，下发游戏 token
```

```csharp
public class OAuthService
{
    // 为不同平台注册的客户端信息
    private readonly Dictionary<PlatformType, OAuthClientConfig> _clients = new()
    {
        [PlatformType.Steam] = new()
        {
            ClientId = "steam_app_id",
            ClientSecret = "", // Steam 不需要 secret
            AuthUrl = "https://steamcommunity.com/openid/login",
            TokenUrl = "https://api.steampowered.com/ISteamUserAuth/AuthenticateUserTicket/v1/",
            UserInfoUrl = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/",
            Scope = "openid profile"
        },
        [PlatformType.Apple] = new()
        {
            ClientId = "com.yourgame.apple",
            ClientSecret = GenerateAppleClientSecret(),
            AuthUrl = "https://appleid.apple.com/auth/authorize",
            TokenUrl = "https://appleid.apple.com/auth/token",
            UserInfoUrl = "", // Apple 的 id_token 直接包含用户信息
            Scope = "openid email name"
        },
        [PlatformType.Google] = new()
        {
            ClientId = "123456789-xxxxx.apps.googleusercontent.com",
            ClientSecret = "GOCSPX-xxxxxxxx",
            AuthUrl = "https://accounts.google.com/o/oauth2/v2/auth",
            TokenUrl = "https://oauth2.googleapis.com/token",
            UserInfoUrl = "https://www.googleapis.com/oauth2/v3/userinfo",
            Scope = "openid email profile"
        }
    };

    public async Task<OAuthTokenResult> ExchangeAuthCode(PlatformType platform, string authCode)
    {
        var config = _clients[platform];
        using var http = new HttpClient();
        var resp = await http.PostAsync(config.TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code", ["code"] = authCode,
            ["client_id"] = config.ClientId, ["client_secret"] = config.ClientSecret,
            ["redirect_uri"] = config.RedirectUri
        }));
        var tokenData = JsonSerializer.Deserialize<OAuthTokenResponse>(await resp.Content.ReadAsStringAsync());
        var userInfo = await GetUserInfo(platform, tokenData.AccessToken);
        return new OAuthTokenResult { PlatformUserId = userInfo.Sub, Platform = platform,
            AccessToken = tokenData.AccessToken, RefreshToken = tokenData.RefreshToken, UserName = userInfo.Name };
    }

    // Apple client_secret: ES256 JWT 用私钥签名
    private string GenerateAppleClientSecret()
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(File.ReadAllBytes("config/AuthKey_XXXXXX.p8"), out _);
        var jwt = new JwtSecurityToken("TEAMID",
            claims: new[] { new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()), new Claim("exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds().ToString()) },
            signingCredentials: new SigningCredentials(new ECDsaSecurityKey(ecdsa), SecurityAlgorithms.EcdsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
```

---

## 二、JWT 的 RSA/ECDSA 签名

### 对称签名 vs 非对称签名

```csharp
public class JwtTokenFactory
{
    // 方式1: HMAC-SHA256（对称签名）
    // 优点：性能快
    // 缺点：签发和验证用同一个密钥，不适合多服务
    private readonly string _hmacSecret = "your-256-bit-secret";

    public string CreateHmacToken(long playerId, string role)
    {
        var claims = new[]
        {
            new Claim("sub", playerId.ToString()),
            new Claim("role", role),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new Claim("exp", DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "GameServer",
            audience: "GameClient",
            claims: claims,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_hmacSecret)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // 方式2: RSA-256（非对称签名）
    // 优点：私钥在登录服，公钥给所有网关/游戏服
    // 缺点：性能比 HMAC 慢
    private static readonly RSA _rsaPrivateKey;
    private static readonly RSA _rsaPublicKey;

    static JwtTokenFactory()
    {
        _rsaPrivateKey = RSA.Create(2048);
        // 生产环境从文件/密钥管理服务加载
        _rsaPrivateKey.ImportFromPem(File.ReadAllText("keys/private.pem"));
        _rsaPublicKey = RSA.Create();
        _rsaPublicKey.ImportFromPem(File.ReadAllText("keys/public.pem"));
    }

    public string CreateRsaToken(long playerId, string extraData)
    {
        var claims = new[]
        {
            new Claim("sub", playerId.ToString()),
            new Claim("data", extraData),
            new Claim("exp", DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "LoginServer",
            audience: "GameServers",
            claims: claims,
            signingCredentials: new SigningCredentials(
                new RsaSecurityKey(_rsaPrivateKey),
                SecurityAlgorithms.RsaSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // 方式3: ECDSA-256（非对称签名，更安全更快速）
    // 推荐：比 RSA 更短的密钥，更快的签名速度
    private static readonly ECDsa _ecdsaKey;

    static JwtTokenFactory()
    {
        _ecdsaKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        _ecdsaKey.ImportFromPem(File.ReadAllText("keys/ec_private.pem"));
    }

    public string CreateEcdsaToken(long playerId)
    {
        var claims = new[]
        {
            new Claim("sub", playerId.ToString()),
            new Claim("exp", DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "LoginServer",
            audience: "GameServers",
            claims: claims,
            signingCredentials: new SigningCredentials(
                new ECDsaSecurityKey(_ecdsaKey),
                SecurityAlgorithms.EcdsaSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// 验证方（网关/游戏服务器）只用公钥
public class JwtTokenValidator
{
    private readonly TokenValidationParameters _validationParams;

    public JwtTokenValidator(string publicKeyPem)
    {
        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(publicKeyPem);

        _validationParams = new TokenValidationParameters
        {
            ValidIssuer = "LoginServer",
            ValidAudience = "GameServers",
            IssuerSigningKey = new ECDsaSecurityKey(ecdsa),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30) // 允许30秒时钟偏差
        };
    }

    public ClaimsPrincipal Validate(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, _validationParams, out _);
    }
}
```

---

## 三、Refresh Token 轮转策略

### 双 Token 机制

```
Access Token（短效）:
  - 有效期: 15-30分钟
  - 用途: 每次API请求的认证
  - 风险: 泄露后危害时间短

Refresh Token（长效）:
  - 有效期: 7-30天
  - 用途: 换取新的 Access Token
  - 风险: 需要更严格的保护

轮转（Rotation）:
  - 每次使用 Refresh Token 时，旧的立即失效，发新的
  - 如果旧的 Refresh Token 被重用（泄露），服务端可检测
```

```csharp
public class RefreshTokenService
{
    private readonly IDatabase _redis;

    // 生成 Refresh Token
    public async Task<RefreshTokenResult> CreateRefreshToken(long playerId)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var familyId = Guid.NewGuid().ToString("N"); // token 家族ID

        var tokenEntry = new RefreshTokenEntry
        {
            Token = token,
            FamilyId = familyId,
            PlayerId = playerId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            Used = false
        };

        // 存储 token 信息
        await _redis.HashSetAsync($"refresh_token:{token}", new HashEntry[]
        {
            new("family", familyId),
            new("player_id", playerId),
            new("created", tokenEntry.CreatedAt.Ticks),
            new("used", "false")
        });
        await _redis.KeyExpireAsync($"refresh_token:{token}", TimeSpan.FromDays(31));

        // 记录家族信息（用于检测泄露）
        await _redis.StringSetAsync(
            $"refresh_family:{familyId}",
            JsonSerializer.Serialize(new
            {
                PlayerId = playerId,
                CreatedAt = tokenEntry.CreatedAt,
                LastUsedAt = DateTime.UtcNow,
                Version = 1
            }));

        return new RefreshTokenResult { Token = token, ExpiresAt = tokenEntry.ExpiresAt };
    }

    // 轮转 Refresh Token
    public async Task<RefreshTokenResult> RotateRefreshToken(
        string oldToken, long expectedPlayerId)
    {
        // 1. 检查旧的 token
        var info = await _redis.HashGetAllAsync($"refresh_token:{oldToken}");
        if (info.Length == 0) throw new AuthException("Refresh token 无效");

        var familyId = info.FirstOrDefault(h => h.Name == "family").Value;
        var playerId = long.Parse(info.FirstOrDefault(h => h.Name == "player_id").Value);
        var used = info.FirstOrDefault(h => h.Name == "used").Value == "true";

        if (playerId != expectedPlayerId) throw new AuthException("Token 不属于此用户");

        // 检测 Token 泄露：已被使用过则使整个家族失效
        if (used) { await HandleTokenLeak(familyId, playerId); throw new AuthException("Refresh token 已被使用，可能是泄露"); }

        await _redis.HashSetAsync($"refresh_token:{oldToken}", new HashEntry("used", "true"));
        await _redis.KeyExpireAsync($"refresh_token:{oldToken}", TimeSpan.FromDays(1));
        return await CreateRefreshToken(playerId);
    }

    // 检测到泄露：使整个家族失效
    private async Task HandleTokenLeak(string familyId, long playerId)
    {
        Log.Warning("Refresh token 泄露检测: Player={Player}, Family={Family}",
            playerId, familyId);

        // 该家族所有 token 不可用
        // 通知用户：账号可能被盗，建议修改密码
        await NotifyPlayerSecurityAlert(playerId);

        // 记录安全事件
        await _db.ExecuteAsync(
            "INSERT INTO security_events (player_id, event_type, detail) " +
            "VALUES (@P, 'token_leak', @D)",
            new { P = playerId, D = $"Refresh token family {familyId} compromised" });
    }
}

public class RefreshTokenResult
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

### 登录流程整合

```csharp
public class LoginSessionService
{
    // 完整登录流程
    public async Task<LoginSessionResult> Login(string platform, string authCode)
    {
        // 1. OAuth 认证
        var oauthResult = await _oauthService.ExchangeAuthCode(ParsePlatform(platform), authCode);

        // 2. 查找或创建账号
        var accountId = await FindOrCreateAccount(oauthResult);

        // 3. 签发 Access Token (15分钟)
        string accessToken = _jwtFactory.CreateEcdsaToken(accountId);

        // 4. 签发 Refresh Token (30天)
        var refreshResult = await _refreshTokenService.CreateRefreshToken(accountId);

        // 5. 返回会话
        return new LoginSessionResult
        {
            AccountId = accountId,
            AccessToken = accessToken,
            AccessTokenExpiresIn = 900,
            RefreshToken = refreshResult.Token,
            RefreshTokenExpiresAt = refreshResult.ExpiresAt,
            IsNewAccount = oauthResult.IsNewAccount
        };
    }

    // 续期流程
    public async Task<LoginSessionResult> RefreshAccessToken(string refreshToken)
    {
        var rotated = await _refreshTokenService.RotateRefreshToken(refreshToken, GetCurrentPlayerId());
        string newAccessToken = _jwtFactory.CreateEcdsaToken(rotated.PlayerId);

        return new LoginSessionResult
        {
            AccountId = rotated.PlayerId,
            AccessToken = newAccessToken,
            AccessTokenExpiresIn = 900,
            RefreshToken = rotated.Token,
            RefreshTokenExpiresAt = rotated.ExpiresAt
        };
    }
}
```

---

## 四、多平台登录绑定

### 账号绑定架构

```csharp
public class AccountBindingService
{
    // 账号绑定表结构 (MySQL)
    // CREATE TABLE account_bindings (
    //     id BIGINT AUTO_INCREMENT PRIMARY KEY,
    //     account_id BIGINT NOT NULL,
    //     platform VARCHAR(20) NOT NULL,    -- steam/apple/google/device
    //     platform_user_id VARCHAR(128) NOT NULL,  -- 平台用户ID
    //     platform_user_name VARCHAR(64),   -- 平台用户名
    //     bind_time DATETIME NOT NULL,
    //     UNIQUE KEY uk_platform (platform, platform_user_id),
    //     INDEX idx_account (account_id)
    // );

    // 将平台账号绑定到游戏账号
    public async Task<BindingResult> BindPlatform(
        long accountId, PlatformType platform, string authCode)
    {
        // 1. 通过 OAuth 获取平台用户ID
        var oauthResult = await _oauthService.ExchangeAuthCode(platform, authCode);

        // 2. 检查该平台账号是否已被其他游戏账号绑定
        var existingBinding = await _db.QueryFirstOrDefaultAsync(
            "SELECT account_id FROM account_bindings " +
            "WHERE platform = @P AND platform_user_id = @U",
            new { P = platform.ToString(), U = oauthResult.PlatformUserId });

        if (existingBinding != null)
        {
            if ((long)existingBinding.account_id == accountId)
                return BindingResult.AlreadyBound;
            return BindingResult.AlreadyBoundByOther;
        }

        // 3. 限制每个平台的绑定数量
        int bindCount = await _db.QueryFirstAsync<int>(
            "SELECT COUNT(*) FROM account_bindings WHERE account_id = @A",
            new { A = accountId });

        if (bindCount >= _maxBindings)
            return BindingResult.TooManyBindings;

        // 4. 写入绑定关系
        await _db.ExecuteAsync(
            "INSERT INTO account_bindings (account_id, platform, platform_user_id, platform_user_name) " +
            "VALUES (@A, @P, @U, @N)",
            new
            {
                A = accountId,
                P = platform.ToString(),
                U = oauthResult.PlatformUserId,
                N = oauthResult.UserName ?? ""
            });

        return BindingResult.Success;
    }

    // 解绑（需要安全验证）
    public async Task<BindingResult> UnbindPlatform(long accountId, PlatformType platform)
    {
        // 检查是否至少保留一种登录方式
        int bindCount = await _db.QueryFirstAsync<int>(
            "SELECT COUNT(*) FROM account_bindings WHERE account_id = @A",
            new { A = accountId });

        if (bindCount <= 1)
            return BindingResult.LastBindingCannotUnbind;

        await _db.ExecuteAsync(
            "DELETE FROM account_bindings WHERE account_id = @A AND platform = @P",
            new { A = accountId, P = platform.ToString() });

        return BindingResult.Success;
    }

    // 通过任意平台登录
    public async Task<long> LoginWithAnyPlatform(PlatformType platform, string authCode)
    {
        var oauthResult = await _oauthService.ExchangeAuthCode(platform, authCode);

        var binding = await _db.QueryFirstOrDefaultAsync<AccountBinding>(
            "SELECT account_id FROM account_bindings " +
            "WHERE platform = @P AND platform_user_id = @U",
            new { P = platform.ToString(), U = oauthResult.PlatformUserId });

        if (binding == null)
            throw new AuthException("该平台账号未绑定游戏账号，请先注册或绑定");

        return binding.AccountId;
    }
}
```

### Apple 和 Google 的特殊处理

```csharp
// Apple 登录注意点：
// 1. Apple 要求必须提供"隐藏邮件"选项
// 2. 用户删除 Apple ID 后，你的 app 必须删除对应数据
// 3. Apple 的 JWT (id_token) 中 sub 是匿名化的用户标识

// Google 登录注意点：
// 1. Google 的 sub 是固定的用户标识，不会变
// 2. 推荐用 Google 的 id_token 而非 access_token 做认证
// 3. 验证 id_token 时需要校验 Google 的公钥

public class GoogleTokenVerifier
{
    // Google 公钥端点
    private const string GoogleCertsUrl = "https://www.googleapis.com/oauth2/v3/certs";
    private static readonly ConcurrentDictionary<string, ECDsa> _googlePublicKeys = new();

    public async Task<GoogleUserInfo> VerifyGoogleIdToken(string idToken)
    {
        // 1. 获取 Google 公钥
        await RefreshGoogleCerts();

        // 2. 无签名验证解析 header（获取 kid）
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);
        var kid = jwt.Header.Kid;

        if (!_googlePublicKeys.TryGetValue(kid, out var publicKey))
            throw new AuthException("Unknown Google signing key");

        // 3. 验证签名
        var validationParams = new TokenValidationParameters
        {
            IssuerSigningKey = new ECDsaSecurityKey(publicKey),
            ValidIssuer = "https://accounts.google.com",
            ValidAudience = _googleClientId,
            ValidateLifetime = true
        };

        var claims = handler.ValidateToken(idToken, validationParams, out _);

        return new GoogleUserInfo
        {
            Sub = claims.FindFirst("sub").Value,
            Email = claims.FindFirst("email")?.Value,
            Name = claims.FindFirst("name")?.Value,
            Picture = claims.FindFirst("picture")?.Value
        };
    }

    private async Task RefreshGoogleCerts()
    {
        // 每24小时刷新一次
        using var http = new HttpClient();
        var json = await http.GetStringAsync(GoogleCertsUrl);
        var certs = JsonSerializer.Deserialize<GoogleCertsResponse>(json);

        foreach (var key in certs.Keys)
        {
            var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(
                Convert.FromBase64String(key.X5c[0]), out _);
            _googlePublicKeys[key.Kid] = ecdsa;
        }
    }
}
```

---

## 五、设备指纹与防欺诈

### 设备指纹生成

```csharp
public class DeviceFingerprintService
{
    // 客户端上报的设备信息
    public class DeviceInfo
    {
        public string DeviceId { get; set; }       // IDFA / OAID
        public string DeviceModel { get; set; }
        public string OsVersion { get; set; }
        public string ScreenResolution { get; set; }
        public string GpuInfo { get; set; }
        public string CpuInfo { get; set; }
        public string Timezone { get; set; }
        public string Language { get; set; }
    }

    // 服务端哈希生成指纹
    public string GenerateFingerprint(DeviceInfo info)
    {
        var raw = string.Join("|", info.DeviceModel, info.ScreenResolution, info.GpuInfo, info.CpuInfo, info.Timezone, info.Language);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLower()[..32];
    }

    // 设备风险评分：多设备切换 + 多账号共享 + VPN/模拟器检测
    public async Task<DeviceRiskLevel> AssessDeviceRisk(long playerId, DeviceInfo deviceInfo, string ip)
    {
        string fp = GenerateFingerprint(deviceInfo);
        int riskScore = 0;

        var lastFp = await _redis.StringGetAsync($"device:{playerId}:last");
        if (lastFp.HasValue && lastFp != fp)
        {
            var lastTime = await _redis.StringGetAsync($"device:{playerId}:last_time");
            if (lastTime.HasValue && (DateTime.UtcNow - DateTime.Parse(lastTime)).TotalHours < 1)
                riskScore += 30;
        }

        long accountCount = await _redis.SetLengthAsync($"device_accounts:{fp}");
        if (accountCount > 5) riskScore += 40;
        if (await IsVpnOrProxy(ip)) riskScore += 20;
        if (IsEmulator(deviceInfo)) riskScore += 30;

        await _redis.StringSetAsync($"device:{playerId}:last", fp);
        await _redis.StringSetAsync($"device:{playerId}:last_time", DateTime.UtcNow.ToString("O"));
        await _redis.SetAddAsync($"device_accounts:{fp}", playerId.ToString());

        return new DeviceRiskLevel
        {
            Fingerprint = fingerprint,
            RiskScore = riskScore,
            Level = riskScore switch
            {
                < 30 => RiskLevel.Low,
                < 60 => RiskLevel.Medium,
                < 80 => RiskLevel.High,
                _ => RiskLevel.Critical
            }
        };
    }

    // 模拟器检测
    private bool IsEmulator(DeviceInfo info)
    {
        var emulatorSigns = new[]
        {
            "generic", "sdk_phone", "emulator", "android_x86",
            "vbox", "qemu", "goldfish"
        };

        return emulatorSigns.Any(sign =>
            info.DeviceModel.Contains(sign, StringComparison.OrdinalIgnoreCase) ||
            info.CpuInfo.Contains(sign, StringComparison.OrdinalIgnoreCase));
    }
}
```

---

## 六、登录限流

### 双维度限流（IP + 账号）

```csharp
public class LoginRateLimiter
{
    // 三级别限流：IP → 账号 → 全局
    public async Task<RateLimitResult> CheckLoginRateLimit(string ip, string accountName)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (await CountRecentAttempts($"login:ip:{ip}", now) > 20)
            return RateLimitResult.Blocked("IP 临时封禁 5 分钟");
        if (await CountRecentAttempts($"login:account:{accountName}", now) > 5)
            return RateLimitResult.Blocked("登录尝试过多，15 分钟后重试");

        long global = await _redis.StringIncrementAsync($"login:global:{now / 60}");
        await _redis.KeyExpireAsync($"login:global:{now / 60}", TimeSpan.FromMinutes(2));
        if (global > 10000) return RateLimitResult.Blocked("登录服务繁忙");
        return RateLimitResult.Allowed();
    }

    private async Task<int> CountRecentAttempts(string key, long now)
    {
        await _redis.SortedSetRemoveRangeByScoreAsync(key, 0, now - 60);
        return (int)await _redis.SortedSetLengthAsync(key);
    }

    public async Task RecordLoginAttempt(string ip, string accountName, bool success)
    {
        if (!success)
        {
            var batch = _redis.CreateBatch();
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            batch.SortedSetAddAsync($"login:ip:{ip}", Guid.NewGuid().ToString(), now);
            batch.KeyExpireAsync($"login:ip:{ip}", TimeSpan.FromMinutes(5));
            batch.SortedSetAddAsync($"login:account:{accountName}", Guid.NewGuid().ToString(), now);
            batch.KeyExpireAsync($"login:account:{accountName}", TimeSpan.FromMinutes(5));
            batch.Execute();
        }
        await _db.ExecuteAsync("INSERT INTO login_logs (ip, account_name, success, login_time) VALUES (@I, @A, @S, NOW())", new { I = ip, A = accountName, S = success });
    }

    public async Task<bool> IsBlocked(string ip, string accountName) =>
        await _redis.KeyExistsAsync($"blocked:login:ip:{ip}") || await _redis.KeyExistsAsync($"blocked:login:account:{accountName}");
}
```

---

## 七、GDPR 合规要求

### 数据导出与删除

```csharp
public class GdprService
{
    // 数据导出：查询所有个人数据 → JSON → 安全存储 → 过期下载链接
    public async Task<GdprDataPackage> ExportPlayerData(long playerId)
    {
        var pkg = new GdprDataPackage { ExportTime = DateTime.UtcNow, PlayerData = new() };
        pkg.PlayerData["profile"] = await _db.QueryFirstAsync("SELECT id, name, level, exp, gold, created_at FROM players WHERE id = @Id", new { Id = playerId });
        pkg.PlayerData["account_bindings"] = await _db.QueryAsync("SELECT platform, platform_user_id, bind_time FROM account_bindings WHERE account_id = @A", new { A = playerId });
        pkg.PlayerData["payment_history"] = await _db.QueryAsync("SELECT order_id, amount, currency, status, created_at FROM payments WHERE player_id = @P", new { P = playerId });
        string json = JsonSerializer.Serialize(pkg, new JsonSerializerOptions { WriteIndented = true });
        await UploadToSecureStorage(playerId, json);
        return pkg;
    }

    // 数据删除（被遗忘权）：匿名化身份 + 删除角色/社交/聊天数据，保留充值记录（税务要求）
    public async Task DeletePlayerData(long playerId)
    {
        using var tx = await _db.BeginTransactionAsync();
        try
        {
            await _db.ExecuteAsync("UPDATE players SET name = CONCAT('deleted_', id), email = '', phone = '' WHERE id = @Id", new { Id = playerId }, tx);
            await _db.ExecuteAsync("DELETE FROM inventory WHERE player_id = @P", new { P = playerId }, tx);
            await _db.ExecuteAsync("DELETE FROM skills WHERE player_id = @P", new { P = playerId }, tx);
            await _db.ExecuteAsync("DELETE FROM friends WHERE player_id = @P OR friend_id = @P", new { P = playerId }, tx);
            await _db.ExecuteAsync("DELETE FROM guild_members WHERE player_id = @P", new { P = playerId }, tx);
            await _db.ExecuteAsync("DELETE FROM mail WHERE receiver_id = @P", new { P = playerId }, tx);
            await _db.ExecuteAsync("DELETE FROM chat_logs WHERE player_id = @P", new { P = playerId }, tx);
            var batch = _redis.CreateBatch();
            batch.KeyDeleteAsync($"player:{playerId}"); batch.KeyDeleteAsync($"inventory:{playerId}"); batch.Execute();
            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| OAuth 2.0 | 授权码模式，客户端拿code换access_token |
| JWT 签名 | ECDSA比RSA更快密钥更短，推荐用于游戏服务 |
| Refresh Token 轮转 | 每次使用刷新token时发新token，检测泄露 |
| 多平台绑定 | 一个游戏账号可以绑定Steam/Apple/Google |
| 设备指纹 | 硬件特征哈希，检测多开/工作室/脚本 |
| 登录限流 | IP+账号双重限流，滑动窗口计数器 |
| GDPR | 玩家有权导出个人数据或被遗忘（删除） |
