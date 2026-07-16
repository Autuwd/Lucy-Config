# Day 18：网络通信 — KCP 协议、预测与同步、带宽优化

## 0. 为什么需要这些高级技术？

基础网络概念（TCP/UDP、心跳、重连）解决了"能不能连上"的问题，但真实游戏中还有更难的挑战：

| 问题 | 表现 | 原因 |
|------|------|------|
| 延迟 | 开枪 0.5 秒后才有反应 | 物理距离、路由 |
| 抖动 | 位置忽前忽后 | 网络不稳定 |
| 丢包 | 玩家瞬移 | UDP 丢包 |
| 带宽 | 手机流量不够用 | 数据太大 |

解决方案不是某一项技术，而是**一整套组合拳**：

```
低延迟传输  ← KCP（可靠 UDP）
平滑表现    ← 插值 + 预测
公平判定    ← 服务器回滚 + 延迟补偿
节省带宽    ← 压缩 + 差量更新 + 频率控制
```

对比 C++ 网络开发：C++ 游戏服务器常用 enet、libuv、muduo 等库，Unity 中这些技术需要自己实现或使用第三方库。核心思路一致。

---

## 1. KCP 可靠 UDP 协议

### 为什么不用 TCP？

```
TCP 的问题（对实时游戏来说）：
1. 队头阻塞（Head-of-line blocking）：丢一个包，后面的全等
2. 拥塞控制：TCP 发现丢包会降低发送速度（对视频流好，对游戏是灾难）
3. 重传慢：RTO（超时重传）最小 200ms，游戏等不起

KCP 的改进：
1. 快速重传：收到一个包后立刻发 ACK，不等定时器
2. 选择性重传：只重传真正丢了的包
3. 流量控制简化：不降速，以最低延迟为目标
```

### KCP 核心机制

```
KCP 在 UDP 之上加了一层：
┌──────────────────────────────────┐
│  应用层（你的游戏协议）            │
├──────────────────────────────────┤
│  KCP 层（可靠 UDP）               │
│  - 序列号 + ACK                  │
│  - 快速重传 + 选择性重传          │
│  - 流量控制（可配置）             │
├──────────────────────────────────┤
│  UDP 层（不可靠传输）              │
├──────────────────────────────────┤
│  IP 层                           │
└──────────────────────────────────┘
```

### C# KCP 实现核心

```csharp
// KCP 的核心数据结构
public class KCP
{
    // 发送缓冲区
    private Queue<Segment> sndQueue = new();
    // 已发送但未确认的包（等待 ACK）
    private List<Segment> sndBuffer = new();
    // 接收缓冲区（等待应用层取走）
    private List<Segment> rcvBuffer = new();
    // 接收队列（已按序排列，可直接交付）
    private Queue<Segment> rcvQueue = new();

    // 重要参数（可配置）
    public int nodelay;     // 0: 标准模式, 1: 极速模式
    public int interval;    // 内部更新间隔（ms）
    public int resend;      // 快速重传触发阈值（丢包次数）
    public int nc;          // 是否关闭流量控制

    // 序列号
    private uint sndNext;   // 下一个发送序列号
    private uint rcvNext;   // 期望接收的序列号
    private uint sndUna;    // 第一个未确认的序列号
}

// KCP 数据段结构
public struct Segment
{
    public uint conv;       // 会话 ID
    public uint sn;         // 序列号
    public uint una;        // 未确认的序列号
    public uint len;        // 数据长度
    public byte[] data;     // 数据
    public uint resendts;   // 下次重传时间戳
    public uint rto;        // 超时重传时间
    public int fastack;     // 快速确认计数
    public int xmit;        // 已重传次数
}
```

### KCP 发送与重传逻辑

