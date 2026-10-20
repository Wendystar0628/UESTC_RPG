<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# WebRtcMultiplayerPeer

命名空间 `Godot`；class；继承 [`MultiplayerPeer`](MultiplayerPeer.md) → [`PacketPeer`](PacketPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class constructs a full mesh of [`WebRtcPeerConnection`](WebRtcPeerConnection.md) (one connection for each peer) that can be used as a [`MultiplayerApi.MultiplayerPeer`](MultiplayerApi.md).

You can add each [`WebRtcPeerConnection`](WebRtcPeerConnection.md) via `AddPeer()` or remove them via `RemovePeer()`. Peers must be added in [`WebRtcPeerConnection.ConnectionState.New`](WebRtcPeerConnection.md) state to allow it to create the appropriate channels. This class will not create offers nor set descriptions, it will only poll them, and notify connections and disconnections.

When creating the peer via `CreateClient()` or `CreateServer()` the [`MultiplayerPeer.IsServerRelaySupported`](MultiplayerPeer.md) method will return `true` enabling peer exchange and packet relaying when supported by the [`MultiplayerApi`](MultiplayerApi.md) implementation.

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## 构造函数

### WebRtcMultiplayerPeer

```csharp
public WebRtcMultiplayerPeer()
```

## 方法

### AddPeer

```csharp
public Error AddPeer(WebRtcPeerConnection peer, int peerId, int unreliableLifetime = 1)
```

Add a new peer to the mesh with the given `peerId`. The [`WebRtcPeerConnection`](WebRtcPeerConnection.md) must be in state [`WebRtcPeerConnection.ConnectionState.New`](WebRtcPeerConnection.md).

Three channels will be created for reliable, unreliable, and ordered transport. The value of `unreliableLifetime` will be passed to the `"maxPacketLifetime"` option when creating unreliable and ordered channels (see [`WebRtcPeerConnection.CreateDataChannel`](WebRtcPeerConnection.md)).

### CreateClient

```csharp
public Error CreateClient(int peerId, Array channelsConfig = null)
```

Initialize the multiplayer peer as a client with the given `peerId` (must be between 2 and 2147483647). In this mode, you should only call `AddPeer()` once and with `peerId` of `1`. This mode enables [`MultiplayerPeer.IsServerRelaySupported`](MultiplayerPeer.md), allowing the upper [`MultiplayerApi`](MultiplayerApi.md) layer to perform peer exchange and packet relaying.

You can optionally specify a `channelsConfig` array of [`MultiplayerPeer.TransferModeEnum`](MultiplayerPeer.md) which will be used to create extra channels (WebRTC only supports one transfer mode per channel).

### CreateMesh

```csharp
public Error CreateMesh(int peerId, Array channelsConfig = null)
```

Initialize the multiplayer peer as a mesh (i.e. all peers connect to each other) with the given `peerId` (must be between 1 and 2147483647).

### CreateServer

```csharp
public Error CreateServer(Array channelsConfig = null)
```

Initialize the multiplayer peer as a server (with unique ID of `1`). This mode enables [`MultiplayerPeer.IsServerRelaySupported`](MultiplayerPeer.md), allowing the upper [`MultiplayerApi`](MultiplayerApi.md) layer to perform peer exchange and packet relaying.

You can optionally specify a `channelsConfig` array of [`MultiplayerPeer.TransferModeEnum`](MultiplayerPeer.md) which will be used to create extra channels (WebRTC only supports one transfer mode per channel).

### GetPeer

```csharp
public Dictionary GetPeer(int peerId)
```

Returns a dictionary representation of the peer with given `peerId` with three keys. `"connection"` containing the [`WebRtcPeerConnection`](WebRtcPeerConnection.md) to this peer, `"channels"` an array of three [`WebRtcDataChannel`](WebRtcDataChannel.md), and `"connected"` a boolean representing if the peer connection is currently connected (all three channels are open).

### GetPeers

```csharp
public Dictionary GetPeers()
```

Returns a dictionary which keys are the peer ids and values the peer representation as in `GetPeer()`.

### HasPeer

```csharp
public bool HasPeer(int peerId)
```

Returns `true` if the given `peerId` is in the peers map (it might not be connected though).

### RemovePeer

```csharp
public void RemovePeer(int peerId)
```

Remove the peer with given `peerId` from the mesh. If the peer was connected, and [`MultiplayerPeer.PeerConnected`](MultiplayerPeer.md) was emitted for it, then [`MultiplayerPeer.PeerDisconnected`](MultiplayerPeer.md) will be emitted.
