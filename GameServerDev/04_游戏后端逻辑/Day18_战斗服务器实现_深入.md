# Day 18：战斗服务器实现 — 进阶深入

## 一、确定性帧同步实现

### 锁定步进算法

帧同步的核心是所有客户端用相同的输入、相同的逻辑产生相同的结果。服务端只负责定帧和转发输入：

```csharp
public class LockstepSimulation
{
    // 帧同步需要保证的3个确定性：
    // 1. 输入确定性：所有客户端收到相同的输入序列
    // 2. 时间确定性：每帧的执行时间一致（固定步长，不依赖真实时间）
    // 3. 数学确定性：浮点数运算一致（32位浮点在不同平台有差异）

    private int _currentFrame;
    private const int TicksPerFrame = 66; // 15 FPS, 66ms/帧
    private const int MaxPredictionFrames = 3; // 客户端预测帧数

    // 帧缓冲区（记录每帧所有玩家的输入）
    private readonly Dictionary<int, FrameData> _frameHistory = new();

    // 玩家输入确认状态
    private readonly Dictionary<long, int> _playerConfirmedFrame = new();

    public class FrameData
    {
        public int FrameNumber { get; set; }
        public Dictionary<long, byte[]> PlayerInputs { get; set; } = new();
        public bool IsConfirmed { get; set; }
    }

    // 收到玩家输入，缓存到当前帧
    public void OnPlayerInput(long playerId, byte[] inputData, int targetFrame)
    {
        lock (_frameHistory)
        {
            if (!_frameHistory.TryGetValue(targetFrame, out var frame))
            {
                frame = new FrameData { FrameNumber = targetFrame };
                _frameHistory[targetFrame] = frame;
            }

            // 同一个帧内，后到的输入覆盖先到的（防止重放攻击）
            frame.PlayerInputs[playerId] = inputData;
            _playerConfirmedFrame[playerId] = targetFrame;
        }
    }

    // 生成并广播一帧（每66ms调用一次）
    public async Task<FramePacket> GenerateFrame()
    {
        int frameNumber = Interlocked.Increment(ref _currentFrame);
        var frameData = new FrameData { FrameNumber = frameNumber };

        lock (_frameHistory)
        {
            _frameHistory[frameNumber] = frameData;
        }

        // 等待所有玩家输入（最多50ms）
        await WaitForAllPlayersInput(frameNumber, TimeSpan.FromMilliseconds(50));

        // 如果某个玩家当前帧没有输入，使用上一帧的输入（固定步长不能缺帧）
        FillMissingInputs(frameNumber);

        // 广播这一帧到所有玩家
        var packet = new FramePacket
        {
            FrameNumber = frameNumber,
            Inputs = frameData.PlayerInputs
        };

        // 清理过旧的帧（仅保留最近30帧用于断线重连）
        CleanOldFrames(frameNumber - 30);

        return packet;
    }

    // 等待所有玩家（最多等待50ms，超时则用旧输入）
    private async Task WaitForAllPlayersInput(int frameNumber, TimeSpan timeout)
    {
        var allPlayers = GetCurrentPlayers();
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < timeout)
        {
            lock (_frameHistory)
            {
                if (_frameHistory.TryGetValue(frameNumber, out var frame))
                {
                    if (allPlayers.All(p => frame.PlayerInputs.ContainsKey(p)))
                        return;
                }
            }
            await Task.Delay(1);
        }
    }

    // 为未提交输入的玩家填充上一帧的输入
    private void FillMissingInputs(int frameNumber)
    {
        lock (_frameHistory)
        {
            var frame = _frameHistory[frameNumber];
            var allPlayers = GetCurrentPlayers();

            foreach (var player in allPlayers)
            {
                if (!frame.PlayerInputs.ContainsKey(player))
                {
                    // 用上一帧的输入
                    if (_frameHistory.TryGetValue(frameNumber - 1, out var prevFrame))
                    {
                        if (prevFrame.PlayerInputs.TryGetValue(player, out var prevInput))
                        {
                            frame.PlayerInputs[player] = prevInput;
                        }
                    }
                }
            }
        }
    }
}
```

### 32位浮点确定性方案