```csharp
public class KcpSession
{
    private KCP kcp;
    private UdpClient udp;

    // 发送数据
    public void Send(byte[] data)
    {
        kcp.Send(data);  // 分段、编号、加入发送队列
        kcp.Flush();     // 立即发送缓冲区中的数据
    }

    // KCP 内部发送逻辑
    public void Flush()
    {
        // 1. 把 sndQueue 中的分段移到 sndBuffer
        // 2. 发送 sndBuffer 中未发送的分段
        // 3. 检查重传：
        //    - 超过 RTO 的 → 重传
        //    - fastack >= resend 的 → 快速重传（不等超时）
        // 4. 发送 ACK
    }

    // 收到数据时调用
    public void Input(byte[] rawPacket)
    {
        // 1. 解析 UDP 包
        // 2. 更新 rcvBuffer
        // 3. 发送 ACK
        // 4. 尝试把 rcvBuffer 中连续的包移到 rcvQueue
        // 5. 更新 rcvNext
    }

    // 正确收到数据段
    private void ParseSegment(Segment seg)
    {
        // 检查序列号是否连续
        if (seg.sn == rcvNext)
        {
            rcvQueue.Enqueue(seg);
            rcvNext++;
        }
        else
        {
            // 乱序包 → 放入缓冲区等前面的到齐
            rcvBuffer.Add(seg);
        }

        // 检查 rcvBuffer 中是否有可以排序的包了
        rcvBuffer.Sort((a, b) => a.sn.CompareTo(b.sn));
        while (rcvBuffer.Count > 0 && rcvBuffer[0].sn == rcvNext)
        {
            rcvQueue.Enqueue(rcvBuffer[0]);
            rcvBuffer.RemoveAt(0);
            rcvNext++;
        }
    }
}
```

### KCP 参数调优

```
游戏类型       nodelay  interval  resend  nc   特点
MOBA/射击      1        10       2       1    极速模式，最低延迟
MMORPG         0        30       3       0    平衡模式，省带宽
回合制         0        50       5       0    省流量模式

说明：
nodelay=1: 不启用拥塞控制
interval=10: 每 10ms 刷新一次
resend=2: 同一个包被 ACK 2 次就快速重传
nc=1: 不限制流量
```

### KCP vs TCP 对比

| 特性 | TCP | KCP | UDP 裸发 |
|------|-----|-----|----------|
| 可靠性 | 自动保证 | 可配置 | 不保证 |
| 顺序 | 保证 | 保证 | 不保证 |
| 最小 RTO | 200ms | 可配置到 10ms | N/A |
| 拥塞控制 | 有（不可关） | 可关 | 无 |
| 头部开销 | 20 字节 | 24 字节 | 8 字节 |
| 适用场景 | 登录/聊天 | 实时战斗 | 位置同步 |

---

## 2. 客户端预测与服务器回滚

### 核心问题

```
你按了 W 键，希望角色往前走：
┌─── 乐观（客户端预测）───┐
│ 客户端立即移动角色       │ ← 0ms 响应（爽！）
│ 同时发送移动指令给服务器  │
│ 收到服务器确认位置       │ ← 100ms 后
│ 如果位置不一致 = 回滚    │
└─────────────────────────┘

┌─── 悲观（等服务器确认）───┐
│ 发送移动指令给服务器      │ ← 0ms
│ 等待服务器返回新位置      │ ← 100ms 后
│ 客户端再移动角色          │ ← 好卡！
└─────────────────────────┘
```

### 客户端预测实现

```csharp
public class ClientPrediction : MonoBehaviour
{
    // 客户端本地模拟
    private Vector3 predictedPosition;
    private Vector3 velocity;

    // 待确认的输入（发送给服务器但还没回复的）
    private Queue<InputCommand> pendingInputs = new();
    private const int MAX_PENDING = 30;

    void Update()
    {
        // 1. 收集输入
        InputCommand input = GatherInput();

        // 2. 客户端预测（立即执行）
        ExecuteLocally(input, Time.deltaTime);

        // 3. 发送给服务器
        pendingInputs.Enqueue(input);

        // 4. 限制未确认输入数量
        if (pendingInputs.Count > MAX_PENDING)
        {
            // 等太久没回复 = 可能断线
            StartCoroutine(Reconnect());
        }
    }

    private void ExecuteLocally(InputCommand input, float dt)
    {
        Vector3 move = Vector3.zero;
        if (input.forward) move += transform.forward;
        if (input.back) move -= transform.forward;
        if (input.left) move -= transform.right;
        if (input.right) move += transform.right;

        predictedPosition = transform.position + move.normalized * speed * dt;
        transform.position = predictedPosition;
    }

    // 服务器返回权威位置
    public void OnServerUpdate(uint lastProcessedInputId, Vector3 serverPosition)
    {
        // 1. 移除已确认的输入
        while (pendingInputs.Count > 0 &&
               pendingInputs.Peek().inputId <= lastProcessedInputId)
        {
            pendingInputs.Dequeue();
        }

        // 2. 纠正位置（服务器说你在哪）
        Vector3 correction = serverPosition - predictedPosition;

        if (correction.magnitude > 0.1f)
        {
            // 3. 需要回滚！
            predictedPosition = serverPosition;
            transform.position = serverPosition;

            // 4. 重放未确认的输入
            foreach (var input in pendingInputs)
            {
                ExecuteLocally(input, Time.fixedDeltaTime);
            }
        }
    }

    private struct InputCommand
    {
        public uint inputId;
        public bool forward, back, left, right;
        public float timestamp;
    }

    private uint inputCounter;
    private InputCommand GatherInput()
    {
        return new InputCommand
        {
            inputId = inputCounter++,
            forward = Input.GetKey(KeyCode.W),
            back = Input.GetKey(KeyCode.S),
            left = Input.GetKey(KeyCode.A),
            right = Input.GetKey(KeyCode.D),
            timestamp = Time.time
        };
    }
}
```

