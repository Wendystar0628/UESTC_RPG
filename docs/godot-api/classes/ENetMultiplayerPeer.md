<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ENetMultiplayerPeer

命名空间 `Godot`；class；继承 [`MultiplayerPeer`](MultiplayerPeer.md) → [`PacketPeer`](PacketPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A MultiplayerPeer implementation that should be passed to [`MultiplayerApi.MultiplayerPeer`](MultiplayerApi.md) after being initialized as either a client, server, or mesh. Events can then be handled by connecting to [`MultiplayerApi`](MultiplayerApi.md) signals. See [`ENetConnection`](ENetConnection.md) for more information on the ENet library wrapper.

**Note:** ENet only uses UDP, not TCP. When forwarding the server port to make your server accessible on the public Internet, you only need to forward the server port in UDP. You can use the [`Upnp`](Upnp.md) class to try to forward the server port automatically when starting the server.

## 构造函数

### ENetMultiplayerPeer

```csharp
public ENetMultiplayerPeer()
```

## 属性

### Host

```csharp
public ENetConnection Host { get; }
```

引擎名：`host`

The underlying [`ENetConnection`](ENetConnection.md) created after `CreateClient()` and `CreateServer()`.

## 方法

### AddMeshPeer

```csharp
public Error AddMeshPeer(int peerId, ENetConnection host)
```

引擎名：`add_mesh_peer`

Add a new remote peer with the given `peerId` connected to the given `host`.

**Note:** The `host` must have exactly one peer in the [`ENetPacketPeer.PeerState.Connected`](ENetPacketPeer.md) state.

### CreateClient

```csharp
public Error CreateClient(string address, int port, int channelCount = 0, int inBandwidth = 0, int outBandwidth = 0, int localPort = 0)
```

引擎名：`create_client`

Create client that connects to a server at `address` using specified `port`. The given address needs to be either a fully qualified domain name (e.g. `"www.example.com"`) or an IP address in IPv4 or IPv6 format (e.g. `"192.168.1.1"`). The `port` is the port the server is listening on. The `channelCount` parameter can be used to specify the number of ENet channels allocated for the connection. The `inBandwidth` and `outBandwidth` parameters can be used to limit the incoming and outgoing bandwidth to the given number of bytes per second. The default of 0 means unlimited bandwidth. Note that ENet will strategically drop packets on specific sides of a connection between peers to ensure the peer's bandwidth is not overwhelmed. The bandwidth parameters also determine the window size of a connection which limits the amount of reliable packets that may be in transit at any given time. Returns [`Error.Ok`](Error.md) if a client was created, [`Error.AlreadyInUse`](Error.md) if this ENetMultiplayerPeer instance already has an open connection (in which case you need to call [`MultiplayerPeer.Close`](MultiplayerPeer.md) first) or [`Error.CantCreate`](Error.md) if the client could not be created. If `localPort` is specified, the client will also listen to the given port; this is useful for some NAT traversal techniques.

### CreateMesh

```csharp
public Error CreateMesh(int uniqueId)
```

引擎名：`create_mesh`

Initialize this [`MultiplayerPeer`](MultiplayerPeer.md) in mesh mode. The provided `uniqueId` will be used as the local peer network unique ID once assigned as the [`MultiplayerApi.MultiplayerPeer`](MultiplayerApi.md). In the mesh configuration you will need to set up each new peer manually using [`ENetConnection`](ENetConnection.md) before calling `AddMeshPeer()`. While this technique is more advanced, it allows for better control over the connection process (e.g. when dealing with NAT punch-through) and for better distribution of the network load (which would otherwise be more taxing on the server).

### CreateServer

```csharp
public Error CreateServer(int port, int maxClients = 32, int maxChannels = 0, int inBandwidth = 0, int outBandwidth = 0)
```

引擎名：`create_server`

Create server that listens to connections via `port`. The port needs to be an available, unused port between 0 and 65535. Note that ports below 1024 are privileged and may require elevated permissions depending on the platform. To change the interface the server listens on, use `SetBindIP()`. The default IP is the wildcard `"*"`, which listens on all available interfaces. `maxClients` is the maximum number of clients that are allowed at once, any number up to 4095 may be used, although the achievable number of simultaneous clients may be far lower and depends on the application. For additional details on the bandwidth parameters, see `CreateClient()`. Returns [`Error.Ok`](Error.md) if a server was created, [`Error.AlreadyInUse`](Error.md) if this ENetMultiplayerPeer instance already has an open connection (in which case you need to call [`MultiplayerPeer.Close`](MultiplayerPeer.md) first) or [`Error.CantCreate`](Error.md) if the server could not be created.

### GetHost

```csharp
public ENetConnection GetHost()
```

引擎名：`get_host`

### GetPeer

```csharp
public ENetPacketPeer GetPeer(int id)
```

引擎名：`get_peer`

Returns the [`ENetPacketPeer`](ENetPacketPeer.md) associated to the given `id`.

### SetBindIP

```csharp
public void SetBindIP(string iP)
```

引擎名：`set_bind_ip`

The IP used when creating a server. This is set to the wildcard `"*"` by default, which binds to all available interfaces. The given IP needs to be in IPv4 or IPv6 address format, for example: `"192.168.1.1"`.