```csharp
// C# 的 float 在 x86/x64 上通常是确定的
// 但以下操作不确定需要小心：
// 1. Math.Sin/Cos 在不同平台实现不同
// 2. 跨语言计算（C# vs C++ vs Lua）
// 3. 并行计算中的浮点累加

public class DeterministicMath
{
    // 方案1: 使用定点数（整数模拟小数）
    // 推荐用于帧同步游戏
    public struct FixedPoint
    {
        private const int FractionalBits = 16;
        private const long Scale = 1L << FractionalBits; // 65536

        private readonly long _raw;

        private FixedPoint(long raw) => _raw = raw;

        public static FixedPoint FromFloat(float value) =>
            new((long)(value * Scale));

        public float ToFloat() => (float)_raw / Scale;

        // 所有运算都是整数运算，完全确定
        public static FixedPoint operator +(FixedPoint a, FixedPoint b) =>
            new(a._raw + b._raw);

        public static FixedPoint operator -(FixedPoint a, FixedPoint b) =>
            new(a._raw - b._raw);

        public static FixedPoint operator *(FixedPoint a, FixedPoint b) =>
            new((a._raw * b._raw) >> FractionalBits);

        public static FixedPoint operator /(FixedPoint a, FixedPoint b) =>
            new((a._raw << FractionalBits) / b._raw);

        // 预计算查找表（避免 Sin/Cos 平台差异）
        private static readonly float[] SinTable = PrecomputeSinTable();

        public static FixedPoint Sin(FixedPoint angle)
        {
            // 使用查找表 + 线性插值
            int index = (int)(angle._raw * SinTable.Length / (2 * Math.PI * Scale));
            index = ((index % SinTable.Length) + SinTable.Length) % SinTable.Length;
            return FromFloat(SinTable[index]);
        }

        private static float[] PrecomputeSinTable()
        {
            var table = new float[65536];
            for (int i = 0; i < table.Length; i++)
            {
                table[i] = (float)Math.Sin(2 * Math.PI * i / table.Length);
            }
            return table;
        }
    }

    // 方案2: 确定性随机数生成器（确保所有客户端得到相同随机序列）
    public class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(ulong seed)
        {
            _state = seed;
        }

        // xorshift64* 算法（完全确定）
        public ulong Next()
        {
            _state ^= _state >> 12;
            _state ^= _state << 25;
            _state ^= _state >> 27;
            return _state * 0x2545F4914F6CDD1DUL;
        }

        public float NextFloat()
        {
            return (Next() & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }

        public int Range(int min, int max)
        {
            return min + (int)(Next() % (ulong)(max - min));
        }
    }
}
```

---

## 二、服务端权威物理验证

### 物理状态校验

即使使用帧同步，服务器也需要做物理合理性检查防止外挂：

```csharp
public class PhysicsValidator
{
    // 服务器权威移动验证（速度/加速度/穿墙）
    public class MovementValidation
    {
        private const float WalkSpeed = 4.0f, RunSpeed = 8.0f, SprintSpeed = 12.0f, MaxAccel = 20.0f;

        public ValidationResult ValidateMove(long playerId, Vector3 oldPos, Vector3 newPos, float dt, MoveState state)
        {
            float maxSpeed = state switch { MoveState.Walk => WalkSpeed, MoveState.Run => RunSpeed, MoveState.Sprint => SprintSpeed, _ => RunSpeed };
            float dist = Vector3.Distance(oldPos, newPos);
            float maxDist = maxSpeed * dt;
            if (dist > maxDist * 1.1f)
                return ValidationResult.Failed($"Speed hack: moved {dist:F2}m max {maxDist:F2}m");

            float prevVel = GetPlayerVelocity(playerId);
            float curSpeed = dist / dt;
            float accel = (curSpeed - prevVel) / dt;
            if (accel > MaxAccel * 1.2f)
                return ValidationResult.Failed($"Accel hack: {accel:F2}m/s² max {MaxAccel}m/s²");

            if (HasCollision(oldPos, newPos, GetMapColliders(state.MapId)))
                return ValidationResult.Failed("Wall hack");
            UpdatePlayerVelocity(playerId, curSpeed);
            return ValidationResult.Passed();
        }
    }

    // 位置快照校验（每500ms对比服务器权威快照）
    public class PositionSnapshotValidator
    {
        private readonly Dictionary<long, Queue<PositionSnapshot>> _history = new();
        public void OnServerSnapshot(long playerId, Vector3 pos, int frame)
        {
            if (!_history.ContainsKey(playerId)) _history[playerId] = new Queue<PositionSnapshot>();
            var q = _history[playerId];
            q.Enqueue(new PositionSnapshot { Position = pos, ServerFrame = frame, Time = DateTime.UtcNow });
            while (q.Count > 10) q.Dequeue();
        }
        public bool ValidateAgainstSnapshot(long playerId, Vector3 reportedPos, int clientFrame)
        {
            if (!_history.TryGetValue(playerId, out var snapshots)) return true;
            var nearest = snapshots.Where(s => s.ServerFrame <= clientFrame).MaxBy(s => s.ServerFrame);
            return nearest == null || Vector3.Distance(reportedPos, nearest.Position) <= 5.0f;
        }
    }
}
```