### 服务器回滚（Server Reconciliation）

```
服务器收到客户端的移动指令后：
1. 先模拟执行（客户端在 100ms 前已经做了这件事）
2. 比较客户端"声称的位置"和服务器计算的位置
3. 如果差异超过阈值 → 发送纠正

服务器的回滚更复杂：
1. 暂存最近 N 帧的世界状态
2. 收到玩家射击指令时，不是检查"当前"位置
3. 而是回到"扣动扳机那一刻"的玩家位置
4. 在那时玩家的位置上做射线检测
5. 这样即使有延迟，射击判定依然公平
```

```csharp
// 服务器端的延迟补偿
public class ServerLagCompensation
{
    // 历史位置快照（存储过去 500ms 的玩家位置）
    private Dictionary<uint, List<PositionSnapshot>> positionHistory = new();

    // 每帧记录位置
    public void RecordSnapshot(uint playerId, Vector3 position, Quaternion rotation, float time)
    {
        if (!positionHistory.ContainsKey(playerId))
            positionHistory[playerId] = new List<PositionSnapshot>();

        var history = positionHistory[playerId];
        history.Add(new PositionSnapshot { position = position, rotation = rotation, time = time });

        // 只保留最近 1 秒的历史
        while (history.Count > 0 && (time - history[0].time) > 1.0f)
            history.RemoveAt(0);
    }

    // 回到过去做判定
    public bool CheckHitAtTime(uint playerId, Vector3 hitOrigin, Vector3 hitDir,
                                 float targetTime, out RaycastHit hitInfo)
    {
        // 在历史中找到最接近 targetTime 的位置
        var history = positionHistory[playerId];
        PositionSnapshot snap = FindClosestSnapshot(history, targetTime);

        // 在"历史位置"做碰撞检测
        Ray ray = new Ray(hitOrigin, hitDir);
        Collider col = GetPlayerCollider(playerId);
        col.transform.position = snap.position;
        col.transform.rotation = snap.rotation;

        bool hit = col.Raycast(ray, out hitInfo, maxDistance);

        // 恢复位置
        // （在真实服务器实现中，使用射线 vs 胶囊体的数学检测更高效）
        return hit;
    }

    private struct PositionSnapshot
    {
        public Vector3 position;
        public Quaternion rotation;
        public float time;
    }
}
```

---

## 3. 实体插值与外推

### 为什么需要插值？

```
服务器以 20fps（50ms 间隔）发送位置更新：
t=0ms    位置 A
t=50ms   位置 B  ← 收到 A，知道要去 B
t=100ms  位置 C  ← 收到 B，知道要去 C
t=150ms  位置 D  ← 收到 C

如果直接设置位置：
帧 0：A → 帧 1：B → 帧 2：C → 感觉一跳一跳的

如果用插值：
帧 0：A
帧 1：A→B 之间 20%
帧 2：A→B 之间 40%
...平滑移动到 B
```

### 实体插值实现

