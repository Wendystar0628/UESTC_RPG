<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SceneMultiplayer

命名空间 `Godot`；class；继承 [`MultiplayerApi`](MultiplayerApi.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class is the default implementation of [`MultiplayerApi`](MultiplayerApi.md), used to provide multiplayer functionalities in Godot Engine.

This implementation supports RPCs via [`Node.Rpc`](Node.md) and [`Node.RpcId`](Node.md) and requires [`MultiplayerApi.Rpc`](MultiplayerApi.md) to be passed a [`Node`](Node.md) (it will fail for other object types).

This implementation additionally provide [`SceneTree`](SceneTree.md) replication via the [`MultiplayerSpawner`](MultiplayerSpawner.md) and [`MultiplayerSynchronizer`](MultiplayerSynchronizer.md) nodes, and the [`SceneReplicationConfig`](SceneReplicationConfig.md) resource.

**Note:** The high-level multiplayer API protocol is an implementation detail and isn't meant to be used by non-Godot servers. It may change without notice.

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## 构造函数

### SceneMultiplayer

```csharp
public SceneMultiplayer()
```

## 属性

### AllowObjectDecoding

```csharp
public bool AllowObjectDecoding { get; set; }
```

引擎名：`allow_object_decoding`

If `true`, the MultiplayerAPI will allow encoding and decoding of object during RPCs.

**Warning:** Deserialized objects can contain code which gets executed. Do not use this option if the serialized object comes from untrusted sources to avoid potential security threat such as remote code execution.

### AuthCallback

```csharp
public Callable AuthCallback { get; set; }
```

引擎名：`auth_callback`

The callback to execute when receiving authentication data sent via `SendAuth()`. If the [`Callable`](Callable.md) is empty (default), peers will be automatically accepted as soon as they connect.

### AuthTimeout

```csharp
public double AuthTimeout { get; set; }
```

引擎名：`auth_timeout`

If set to a value greater than `0.0`, the maximum duration in seconds peers can stay in the authenticating state, after which the authentication will automatically fail. See the `PeerAuthenticating` and `PeerAuthenticationFailed` signals.

### MaxDeltaPacketSize

```csharp
public int MaxDeltaPacketSize { get; set; }
```

引擎名：`max_delta_packet_size`

Maximum size of each delta packet. Higher values increase the chance of receiving full updates in a single frame, but also the chance of causing networking congestion (higher latency, disconnections). See [`MultiplayerSynchronizer`](MultiplayerSynchronizer.md).

### MaxSyncPacketSize

```csharp
public int MaxSyncPacketSize { get; set; }
```

引擎名：`max_sync_packet_size`

Maximum size of each synchronization packet. Higher values increase the chance of receiving full updates in a single frame, but also the chance of packet loss. See [`MultiplayerSynchronizer`](MultiplayerSynchronizer.md).

### RefuseNewConnections

```csharp
public bool RefuseNewConnections { get; set; }
```

引擎名：`refuse_new_connections`

If `true`, the MultiplayerAPI's [`MultiplayerApi.MultiplayerPeer`](MultiplayerApi.md) refuses new incoming connections.

### RootPath

```csharp
public NodePath RootPath { get; set; }
```

引擎名：`root_path`

The root path to use for RPCs and replication. Instead of an absolute path, a relative path will be used to find the node upon which the RPC should be executed.

This effectively allows to have different branches of the scene tree to be managed by different MultiplayerAPI, allowing for example to run both client and server in the same scene.

### ServerRelay

```csharp
public bool ServerRelay { get; set; }
```

引擎名：`server_relay`

Enable or disable the server feature that notifies clients of other peers' connection/disconnection, and relays messages between them. When this option is `false`, clients won't be automatically notified of other peers and won't be able to send them packets through the server.

**Note:** Changing this option while other peers are connected may lead to unexpected behaviors.

**Note:** Support for this feature may depend on the current [`MultiplayerPeer`](MultiplayerPeer.md) configuration. See [`MultiplayerPeer.IsServerRelaySupported`](MultiplayerPeer.md).

## 方法

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the current SceneMultiplayer network state (you shouldn't call this unless you know what you are doing).

### CompleteAuth

```csharp
public Error CompleteAuth(int id)
```

引擎名：`complete_auth`

Mark the authentication step as completed for the remote peer identified by `id`. The [`MultiplayerApi.PeerConnected`](MultiplayerApi.md) signal will be emitted for this peer once the remote side also completes the authentication. No further authentication messages are expected to be received from this peer.

If a peer disconnects before completing authentication, either due to a network issue, the `AuthTimeout` expiring, or manually calling `DisconnectPeer()`, the `PeerAuthenticationFailed` signal will be emitted instead of [`MultiplayerApi.PeerDisconnected`](MultiplayerApi.md).

### DisconnectPeer

```csharp
public void DisconnectPeer(int id)
```

引擎名：`disconnect_peer`

Disconnects the peer identified by `id`, removing it from the list of connected peers, and closing the underlying connection with it.

### GetAuthCallback

```csharp
public Callable GetAuthCallback()
```

引擎名：`get_auth_callback`

### GetAuthTimeout

```csharp
public double GetAuthTimeout()
```

引擎名：`get_auth_timeout`

### GetAuthenticatingPeers

```csharp
public int[] GetAuthenticatingPeers()
```

引擎名：`get_authenticating_peers`

Returns the IDs of the peers currently trying to authenticate with this [`MultiplayerApi`](MultiplayerApi.md).

### GetMaxDeltaPacketSize

```csharp
public int GetMaxDeltaPacketSize()
```

引擎名：`get_max_delta_packet_size`

### GetMaxSyncPacketSize

```csharp
public int GetMaxSyncPacketSize()
```

引擎名：`get_max_sync_packet_size`

### GetRootPath

```csharp
public NodePath GetRootPath()
```

引擎名：`get_root_path`

### IsObjectDecodingAllowed

```csharp
public bool IsObjectDecodingAllowed()
```

引擎名：`is_object_decoding_allowed`

### IsRefusingNewConnections

```csharp
public bool IsRefusingNewConnections()
```

引擎名：`is_refusing_new_connections`

### IsServerRelayEnabled

```csharp
public bool IsServerRelayEnabled()
```

引擎名：`is_server_relay_enabled`

### SendAuth

```csharp
public Error SendAuth(int id, ReadOnlySpan<byte> data)
```

引擎名：`send_auth`

Sends the specified `data` to the remote peer identified by `id` as part of an authentication message. This can be used to authenticate peers, and control when [`MultiplayerApi.PeerConnected`](MultiplayerApi.md) is emitted (and the remote peer accepted as one of the connected peers).

### SendAuth

```csharp
public Error SendAuth(int id, byte[] data)
```

引擎名：`send_auth`

Sends the specified `data` to the remote peer identified by `id` as part of an authentication message. This can be used to authenticate peers, and control when [`MultiplayerApi.PeerConnected`](MultiplayerApi.md) is emitted (and the remote peer accepted as one of the connected peers).

### SendBytes

```csharp
public Error SendBytes(ReadOnlySpan<byte> bytes, int id, MultiplayerPeer.TransferModeEnum mode, int channel)
```

引擎名：`send_bytes`

Sends the given raw `bytes` to a specific peer identified by `id` (see [`MultiplayerPeer.SetTargetPeer`](MultiplayerPeer.md)). Default ID is `0`, i.e. broadcast to all peers.

### SendBytes

```csharp
public Error SendBytes(byte[] bytes, int id = 0, MultiplayerPeer.TransferModeEnum mode = MultiplayerPeer.TransferModeEnum.Reliable, int channel = 0)
```

引擎名：`send_bytes`

Sends the given raw `bytes` to a specific peer identified by `id` (see [`MultiplayerPeer.SetTargetPeer`](MultiplayerPeer.md)). Default ID is `0`, i.e. broadcast to all peers.

### SetAllowObjectDecoding

```csharp
public void SetAllowObjectDecoding(bool enable)
```

引擎名：`set_allow_object_decoding`

### SetAuthCallback

```csharp
public void SetAuthCallback(Callable callback)
```

引擎名：`set_auth_callback`

### SetAuthTimeout

```csharp
public void SetAuthTimeout(double timeout)
```

引擎名：`set_auth_timeout`

### SetMaxDeltaPacketSize

```csharp
public void SetMaxDeltaPacketSize(int size)
```

引擎名：`set_max_delta_packet_size`

### SetMaxSyncPacketSize

```csharp
public void SetMaxSyncPacketSize(int size)
```

引擎名：`set_max_sync_packet_size`

### SetRefuseNewConnections

```csharp
public void SetRefuseNewConnections(bool refuse)
```

引擎名：`set_refuse_new_connections`

### SetRootPath

```csharp
public void SetRootPath(NodePath path)
```

引擎名：`set_root_path`

### SetServerRelayEnabled

```csharp
public void SetServerRelayEnabled(bool enabled)
```

引擎名：`set_server_relay_enabled`

## 事件（信号）

### PeerAuthenticating

```csharp
public event SceneMultiplayer.PeerAuthenticatingEventHandler PeerAuthenticating
```

引擎名：`peer_authenticating`

Emitted when this MultiplayerAPI's [`MultiplayerApi.MultiplayerPeer`](MultiplayerApi.md) connects to a new peer and a valid `AuthCallback` is set. In this case, the [`MultiplayerApi.PeerConnected`](MultiplayerApi.md) will not be emitted until `CompleteAuth()` is called with given peer `id`. While in this state, the peer will not be included in the list returned by [`MultiplayerApi.GetPeers`](MultiplayerApi.md) (but in the one returned by `GetAuthenticatingPeers()`), and only authentication data will be sent or received. See `SendAuth()` for sending authentication data.

### PeerAuthenticationFailed

```csharp
public event SceneMultiplayer.PeerAuthenticationFailedEventHandler PeerAuthenticationFailed
```

引擎名：`peer_authentication_failed`

Emitted when this MultiplayerAPI's [`MultiplayerApi.MultiplayerPeer`](MultiplayerApi.md) disconnects from a peer for which authentication had not yet completed. See `PeerAuthenticating`.

### PeerPacket

```csharp
public event SceneMultiplayer.PeerPacketEventHandler PeerPacket
```

引擎名：`peer_packet`

Emitted when this MultiplayerAPI's [`MultiplayerApi.MultiplayerPeer`](MultiplayerApi.md) receives a `packet` with custom data (see `SendBytes()`). ID is the peer ID of the peer that sent the packet.

## PeerAuthenticatingEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`SceneMultiplayer.PeerAuthenticating`](SceneMultiplayer.md) event of a [`SceneMultiplayer`](SceneMultiplayer.md) class.

### 签名

#### Invoke

```csharp
delegate void PeerAuthenticatingEventHandler(long id)
```

## PeerAuthenticationFailedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`SceneMultiplayer.PeerAuthenticationFailed`](SceneMultiplayer.md) event of a [`SceneMultiplayer`](SceneMultiplayer.md) class.

### 签名

#### Invoke

```csharp
delegate void PeerAuthenticationFailedEventHandler(long id)
```

## PeerPacketEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`SceneMultiplayer.PeerPacket`](SceneMultiplayer.md) event of a [`SceneMultiplayer`](SceneMultiplayer.md) class.

### 签名

#### Invoke

```csharp
delegate void PeerPacketEventHandler(long id, byte[] packet)
```