---

## 三、兴趣区域管理（AOI 进阶）

### 十字链表算法（高性能 AOI）

相比遍历所有玩家，十字链表是 MMORPG 服务器 AOI 的经典方案：

```csharp
public class CrossLinkedListAOI
{
    // AOI 节点（每个实体一个节点）
    public class AoiNode
    {
        public long EntityId { get; set; }
        public int EntityType { get; set; } // 0=玩家, 1=NPC, 2=掉落物
        public float X { get; set; }
        public float Y { get; set; }

        // X 轴链表指针
        public AoiNode XPrev { get; set; }
        public AoiNode XNext { get; set; }
        // Y 轴链表指针
        public AoiNode YPrev { get; set; }
        public AoiNode YNext { get; set; }
    }

    // X 轴和 Y 轴的头节点
    private readonly AoiNode _xHead = new();
    private readonly AoiNode _yHead = new();

    // 查找实体的视野内对象（O(sqrt(n)) 而不是 O(n)）
    public List<AoiNode> QueryRange(float centerX, float centerY, float viewRadius)
    {
        var result = new List<AoiNode>();

        // X 轴：从中心点向左右搜索 viewRadius 内的节点
        var xNode = FindNearestX(centerX);
        var candidates = new HashSet<AoiNode>();

        // 向右搜索
        var right = xNode;
        while (right != null && right.X - centerX <= viewRadius)
        {
            if (Math.Abs(right.Y - centerY) <= viewRadius)
                candidates.Add(right);
            right = right.XNext;
        }

        // 向左搜索
        var left = xNode?.XPrev;
        while (left != null && centerX - left.X <= viewRadius)
        {
            if (Math.Abs(left.Y - centerY) <= viewRadius)
                candidates.Add(left);
            left = left.XPrev;
        }

        return candidates.ToList();
    }

    // 找到 X 轴上最接近的节点（二分插入位置）
    private AoiNode FindNearestX(float x)
    {
        var current = _xHead.XNext;
        AoiNode nearest = null;
        float minDiff = float.MaxValue;

        while (current != null)
        {
            float diff = Math.Abs(current.X - x);
            if (diff < minDiff)
            {
                minDiff = diff;
                nearest = current;
            }
            current = current.XNext;
        }

        return nearest;
    }

    public void UpdateNode(long entityId, float newX, float newY, int entityType)
    {
        RemoveNode(entityId);
        var node = new AoiNode { EntityId = entityId, EntityType = entityType, X = newX, Y = newY };
        InsertSorted(node, true);
        InsertSorted(node, false);
    }

    // 在 X 或 Y 链表中按坐标有序插入
    private void InsertSorted(AoiNode node, bool isX)
    {
        var head = isX ? _xHead : _yHead;
        float coord = isX ? node.X : node.Y;
        var cur = head;
        while ((isX ? cur.XNext : cur.YNext) != null && (isX ? (cur.XNext.X < coord) : (cur.YNext.Y < coord)))
            cur = isX ? cur.XNext : cur.YNext;

        if (isX) { node.XNext = cur.XNext; node.XPrev = cur; if (cur.XNext != null) cur.XNext.XPrev = node; cur.XNext = node; }
        else { node.YNext = cur.YNext; node.YPrev = cur; if (cur.YNext != null) cur.YNext.YPrev = node; cur.YNext = node; }
    }

    public void RemoveNode(long entityId)
    {
        var cur = _xHead.XNext;
        while (cur != null && cur.EntityId != entityId) cur = cur.XNext;
        if (cur == null) return;
        if (cur.XPrev != null) cur.XPrev.XNext = cur.XNext;
        if (cur.XNext != null) cur.XNext.XPrev = cur.XPrev;
        if (cur.YPrev != null) cur.YPrev.YNext = cur.YNext;
        if (cur.YNext != null) cur.YNext.YPrev = cur.YPrev;
    }
}
```