```csharp
public class EntityInterpolation : MonoBehaviour
{
    // 状态缓冲区
    private struct StateBuffer
    {
        public Vector3 position;
        public Quaternion rotation;
        public float timestamp;
    }

    private Queue<StateBuffer> stateBuffer = new();
    private const int BUFFER_SIZE = 4;
    private const float RENDER_DELAY = 0.1f; // 故意延迟 100ms 用于插值

    // 收到的当前帧和上一帧
    private StateBuffer previousState;
    private StateBuffer nextState;
    private float interpolationTime;

    void Update()
    {
        // 收到服务器状态时调用
        void OnReceiveState(Vector3 pos, Quaternion rot, float serverTime)
        {
            stateBuffer.Enqueue(new StateBuffer
            {
                position = pos,
                rotation = rot,
                timestamp = serverTime
            });

            while (stateBuffer.Count > BUFFER_SIZE)
                stateBuffer.Dequeue();
        }

        // 渲染时间 = 当前时间 - 插值延迟
        float renderTime = Time.time - RENDER_DELAY;

        // 在缓冲区中找合适的两个状态
        while (stateBuffer.Count >= 2 && stateBuffer.Peek().timestamp < renderTime)
        {
            previousState = stateBuffer.Dequeue();
        }

        if (stateBuffer.Count >= 2)
        {
            nextState = stateBuffer.Peek();

            // 计算插值进度
            float duration = nextState.timestamp - previousState.timestamp;
            if (duration > 0)
            {
                float t = (renderTime - previousState.timestamp) / duration;
                t = Mathf.Clamp01(t);

                // 线性插值位置
                transform.position = Vector3.Lerp(
                    previousState.position, nextState.position, t);

                // 球面插值旋转
                transform.rotation = Quaternion.Slerp(
                    previousState.rotation, nextState.rotation, t);
            }
        }
    }
}
```

### 外推（Extrapolation）— 当数据不够时

```
服务器没发新数据时，客户端继续推测：

外推策略：
1. 上次位置 + 上次速度 × Δt = 推测位置
2. 如果有加速度，还加上 1/2 × a × t²
3. 不能无限制外推 → 设置最大外推时间（200ms）

外推 vs 插值：
插值：在两帧已知数据之间平滑过渡（历史数据）
外推：根据已知数据推测未来位置（未来数据）

通常两者结合：
- 有前后帧 → 插值
- 只有前一帧 → 外推
- 两者都没有 → 停止（等服务器数据）
```

```csharp
public class EntityExtrapolation : MonoBehaviour
{
    private Vector3 lastPosition;
    private Vector3 lastVelocity;
    private float lastUpdateTime;
    private const float MAX_EXTRAPOLATE_TIME = 0.2f;

    void OnReceiveUpdate(Vector3 pos, Vector3 vel, float serverTime)
    {
        // 计算实际速度
        float dt = serverTime - lastUpdateTime;
        if (dt > 0.001f)
            lastVelocity = (pos - lastPosition) / dt;

        lastPosition = pos;
        lastUpdateTime = serverTime;
    }

    void Update()
    {
        float timeSinceUpdate = Time.time - lastUpdateTime;

        if (timeSinceUpdate > 0.05f && timeSinceUpdate < MAX_EXTRAPOLATE_TIME)
        {
            // 外推：位置 += 速度 × Δt
            Vector3 extrapolated = lastPosition + lastVelocity * timeSinceUpdate;
            transform.position = extrapolated;
        }
        else if (timeSinceUpdate >= MAX_EXTRAPOLATE_TIME)
        {
            // 超过最大外推时间 → 停止
            // 或者慢慢减速到零
        }
    }
}
```

---

## 4. 带宽优化

### 数据压缩策略

```
减少带宽的 5 种手段：
1. 差量更新：只发变化的部分（不是全量位置）
2. 量化：float → 半精度 / 整型（损失精度换带宽）
3. 增量压缩：记录相对于上次的变化量
4. 优先级排序：重要的先发，不重要的少发
5. 频率控制：不同数据不同发送频率
```

### 位置量化示例

```csharp
public static class Quantization
{
    // 把 -128~128 范围的 float 压缩到 uint16
    // 精度：256 / 65536 ≈ 0.0039 单位
    public static ushort QuantizePosition(float value)
    {
        // 映射到 [0, 65535]
        float clamped = Mathf.Clamp(value, -128f, 128f);
        float normalized = (clamped + 128f) / 256f;
        return (ushort)(normalized * 65535f);
    }

    public static float DequantizePosition(ushort quantized)
    {
        float normalized = quantized / 65535f;
        return normalized * 256f - 128f;
    }

    // 旋转压缩：把四元数压缩到 3 个 ushort
    // 因为四元数 w² + x² + y² + z² = 1，可以恢复
    public static (ushort, ushort, ushort) CompressRotation(Quaternion q)
    {
        // 取绝对值最大的分量做符号位，用 3 个分量存储
        // 这里简化实现：只存欧拉角并量化到 0.1° 精度
        Vector3 euler = q.eulerAngles;
        ushort x = (ushort)(Mathf.RoundToInt(euler.x / 0.1f) % 3600);
        ushort y = (ushort)(Mathf.RoundToInt(euler.y / 0.1f) % 3600);
        ushort z = (ushort)(Mathf.RoundToInt(euler.z / 0.1f) % 3600);
        return (x, y, z);
    }
}
```

