<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ENetPacketPeer

命名空间 `Godot`；class；继承 [`PacketPeer`](PacketPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A PacketPeer implementation representing a peer of an [`ENetConnection`](ENetConnection.md).

This class cannot be instantiated directly but can be retrieved during [`ENetConnection.Service`](ENetConnection.md) or via [`ENetConnection.GetPeers`](ENetConnection.md).

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## 方法

### GetChannels

```csharp
public int GetChannels()
```

引擎名：`get_channels`

Returns the number of channels allocated for communication with peer.

### GetPacketFlags

```csharp
public int GetPacketFlags()
```

引擎名：`get_packet_flags`

Returns the ENet flags of the next packet in the received queue. See `FLAG_*` constants for available packet flags. Note that not all flags are replicated from the sending peer to the receiving peer.

### GetRemoteAddress

```csharp
public string GetRemoteAddress()
```

引擎名：`get_remote_address`

Returns the IP address of this peer.

### GetRemotePort

```csharp
public int GetRemotePort()
```

引擎名：`get_remote_port`

Returns the remote port of this peer.

### GetState

```csharp
public ENetPacketPeer.PeerState GetState()
```

引擎名：`get_state`

Returns the current peer state.

### GetStatistic

```csharp
public double GetStatistic(ENetPacketPeer.PeerStatistic statistic)
```

引擎名：`get_statistic`

Returns the requested `statistic` for this peer.

### IsActive

```csharp
public bool IsActive()
```

引擎名：`is_active`

Returns `true` if the peer is currently active (i.e. the associated [`ENetConnection`](ENetConnection.md) is still valid).

### PeerDisconnect

```csharp
public void PeerDisconnect(int data = 0)
```

引擎名：`peer_disconnect`

Request a disconnection from a peer. An [`ENetConnection.EventType.Disconnect`](ENetConnection.md) will be generated during [`ENetConnection.Service`](ENetConnection.md) once the disconnection is complete.

### PeerDisconnectLater

```csharp
public void PeerDisconnectLater(int data = 0)
```

引擎名：`peer_disconnect_later`

Request a disconnection from a peer, but only after all queued outgoing packets are sent. An [`ENetConnection.EventType.Disconnect`](ENetConnection.md) will be generated during [`ENetConnection.Service`](ENetConnection.md) once the disconnection is complete.

### PeerDisconnectNow

```csharp
public void PeerDisconnectNow(int data = 0)
```

引擎名：`peer_disconnect_now`

Force an immediate disconnection from a peer. No [`ENetConnection.EventType.Disconnect`](ENetConnection.md) will be generated. The foreign peer is not guaranteed to receive the disconnect notification, and is reset immediately upon return from this function.

### Ping

```csharp
public void Ping()
```

引擎名：`ping`

Sends a ping request to a peer. ENet automatically pings all connected peers at regular intervals, however, this function may be called to ensure more frequent ping requests.

### PingInterval

```csharp
public void PingInterval(int pingInterval)
```

引擎名：`ping_interval`

Sets the `pingInterval` in milliseconds at which pings will be sent to a peer. Pings are used both to monitor the liveness of the connection and also to dynamically adjust the throttle during periods of low traffic so that the throttle has reasonable responsiveness during traffic spikes. The default ping interval is `500` milliseconds.

### Reset

```csharp
public void Reset()
```

引擎名：`reset`

Forcefully disconnects a peer. The foreign host represented by the peer is not notified of the disconnection and will timeout on its connection to the local host.

### Send

```csharp
public Error Send(int channel, ReadOnlySpan<byte> packet, int flags)
```

引擎名：`send`

Queues a `packet` to be sent over the specified `channel`. See `FLAG_*` constants for available packet flags.

### Send

```csharp
public Error Send(int channel, byte[] packet, int flags)
```

引擎名：`send`

Queues a `packet` to be sent over the specified `channel`. See `FLAG_*` constants for available packet flags.

### SetTimeout

```csharp
public void SetTimeout(int timeout, int timeoutMin, int timeoutMax)
```

引擎名：`set_timeout`

Sets the timeout parameters for a peer. The timeout parameters control how and when a peer will timeout from a failure to acknowledge reliable traffic. Timeout values are expressed in milliseconds.

The `timeout` is a factor that, multiplied by a value based on the average round trip time, will determine the timeout limit for a reliable packet. When that limit is reached, the timeout will be doubled, and the peer will be disconnected if that limit has reached `timeoutMin`. The `timeoutMax` parameter, on the other hand, defines a fixed timeout for which any packet must be acknowledged or the peer will be dropped.

### ThrottleConfigure

```csharp
public void ThrottleConfigure(int interval, int acceleration, int deceleration)
```

引擎名：`throttle_configure`

Configures throttle parameter for a peer.

Unreliable packets are dropped by ENet in response to the varying conditions of the Internet connection to the peer. The throttle represents a probability that an unreliable packet should not be dropped and thus sent by ENet to the peer. By measuring fluctuations in round trip times of reliable packets over the specified `interval`, ENet will either increase the probability by the amount specified in the `acceleration` parameter, or decrease it by the amount specified in the `deceleration` parameter (both are ratios to `PacketThrottleScale`).

When the throttle has a value of `PacketThrottleScale`, no unreliable packets are dropped by ENet, and so 100% of all unreliable packets will be sent.

When the throttle has a value of `0`, all unreliable packets are dropped by ENet, and so 0% of all unreliable packets will be sent.

Intermediate values for the throttle represent intermediate probabilities between 0% and 100% of unreliable packets being sent. The bandwidth limits of the local and foreign hosts are taken into account to determine a sensible limit for the throttle probability above which it should not raise even in the best of conditions.

## 常量

### FlagReliable

```csharp
public const long FlagReliable = 1
```

Mark the packet to be sent as reliable.

### FlagUnreliableFragment

```csharp
public const long FlagUnreliableFragment = 8
```

Mark the packet to be sent unreliable even if the packet is too big and needs fragmentation (increasing the chance of it being dropped).

### FlagUnsequenced

```csharp
public const long FlagUnsequenced = 2
```

Mark the packet to be sent unsequenced (unreliable).

### PacketLossScale

```csharp
public const long PacketLossScale = 65536
```

The reference scale for packet loss. See `GetStatistic()` and [`ENetPacketPeer.PeerStatistic.PacketLoss`](ENetPacketPeer.md).

### PacketThrottleScale

```csharp
public const long PacketThrottleScale = 32
```

The reference value for throttle configuration. The default value is `32`. See `ThrottleConfigure()`.

## PeerState

命名空间 `Godot`；enum

### 枚举值

- `Disconnected = 0`：The peer is disconnected.
- `Connecting = 1`：The peer is currently attempting to connect.
- `AcknowledgingConnect = 2`：The peer has acknowledged the connection request.
- `ConnectionPending = 3`：The peer is currently connecting.
- `ConnectionSucceeded = 4`：The peer has successfully connected, but is not ready to communicate with yet (`Connected`).
- `Connected = 5`：The peer is currently connected and ready to communicate with.
- `DisconnectLater = 6`：The peer is expected to disconnect after it has no more outgoing packets to send.
- `Disconnecting = 7`：The peer is currently disconnecting.
- `AcknowledgingDisconnect = 8`：The peer has acknowledged the disconnection request.
- `Zombie = 9`：The peer has lost connection, but is not considered truly disconnected (as the peer didn't acknowledge the disconnection request).

## PeerStatistic

命名空间 `Godot`；enum

### 枚举值

- `PacketLoss = 0`：Mean packet loss of reliable packets as a ratio with respect to the [`ENetPacketPeer.PacketLossScale`](ENetPacketPeer.md).
- `PacketLossVariance = 1`：Packet loss variance.
- `PacketLossEpoch = 2`：The time at which packet loss statistics were last updated (in milliseconds since the connection started).
- `RoundTripTime = 3`：Mean packet round trip time for reliable packets.
- `RoundTripTimeVariance = 4`：Variance of the mean round trip time.
- `LastRoundTripTime = 5`：Last recorded round trip time for a reliable packet.
- `LastRoundTripTimeVariance = 6`：Variance of the last trip time recorded.
- `PacketThrottle = 7`：The peer's current throttle status.
- `PacketThrottleLimit = 8`：The maximum number of unreliable packets that should not be dropped.
- `PacketThrottleCounter = 9`：Internal value used to increment the packet throttle counter.
- `PacketThrottleEpoch = 10`：The time at which throttle statistics were last updated (in milliseconds since the connection started).
- `PacketThrottleAcceleration = 11`：The throttle's acceleration factor.
- `PacketThrottleDeceleration = 12`：The throttle's deceleration factor.
- `PacketThrottleInterval = 13`：The interval over which the lowest mean round trip time should be measured for use by the throttle mechanism (in milliseconds).