---

## 四、战斗快照与增量压缩

### 状态快照和增量更新

```csharp
public class BattleSnapshotSystem
{
    // 列实体快照（每帧全量是基础全量+增量差异）
    // 每30帧生成全量快照，其他帧只记录增量变化

    public DeltaUpdate CreateDelta(BattleSnapshot baseFrame, BattleSnapshot newFrame)
    {
        var delta = new DeltaUpdate { BaseFrame = baseFrame.FrameNumber, TargetFrame = newFrame.FrameNumber };
        var baseDict = baseFrame.Entities.ToDictionary(e => e.EntityId);

        foreach (var ne in newFrame.Entities)
        {
            if (!baseDict.TryGetValue(ne.EntityId, out var be))
            {
                delta.Changes.Add(new EntityDelta { EntityId = ne.EntityId, X = ne.X, Y = ne.Y, Hp = ne.Hp, State = ne.State, AddedBuffs = ne.Buffs });
                continue;
            }
            var ed = new EntityDelta { EntityId = ne.EntityId };
            if (Math.Abs(ne.X - be.X) > 0.01f) ed.X = ne.X;
            if (Math.Abs(ne.Y - be.Y) > 0.01f) ed.Y = ne.Y;
            if (Math.Abs(ne.Hp - be.Hp) > 0.5f) ed.Hp = ne.Hp;
            if (ne.State != be.State) ed.State = ne.State;
            var baseBuffs = be.Buffs.Select(b => b.BuffId).ToHashSet();
            ed.AddedBuffs = ne.Buffs.Where(b => !baseBuffs.Contains(b.BuffId)).ToList();
            ed.RemovedBuffIds = be.Buffs.Where(b => !ne.Buffs.Any(nb => nb.BuffId == b.BuffId)).Select(b => b.BuffId).ToList();
            if (ed.X != null || ed.Y != null || ed.Hp != null || ed.State != null || ed.AddedBuffs.Any() || ed.RemovedBuffIds.Any())
                delta.Changes.Add(ed);
        }
        return delta;
    }
}

public class BattleSnapshot
{
    public int FrameNumber { get; set; }
    public long Timestamp { get; set; }
    public List<EntitySnapshot> Entities { get; set; } = new();
}

public class EntitySnapshot
{
    public long EntityId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Hp { get; set; }
    public float MaxHp { get; set; }
    public float Mp { get; set; }
    public int State { get; set; }
    public List<BuffSnapshot> Buffs { get; set; } = new();
}

public class DeltaUpdate
{
    public int BaseFrame { get; set; }
    public int TargetFrame { get; set; }
    public List<EntityDelta> Changes { get; set; } = new();
}

public class EntityDelta
{
    public long EntityId { get; set; }
    public float? X { get; set; }
    public float? Y { get; set; }
    public float? Hp { get; set; }
    public int? State { get; set; }
    public List<BuffSnapshot> AddedBuffs { get; set; } = new();
    public List<int> RemovedBuffIds { get; set; } = new();
}
```
```

---

## 五、外挂检测系统

### 多维作弊检测

```csharp
public class CheatDetectionSystem
{
    // 检测分数（达到阈值触发封禁）
    private readonly Dictionary<long, int> _playerSuspicionScore = new();
    private const int BanThreshold = 100;

    public enum CheatType
    {
        SpeedHack,        // 移动速度异常
        PositionHack,     // 位置瞬移
        DamageHack,       // 伤害数值篡改
        CooldownHack,     // 技能无CD
        PacketTampering,  // 封包篡改
        AutoPlay,         // 挂机脚本
        MemoryModify,     // 内存修改
    }