### LZ4 压缩

```csharp
// LZ4 是极速压缩算法——压缩率一般但速度极快
// 适合网络包压缩（延迟敏感的场景）
//
// LZ4 vs Zstd 对比：
// ┌──────────┬─────────────┬─────────────┐
// │          │ LZ4         │ Zstd        │
// ├──────────┼─────────────┼─────────────┤
// │ 压缩速度  │ ~500 MB/s   │ ~100 MB/s   │
// │ 解压速度  │ ~1 GB/s     │ ~500 MB/s   │
// │ 压缩率    │ 2:1         │ 3~5:1       │
// │ 适用      │ 网络包/实时  │ 存档/资源   │
// └──────────┴─────────────┴─────────────┘

// 使用 LZ4 压缩网络包（需要 LZ4 库，NuGet: lz4net）
using LZ4;

public class PacketCompression
{
    // 网络包压缩（对延迟敏感，用 LZ4）
    public static byte[] CompressPacket(byte[] rawData)
    {
        // 小包不压缩（头部开销可能比压缩节省的还多）
        if (rawData.Length < 64)
            return rawData;

        byte[] compressed = LZ4Codec.Encode(rawData, 0, rawData.Length);

        // 如果压缩后反而更大，用原始数据
        if (compressed.Length >= rawData.Length)
            return rawData;

        return compressed;
    }

    public static byte[] DecompressPacket(byte[] data)
    {
        // 判断是否压缩（自定义协议标记）
        // if (isCompressed)
        return LZ4Codec.Decode(data, 0, data.Length);
        // else return data;
    }
}

// Zstd 适合存档压缩（追求压缩率）
// NuGet: ZstdSharp
// byte[] compressed = ZstdHelper.Compress(rawData, level: 3);
// byte[] decompressed = ZstdHelper.Decompress(compressed);
```

### 发送频率控制

```csharp
public class BandwidthManager
{
    // 不同数据类型的更新频率
    public enum DataCategory
    {
        Position,       // 位置：10~20次/秒
        Rotation,       // 旋转：5~10次/秒
        Animation,      // 动画状态：变化时发
        Health,         // 血量：变化时发
        Chat,           // 聊天：按需发
    }

    private Dictionary<DataCategory, float> sendIntervals = new()
    {
        { DataCategory.Position, 0.05f },   // 20次/秒
        { DataCategory.Rotation, 0.1f },     // 10次/秒
        { DataCategory.Animation, 0f },      // 不限制
        { DataCategory.Health, 0f },
        { DataCategory.Chat, 0f },
    };

    private Dictionary<DataCategory, float> lastSendTime = new();

    public bool ShouldSend(DataCategory category)
    {
        if (!sendIntervals.ContainsKey(category))
            return true;

        float interval = sendIntervals[category];
        if (interval <= 0) return true; // 不限频率

        float now = Time.time;
        if (!lastSendTime.ContainsKey(category) ||
            now - lastSendTime[category] >= interval)
        {
            lastSendTime[category] = now;
            return true;
        }
        return false;
    }
}
```

### 差量更新（Delta Compression）

```
不是每帧都发完整位置，而是发与上一帧的差异：

完整包（20 字节）：
[玩家ID 4字节][位置 12字节][旋转 4字节]

差量包（8 字节）：
[玩家ID 4字节][Δx 2字节][Δz 2字节]

对于位置变化小的帧，差量包节省 60% 带宽
客户端每 10 帧发一次完整包"校准"
```

---

## 5. 连接质量监控

