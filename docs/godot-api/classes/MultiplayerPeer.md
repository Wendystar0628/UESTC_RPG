<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MultiplayerPeer

命名空间 `Godot`；class；继承 [`PacketPeer`](PacketPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Manages the connection with one or more remote peers acting as server or client and assigning unique IDs to each of them. See also [`MultiplayerApi`](MultiplayerApi.md).

**Note:** The [`MultiplayerApi`](MultiplayerApi.md) protocol is an implementation detail and isn't meant to be used by non-Godot servers. It may change without notice.

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## 属性

### RefuseNewConnections

```csharp
public bool RefuseNewConnections { get; set; }
```

引擎名：`refuse_new_connections`

If `true`, this `MultiplayerPeer` refuses new connections.

### TransferChannel

```csharp
public int TransferChannel { get; set; }
```

引擎名：`transfer_channel`

The channel to use to send packets. Many network APIs such as ENet and WebRTC allow the creation of multiple independent channels which behaves, in a way, like separate connections. This means that reliable data will only block delivery of other packets on that channel, and ordering will only be in respect to the channel the packet is being sent on. Using different channels to send **different and independent** state updates is a common way to optimize network usage and decrease latency in fast-paced games.

**Note:** The default channel (`0`) actually works as 3 separate channels (one for each [`MultiplayerPeer.TransferModeEnum`](MultiplayerPeer.md)) so that [`MultiplayerPeer.TransferModeEnum.Reliable`](MultiplayerPeer.md) and [`MultiplayerPeer.TransferModeEnum.UnreliableOrdered`](MultiplayerPeer.md) does not interact with each other by default. Refer to the specific network API documentation (e.g. ENet or WebRTC) to learn how to set up channels correctly.

### TransferMode

```csharp
public MultiplayerPeer.TransferModeEnum TransferMode { get; set; }
```

引擎名：`transfer_mode`

The manner in which to send packets to the target peer. See the `SetTargetPeer()` method.

## 方法

### Close

```csharp
public void Close()
```

引擎名：`close`

Immediately close the multiplayer peer returning to the state [`MultiplayerPeer.ConnectionStatus.Disconnected`](MultiplayerPeer.md). Connected peers will be dropped without emitting `PeerDisconnected`.

### DisconnectPeer

```csharp
public void DisconnectPeer(int peer, bool force = false)
```

引擎名：`disconnect_peer`

Disconnects the given `peer` from this host. If `force` is `true` the `PeerDisconnected` signal will not be emitted for this peer.

### GenerateUniqueId

```csharp
public uint GenerateUniqueId()
```

引擎名：`generate_unique_id`

Returns a randomly generated integer that can be used as a network unique ID.

### GetConnectionStatus

```csharp
public MultiplayerPeer.ConnectionStatus GetConnectionStatus()
```

引擎名：`get_connection_status`

Returns the current state of the connection.

### GetPacketChannel

```csharp
public int GetPacketChannel()
```

引擎名：`get_packet_channel`

Returns the channel over which the next available packet was received. See [`PacketPeer.GetAvailablePacketCount`](PacketPeer.md).

### GetPacketMode

```csharp
public MultiplayerPeer.TransferModeEnum GetPacketMode()
```

引擎名：`get_packet_mode`

Returns the transfer mode the remote peer used to send the next available packet. See [`PacketPeer.GetAvailablePacketCount`](PacketPeer.md).

### GetPacketPeer

```csharp
public int GetPacketPeer()
```

引擎名：`get_packet_peer`

Returns the ID of the `MultiplayerPeer` who sent the next available packet. See [`PacketPeer.GetAvailablePacketCount`](PacketPeer.md).

### GetTransferChannel

```csharp
public int GetTransferChannel()
```

引擎名：`get_transfer_channel`

### GetTransferMode

```csharp
public MultiplayerPeer.TransferModeEnum GetTransferMode()
```

引擎名：`get_transfer_mode`

### GetUniqueId

```csharp
public int GetUniqueId()
```

引擎名：`get_unique_id`

Returns the ID of this `MultiplayerPeer`.

### IsRefusingNewConnections

```csharp
public bool IsRefusingNewConnections()
```

引擎名：`is_refusing_new_connections`

### IsServerRelaySupported

```csharp
public bool IsServerRelaySupported()
```

引擎名：`is_server_relay_supported`

Returns `true` if the server can act as a relay in the current configuration. That is, if the higher level [`MultiplayerApi`](MultiplayerApi.md) should notify connected clients of other peers, and implement a relay protocol to allow communication between them.

### Poll

```csharp
public void Poll()
```

引擎名：`poll`

Waits up to 1 second to receive a new network event.

### SetRefuseNewConnections

```csharp
public void SetRefuseNewConnections(bool enable)
```

引擎名：`set_refuse_new_connections`

### SetTargetPeer

```csharp
public void SetTargetPeer(int id)
```

引擎名：`set_target_peer`

Sets the peer to which packets will be sent.

The `id` can be one of: `TargetPeerBroadcast` to send to all connected peers, `TargetPeerServer` to send to the peer acting as server, a valid peer ID to send to that specific peer, a negative peer ID to send to all peers except that one. By default, the target peer is `TargetPeerBroadcast`.

### SetTransferChannel

```csharp
public void SetTransferChannel(int channel)
```

引擎名：`set_transfer_channel`

### SetTransferMode

```csharp
public void SetTransferMode(MultiplayerPeer.TransferModeEnum mode)
```

引擎名：`set_transfer_mode`

## 事件（信号）

### PeerConnected

```csharp
public event MultiplayerPeer.PeerConnectedEventHandler PeerConnected
```

引擎名：`peer_connected`

Emitted when a remote peer connects.

### PeerDisconnected

```csharp
public event MultiplayerPeer.PeerDisconnectedEventHandler PeerDisconnected
```

引擎名：`peer_disconnected`

Emitted when a remote peer has disconnected.

## 常量

### TargetPeerBroadcast

```csharp
public const long TargetPeerBroadcast = 0
```

Packets are sent to all connected peers.

### TargetPeerServer

```csharp
public const long TargetPeerServer = 1
```

Packets are sent to the remote peer acting as server.

## ConnectionStatus

命名空间 `Godot`；enum

### 枚举值

- `Disconnected = 0`：The MultiplayerPeer is disconnected.
- `Connecting = 1`：The MultiplayerPeer is currently connecting to a server.
- `Connected = 2`：This MultiplayerPeer is connected.

## PeerConnectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`MultiplayerPeer.PeerConnected`](MultiplayerPeer.md) event of a [`MultiplayerPeer`](MultiplayerPeer.md) class.

### 签名

#### Invoke

```csharp
delegate void PeerConnectedEventHandler(long id)
```

## PeerDisconnectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`MultiplayerPeer.PeerDisconnected`](MultiplayerPeer.md) event of a [`MultiplayerPeer`](MultiplayerPeer.md) class.

### 签名

#### Invoke

```csharp
delegate void PeerDisconnectedEventHandler(long id)
```

## TransferModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Unreliable = 0`：Packets are not acknowledged, no resend attempts are made for lost packets.
- `UnreliableOrdered = 1`：Packets are not acknowledged, no resend attempts are made for lost packets.
- `Reliable = 2`：Packets must be received and resend attempts should be made until the packets are acknowledged.