    // 1. 伤害外挂检测（服务器重算对比）
    public class DamageHackDetector
    {
        public async Task<DetectionResult> Detect(long playerId, DamageReport clientReport, CombatContext ctx)
        {
            var expected = RecalculateDamage(ctx);
            float diff = Math.Abs(clientReport.Damage - expected.ExpectedDamage);
            if (diff / expected.ExpectedDamage > 0.2f)
                return DetectionResult.Suspicious(CheatType.DamageHack, $"预期 {expected.ExpectedDamage} 实际 {clientReport.Damage}");

            double dps = await GetPlayerDps(playerId);
            if (dps > expected.MaxPossibleDps * 1.3)
                return DetectionResult.Suspicious(CheatType.DamageHack, $"DPS {dps:F0} 超限 {expected.MaxPossibleDps:F0}");
            return DetectionResult.Clean();
        }
    }

    // 2. 协议篡改检测（HMAC签名 + 序列号防重放）
    public class PacketTamperDetector
    {
        private readonly string _secret;
        private readonly Dictionary<long, long> _lastSeq = new();

        public bool ValidatePacket(long playerId, byte[] data, byte[] sig)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret + playerId));
            return CryptographicOperations.FixedTimeEquals(hmac.ComputeHash(data), sig);
        }

        public bool ValidateSequence(long playerId, long seqId)
        {
            if (!_lastSeq.TryGetValue(playerId, out var last)) { _lastSeq[playerId] = seqId; return true; }
            if (seqId <= last || seqId - last > 10) return false;
            _lastSeq[playerId] = seqId;
            return true;
        }
    }

    // 3. 积分系统（累积疑点到阈值自动封禁）
    public void AddSuspicion(long playerId, CheatType type, int score)
    {
        _playerSuspicionScore.TryAdd(playerId, 0);
        _playerSuspicionScore[playerId] += score;
        Log.Warning("作弊嫌疑: Player={P}, Type={T}, Total={Total}", playerId, type, _playerSuspicionScore[playerId]);
        if (_playerSuspicionScore[playerId] >= BanThreshold) _ = AutoBan(playerId, "作弊积分超限");
    }

    private async Task AutoBan(long playerId, string reason)
    {
        await _db.ExecuteAsync("UPDATE players SET status = 2, ban_reason = @R, ban_time = NOW() WHERE id = @P", new { R = reason, P = playerId });
        await _onlineManager.KickPlayer(playerId, reason);
    }
}
```

---

## 六、回放系统

### 高效录制与播放

```csharp
public class ReplaySystem
{
    // 录制器：帧数据 GZip 压缩 → Protobuf 序列化 → 对象存储
    public class ReplayRecorder
    {
        private readonly List<byte[]> _frames = new();
        private readonly long _battleId;

        public ReplayRecorder(long battleId) => _battleId = battleId;

        public void RecordFrame(FramePacket frame)
        {
            if (_frames.Count >= 1800) return; // 15分钟 @ 2fps
            using var ms = new MemoryStream();
            using (var gzip = new GZipStream(ms, CompressionLevel.Fastest))
                ProtoBuf.Serializer.Serialize(gzip, frame);
            _frames.Add(ms.ToArray());
        }

        public async Task<string> SaveReplay()
        {
            using var ms = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ms, new ReplayData { BattleId = _battleId, FrameData = _frames });
            string key = $"replays/{_battleId}.binpb";
            await _objectStorage.PutAsync(key, ms.ToArray());
            Log.Information("回放保存: Battle={B}, Size={Size}KB", _battleId, ms.Length / 1024);
            return key;
        }
    }

    // 播放器：按帧范围流式返回
    public class ReplayPlayer
    {
        public async IAsyncEnumerable<FramePacket> PlayReplay(long battleId, int start, int end)
        {
            byte[] data = await _objectStorage.GetAsync($"replays/{battleId}.binpb");
            var replay = ProtoBuf.Serializer.Deserialize<ReplayData>(new MemoryStream(data));
            for (int i = start; i < Math.Min(end, replay.FrameData.Count); i++)
            {
                using var ms = new MemoryStream(replay.FrameData[i]);
                using var gzip = new GZipStream(ms, CompressionMode.Decompress);
                yield return ProtoBuf.Serializer.Deserialize<FramePacket>(gzip);
            }
        }
    }
}