```csharp
public class ConnectionQualityMonitor : MonoBehaviour
{
    public enum Quality { Excellent, Good, Fair, Poor, Disconnected }

    private Quality currentQuality;
    public float rtt;           // 往返延迟 (ms)
    public float jitter;        // 抖动 (ms)
    public float packetLoss;    // 丢包率 (0~1)
    public float bandwidth;     // 带宽 (bytes/s)

    // 统计窗口
    private Queue<float> rttSamples = new();
    private const int SAMPLE_COUNT = 50;
    private int sentPackets;
    private int ackedPackets;
    private float lastSecondBytes;
    private float bandwidthCounter;

    void Update()
    {
        // 计算带宽
        bandwidthCounter += GetSentBytesThisFrame();
        lastSecondBytes += Time.deltaTime;
        if (lastSecondBytes >= 1.0f)
        {
            bandwidth = bandwidthCounter;
            bandwidthCounter = 0;
            lastSecondBytes = 0;
        }

        // 评估连接质量
        UpdateQuality();
    }

    // 每次收到 ACK 时调用
    public void OnAck(float sampleRtt)
    {
        rttSamples.Enqueue(sampleRtt);
        while (rttSamples.Count > SAMPLE_COUNT)
            rttSamples.Dequeue();

        // 计算 RTT（取平均值）
        float sum = 0;
        foreach (var s in rttSamples) sum += s;
        rtt = sum / rttSamples.Count;

        // 计算抖动（RTT 的标准差）
        float variance = 0;
        foreach (var s in rttSamples)
            variance += (s - rtt) * (s - rtt);
        jitter = Mathf.Sqrt(variance / rttSamples.Count);

        ackedPackets++;
    }

    // 每次发送时调用
    public void OnSend() => sentPackets++;

    // 计算丢包率
    public void UpdatePacketLoss()
    {
        int total = sentPackets;
        if (total > 0)
            packetLoss = (float)(total - ackedPackets) / total;

        // 定期重置统计
        if (sentPackets > 1000)
        {
            sentPackets = ackedPackets / 2;
            ackedPackets = ackedPackets / 2;
        }
    }

    private void UpdateQuality()
    {
        if (rtt < 50 && packetLoss < 0.01f)
            currentQuality = Quality.Excellent;
        else if (rtt < 100 && packetLoss < 0.03f)
            currentQuality = Quality.Good;
        else if (rtt < 200 && packetLoss < 0.1f)
            currentQuality = Quality.Fair;
        else if (rtt < 500)
            currentQuality = Quality.Poor;
        else
            currentQuality = Quality.Disconnected;
    }

    // 根据连接质量动态调整
    public void ApplyQualitySettings(NetworkConfig config)
    {
        switch (currentQuality)
        {
            case Quality.Excellent:
                config.sendRate = 20;    // 20次/秒
                config.interpolationDelay = 0.05f;  // 50ms
                break;
            case Quality.Fair:
                config.sendRate = 10;
                config.interpolationDelay = 0.1f;
                break;
            case Quality.Poor:
                config.sendRate = 5;
                config.interpolationDelay = 0.2f;
                config.usePrediction = false;  // 关闭预测，服务器权威
                break;
        }
    }
}
```

---

## 6. 实战：FPS 网络同步架构

```
完整的 FPS 网络同步流程：

每帧客户端：
1. 收集玩家输入（WASD + 鼠标）
2. 客户端预测执行
3. 发送输入到服务器（含序列号）
4. 插值/外推其他玩家位置

服务器收到后：
1. 验证输入合法性（防作弊）
2. 执行权威模拟
3. 应用延迟补偿（射击判定）
4. 广播更新给所有玩家

客户端收到服务器更新：
1. 服务器回滚（纠正位置）
2. 重放未确认输入
3. 更新其他玩家状态缓冲区
4. 调整连接质量参数
```

### C++/Raylib 对比

| 概念 | C++ 实现 | Unity 实现 |
|------|---------|-----------|
| 可靠 UDP | enet / raknet | KCP C# 实现 |
| 预测 | 手动实现 | 同上思路 |
| 延迟补偿 | 存储位置快照 | 同上思路 |
| 压缩 | zlib / LZ4 | LZ4net / ZstdSharp |
| 插值 | 双缓冲状态 | Vector3.Lerp |
| 监控 | 自实现 | System.Net.NetworkInformation |

---

## 停靠点总结

| 概念 | 一句话 |
|------|--------|
| KCP | 在 UDP 上加可靠层，比 TCP 快、比裸 UDP 可靠 |
| 客户端预测 | 不等服务器，先执行再纠正 |
| 服务器回滚 | 回到扣扳机时刻做判定，消除延迟影响 |
| 插值 | 在已知两帧数据之间平滑过渡 |
| 外推 | 没有新数据时根据速度推测位置 |
| 差量更新 | 只发变化的部分，减少带宽 |
| LZ4 | 极速压缩，适合网络包 |
| 量化 | 用整型替代浮点，牺牲精度换带宽 |
| 连接质量 | RTT + 抖动 + 丢包率 → 动态调参 |

**对比 C++/Raylib：** 这些网络高级技术大部分是"算法问题"而非"语言问题"。KCP 的 C# 移植和 C++ 原版逻辑一致。客户端预测和延迟补偿是 Quake 时代的发明，在任何语言/引擎中思路相同。Unity 的优势是提供了 Lerp/Slerp 等现成函数做插值，而 C++ 需要自己实现。
