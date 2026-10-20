<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MultiplayerPeerExtension

命名空间 `Godot`；class；继承 [`MultiplayerPeer`](MultiplayerPeer.md) → [`PacketPeer`](PacketPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class is designed to be inherited from a GDExtension plugin to implement custom networking layers for the multiplayer API (such as WebRTC). All the methods below **must** be implemented to have a working custom multiplayer implementation. See also [`MultiplayerApi`](MultiplayerApi.md).

## 构造函数

### MultiplayerPeerExtension

```csharp
public MultiplayerPeerExtension()
```

## 方法

### _Close

```csharp
public virtual void _Close()
```

引擎名：`_close`

Called when the multiplayer peer should be immediately closed (see [`MultiplayerPeer.Close`](MultiplayerPeer.md)).

### _DisconnectPeer

```csharp
public virtual void _DisconnectPeer(int peer, bool force)
```

引擎名：`_disconnect_peer`

Called when the connected `peer` should be forcibly disconnected (see [`MultiplayerPeer.DisconnectPeer`](MultiplayerPeer.md)).

### _GetAvailablePacketCount

```csharp
public virtual int _GetAvailablePacketCount()
```

引擎名：`_get_available_packet_count`

Called when the available packet count is internally requested by the [`MultiplayerApi`](MultiplayerApi.md).

### _GetConnectionStatus

```csharp
public virtual MultiplayerPeer.ConnectionStatus _GetConnectionStatus()
```

引擎名：`_get_connection_status`

Called when the connection status is requested on the [`MultiplayerPeer`](MultiplayerPeer.md) (see [`MultiplayerPeer.GetConnectionStatus`](MultiplayerPeer.md)).

### _GetMaxPacketSize

```csharp
public virtual int _GetMaxPacketSize()
```

引擎名：`_get_max_packet_size`

Called when the maximum allowed packet size (in bytes) is requested by the [`MultiplayerApi`](MultiplayerApi.md).

### _GetPacketChannel

```csharp
public virtual int _GetPacketChannel()
```

引擎名：`_get_packet_channel`

Called to get the channel over which the next available packet was received. See [`MultiplayerPeer.GetPacketChannel`](MultiplayerPeer.md).

### _GetPacketMode

```csharp
public virtual MultiplayerPeer.TransferModeEnum _GetPacketMode()
```

引擎名：`_get_packet_mode`

Called to get the transfer mode the remote peer used to send the next available packet. See [`MultiplayerPeer.GetPacketMode`](MultiplayerPeer.md).

### _GetPacketPeer

```csharp
public virtual int _GetPacketPeer()
```

引擎名：`_get_packet_peer`

Called when the ID of the [`MultiplayerPeer`](MultiplayerPeer.md) who sent the most recent packet is requested (see [`MultiplayerPeer.GetPacketPeer`](MultiplayerPeer.md)).

### _GetPacketScript

```csharp
public virtual byte[] _GetPacketScript()
```

引擎名：`_get_packet_script`

Called when a packet needs to be received by the [`MultiplayerApi`](MultiplayerApi.md), if `_get_packet` isn't implemented. Use this when extending this class via GDScript.

### _GetTransferChannel

```csharp
public virtual int _GetTransferChannel()
```

引擎名：`_get_transfer_channel`

Called when the transfer channel to use is read on this [`MultiplayerPeer`](MultiplayerPeer.md) (see [`MultiplayerPeer.TransferChannel`](MultiplayerPeer.md)).

### _GetTransferMode

```csharp
public virtual MultiplayerPeer.TransferModeEnum _GetTransferMode()
```

引擎名：`_get_transfer_mode`

Called when the transfer mode to use is read on this [`MultiplayerPeer`](MultiplayerPeer.md) (see [`MultiplayerPeer.TransferMode`](MultiplayerPeer.md)).

### _GetUniqueId

```csharp
public virtual int _GetUniqueId()
```

引擎名：`_get_unique_id`

Called when the unique ID of this [`MultiplayerPeer`](MultiplayerPeer.md) is requested (see [`MultiplayerPeer.GetUniqueId`](MultiplayerPeer.md)). The value must be between `1` and `2147483647`.

### _IsRefusingNewConnections

```csharp
public virtual bool _IsRefusingNewConnections()
```

引擎名：`_is_refusing_new_connections`

Called when the "refuse new connections" status is requested on this [`MultiplayerPeer`](MultiplayerPeer.md) (see [`MultiplayerPeer.RefuseNewConnections`](MultiplayerPeer.md)).

### _IsServer

```csharp
public virtual bool _IsServer()
```

引擎名：`_is_server`

Called when the "is server" status is requested on the [`MultiplayerApi`](MultiplayerApi.md). See [`MultiplayerApi.IsServer`](MultiplayerApi.md).

### _IsServerRelaySupported

```csharp
public virtual bool _IsServerRelaySupported()
```

引擎名：`_is_server_relay_supported`

Called to check if the server can act as a relay in the current configuration. See [`MultiplayerPeer.IsServerRelaySupported`](MultiplayerPeer.md).

### _Poll

```csharp
public virtual void _Poll()
```

引擎名：`_poll`

Called when the [`MultiplayerApi`](MultiplayerApi.md) is polled. See [`MultiplayerApi.Poll`](MultiplayerApi.md).

### _PutPacketScript

```csharp
public virtual Error _PutPacketScript(byte[] buffer)
```

引擎名：`_put_packet_script`

Called when a packet needs to be sent by the [`MultiplayerApi`](MultiplayerApi.md), if `_put_packet` isn't implemented. Use this when extending this class via GDScript.

### _SetRefuseNewConnections

```csharp
public virtual void _SetRefuseNewConnections(bool enable)
```

引擎名：`_set_refuse_new_connections`

Called when the "refuse new connections" status is set on this [`MultiplayerPeer`](MultiplayerPeer.md) (see [`MultiplayerPeer.RefuseNewConnections`](MultiplayerPeer.md)).

### _SetTargetPeer

```csharp
public virtual void _SetTargetPeer(int peer)
```

引擎名：`_set_target_peer`

Called when the target peer to use is set for this [`MultiplayerPeer`](MultiplayerPeer.md) (see [`MultiplayerPeer.SetTargetPeer`](MultiplayerPeer.md)).

### _SetTransferChannel

```csharp
public virtual void _SetTransferChannel(int channel)
```

引擎名：`_set_transfer_channel`

Called when the channel to use is set for this [`MultiplayerPeer`](MultiplayerPeer.md) (see [`MultiplayerPeer.TransferChannel`](MultiplayerPeer.md)).

### _SetTransferMode

```csharp
public virtual void _SetTransferMode(MultiplayerPeer.TransferModeEnum mode)
```

引擎名：`_set_transfer_mode`

Called when the transfer mode is set on this [`MultiplayerPeer`](MultiplayerPeer.md) (see [`MultiplayerPeer.TransferMode`](MultiplayerPeer.md)).