public class ReplayData
{
    public long BattleId { get; set; }
    public List<byte[]> FrameData { get; set; } = new();
}
```

---

## 七、匹配系统集成与迟入机制

### 匹配到战斗的无缝连接

```csharp
public class BattleInitService
{
    public async Task<BattleInstance> CreateBattleFromMatch(MatchResult match)
    {
        var battle = new BattleInstance
        {
            BattleId = Snowflake.NextId(),
            MapId = match.MapId,
            Mode = match.GameMode,
            SceneServerId = _sceneAllocator.Allocate(match.MapId, match.Players.Count),
            State = BattleState.WaitingForPlayers
        };

        await Task.WhenAll(match.Players.Select(p => SendToPlayer(p.PlayerId, MsgId.BattleStart, new BattleStartNotification
        {
            BattleId = battle.BattleId, SceneServerId = battle.SceneServerId, MapId = battle.MapId,
            Players = match.Players.Select(mp => new BattlePlayerInfo { PlayerId = mp.PlayerId, Name = mp.Name, TeamId = mp.TeamId }).ToList(),
            CountdownSeconds = 10, Seed = battle.RandomSeed
        })));

        battle.State = BattleState.Countdown;
        _ = StartCountdownAndBegin(battle);
        return battle;
    }
}
```

### 迟入者状态追赶

```csharp
public class LateJoinerHandler
{
    // 处理玩家战斗中掉线重连
    public async Task<CatchUpPackage> HandleLateJoiner(
        long playerId, long battleId, int currentFrame)
    {
        // 1. 获取战斗状态
        var battle = await GetBattleInstance(battleId);
        if (battle == null)
            throw new BusinessException("战斗不存在");

        // 2. 获取最近的全量快照
        var snapshot = await GetNearestSnapshot(battleId, currentFrame);

        // 3. 从快照帧之后的所有增量帧
        var deltas = await GetDeltaFrames(
            battleId, snapshot.FrameNumber + 1, currentFrame);

        // 4. 组装追赶包
        var catchUp = new CatchUpPackage
        {
            BattleId = battleId,
            SnapshotFrame = snapshot.FrameNumber,
            Snapshot = snapshot,
            PendingDeltas = deltas,
            CurrentFrame = currentFrame,
            PlayerId = playerId,
            // 重连玩家的战斗单位当前状态
            MyUnit = ExtractPlayerUnit(snapshot, deltas, playerId)
        };

        Log.Information("迟入者追赶: Player={Player}, Battle={Battle}, " +
            "Snapshot={Frame}, Deltas={Count}",
            playerId, battleId, snapshot.FrameNumber, deltas.Count);

        return catchUp;
    }

    // 获取最近的全量快照（每30帧或每2秒存一次）
    private async Task<BattleSnapshot> GetNearestSnapshot(
        long battleId, int targetFrame)
    {
        // 从快照存储中查找最接近的快照
        for (int f = targetFrame; f >= 0; f -= 30)
        {
            string key = $"battle_snapshot:{battleId}:{f}";
            var data = await _redis.StringGetAsync(key);
            if (data.HasValue)
            {
                return JsonSerializer.Deserialize<BattleSnapshot>(data);
            }
        }

        return null; // 没有快照，不可能
    }

    private async Task<List<DeltaUpdate>> GetDeltaFrames(
        long battleId, int fromFrame, int toFrame)
    {
        var deltas = new List<DeltaUpdate>();
        for (int f = fromFrame; f <= toFrame; f++)
        {
            string key = $"battle_delta:{battleId}:{f}";
            var data = await _redis.StringGetAsync(key);
            if (data.HasValue)
            {
                deltas.Add(JsonSerializer.Deserialize<DeltaUpdate>(data));
            }
        }
        return deltas;
    }
}
```

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| 确定性帧同步 | 固定步长，所有客户端用相同输入产生相同结果 |
| 定点数运算 | 用整数模拟小数，避免浮点跨平台不一致 |
| 权威物理验证 | 服务器校验速度/加速度/穿墙，拒绝异常移动 |
| 十字链表 AOI | 双链表按 X/Y 轴排序，O(sqrt(n)) 范围查询 |
| 增量压缩 | 只传变化字段，减少 60-80% 的带宽 |
| 外挂检测 | 多维嫌疑分数累积，过阈值自动封禁 |
| 回放系统 | 帧数据压缩存入对象存储，支持任意帧回放 |
| 迟入者追赶 | 全量快照 + 增量帧，让重连玩家快速同步状态 |
