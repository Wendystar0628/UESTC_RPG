<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ENetConnection

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

ENet's purpose is to provide a relatively thin, simple and robust network communication layer on top of UDP (User Datagram Protocol).

## 构造函数

### ENetConnection

```csharp
public ENetConnection()
```

## 方法

### BandwidthLimit

```csharp
public void BandwidthLimit(int inBandwidth = 0, int outBandwidth = 0)
```

引擎名：`bandwidth_limit`

Adjusts the bandwidth limits of a host.

### Broadcast

```csharp
public void Broadcast(int channel, ReadOnlySpan<byte> packet, int flags)
```

引擎名：`broadcast`

Queues a `packet` to be sent to all peers associated with the host over the specified `channel`. See [`ENetPacketPeer`](ENetPacketPeer.md) `FLAG_*` constants for available packet flags.

### Broadcast

```csharp
public void Broadcast(int channel, byte[] packet, int flags)
```

引擎名：`broadcast`

Queues a `packet` to be sent to all peers associated with the host over the specified `channel`. See [`ENetPacketPeer`](ENetPacketPeer.md) `FLAG_*` constants for available packet flags.

### ChannelLimit

```csharp
public void ChannelLimit(int limit)
```

引擎名：`channel_limit`

Limits the maximum allowed channels of future incoming connections.

### Compress

```csharp
public void Compress(ENetConnection.CompressionMode mode)
```

引擎名：`compress`

Sets the compression method used for network packets. These have different tradeoffs of compression speed versus bandwidth, you may need to test which one works best for your use case if you use compression at all.

**Note:** Most games' network design involve sending many small packets frequently (smaller than 4 KB each). If in doubt, it is recommended to keep the default compression algorithm as it works best on these small packets.

**Note:** The compression mode must be set to the same value on both the server and all its clients. Clients will fail to connect if the compression mode set on the client differs from the one set on the server.

### ConnectToHost

```csharp
public ENetPacketPeer ConnectToHost(string address, int port, int channels = 0, int data = 0)
```

引擎名：`connect_to_host`

Initiates a connection to a foreign `address` using the specified `port` and allocating the requested `channels`. Optional `data` can be passed during connection in the form of a 32 bit integer.

**Note:** You must call either `CreateHost()` or `CreateHostBound()` on both ends before calling this method.

### CreateHost

```csharp
public Error CreateHost(int maxPeers = 32, int maxChannels = 0, int inBandwidth = 0, int outBandwidth = 0)
```

引擎名：`create_host`

Creates an ENetHost that allows up to `maxPeers` connected peers, each allocating up to `maxChannels` channels, optionally limiting bandwidth to `inBandwidth` and `outBandwidth` (if greater than zero).

This method binds a random available dynamic UDP port on the host machine at the *unspecified* address. Use `CreateHostBound()` to specify the address and port.

**Note:** It is necessary to create a host in both client and server in order to establish a connection.

### CreateHostBound

```csharp
public Error CreateHostBound(string bindAddress, int bindPort, int maxPeers = 32, int maxChannels = 0, int inBandwidth = 0, int outBandwidth = 0)
```

引擎名：`create_host_bound`

Creates an ENetHost bound to the given `bindAddress` and `bindPort` that allows up to `maxPeers` connected peers, each allocating up to `maxChannels` channels, optionally limiting bandwidth to `inBandwidth` and `outBandwidth` (if greater than zero).

**Note:** It is necessary to create a host in both client and server in order to establish a connection.

### Destroy

```csharp
public void Destroy()
```

引擎名：`destroy`

Destroys the host and all resources associated with it.

### DtlsClientSetup

```csharp
public Error DtlsClientSetup(string hostname, TlsOptions clientOptions = null)
```

引擎名：`dtls_client_setup`

Configure this ENetHost to use the custom Godot extension allowing DTLS encryption for ENet clients. Call this before `ConnectToHost()` to have ENet connect using DTLS validating the server certificate against `hostname`. You can pass the optional `clientOptions` parameter to customize the trusted certification authorities, or disable the common name verification. See [`TlsOptions.Client`](TlsOptions.md) and [`TlsOptions.ClientUnsafe`](TlsOptions.md).

### DtlsServerSetup

```csharp
public Error DtlsServerSetup(TlsOptions serverOptions)
```

引擎名：`dtls_server_setup`

Configure this ENetHost to use the custom Godot extension allowing DTLS encryption for ENet servers. Call this right after `CreateHostBound()` to have ENet expect peers to connect using DTLS. See [`TlsOptions.Server`](TlsOptions.md).

### Flush

```csharp
public void Flush()
```

引擎名：`flush`

Sends any queued packets on the host specified to its designated peers.

### GetLocalPort

```csharp
public int GetLocalPort()
```

引擎名：`get_local_port`

Returns the local port to which this peer is bound.

### GetMaxChannels

```csharp
public int GetMaxChannels()
```

引擎名：`get_max_channels`

Returns the maximum number of channels allowed for connected peers.

### GetPeers

```csharp
public Array<ENetPacketPeer> GetPeers()
```

引擎名：`get_peers`

Returns the list of peers associated with this host.

**Note:** This list might include some peers that are not fully connected or are still being disconnected.

### PopStatistic

```csharp
public double PopStatistic(ENetConnection.HostStatistic statistic)
```

引擎名：`pop_statistic`

Returns and resets host statistics.

### RefuseNewConnections

```csharp
public void RefuseNewConnections(bool refuse)
```

引擎名：`refuse_new_connections`

Configures the DTLS server to automatically drop new connections.

**Note:** This method is only relevant after calling `DtlsServerSetup()`.

### Service

```csharp
public Array Service(int timeout = 0)
```

引擎名：`service`

Waits for events on this connection and shuttles packets between the host and its peers, with the given `timeout` (in milliseconds). The returned [`Collections.Array`](Collections.Array.md) will have 4 elements. An [`ENetConnection.EventType`](ENetConnection.md), the [`ENetPacketPeer`](ENetPacketPeer.md) which generated the event, the event associated data (if any), the event associated channel (if any). If the generated event is [`ENetConnection.EventType.Receive`](ENetConnection.md), the received packet will be queued to the associated [`ENetPacketPeer`](ENetPacketPeer.md).

Call this function regularly to handle connections, disconnections, and to receive new packets.

**Note:** This method must be called on both ends involved in the event (sending and receiving hosts).

### SocketSend

```csharp
public void SocketSend(string destinationAddress, int destinationPort, ReadOnlySpan<byte> packet)
```

引擎名：`socket_send`

Sends a `packet` toward a destination from the address and port currently bound by this ENetConnection instance.

This is useful as it serves to establish entries in NAT routing tables on all devices between this bound instance and the public facing internet, allowing a prospective client's connection packets to be routed backward through the NAT device(s) between the public internet and this host.

This requires forward knowledge of a prospective client's address and communication port as seen by the public internet - after any NAT devices have handled their connection request. This information can be obtained by a [STUN](https://en.wikipedia.org/wiki/STUN) service, and must be handed off to your host by an entity that is not the prospective client. This will never work for a client behind a Symmetric NAT due to the nature of the Symmetric NAT routing algorithm, as their IP and Port cannot be known beforehand.

### SocketSend

```csharp
public void SocketSend(string destinationAddress, int destinationPort, byte[] packet)
```

引擎名：`socket_send`

Sends a `packet` toward a destination from the address and port currently bound by this ENetConnection instance.

This is useful as it serves to establish entries in NAT routing tables on all devices between this bound instance and the public facing internet, allowing a prospective client's connection packets to be routed backward through the NAT device(s) between the public internet and this host.

This requires forward knowledge of a prospective client's address and communication port as seen by the public internet - after any NAT devices have handled their connection request. This information can be obtained by a [STUN](https://en.wikipedia.org/wiki/STUN) service, and must be handed off to your host by an entity that is not the prospective client. This will never work for a client behind a Symmetric NAT due to the nature of the Symmetric NAT routing algorithm, as their IP and Port cannot be known beforehand.

## CompressionMode

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No compression.
- `RangeCoder = 1`：ENet's built-in range encoding.
- `Fastlz = 2`：[FastLZ](https://fastlz.org/) compression.
- `Zlib = 3`：[Zlib](https://www.zlib.net/) compression.
- `Zstd = 4`：[Zstandard](https://facebook.github.io/zstd/) compression.

## EventType

命名空间 `Godot`；enum

### 枚举值

- `Error = -1`：An error occurred during [`ENetConnection.Service`](ENetConnection.md).
- `None = 0`：No event occurred within the specified time limit.
- `Connect = 1`：A connection request initiated by enet_host_connect has completed.
- `Disconnect = 2`：A peer has disconnected.
- `Receive = 3`：A packet has been received from a peer.

## HostStatistic

命名空间 `Godot`；enum

### 枚举值

- `SentData = 0`：Total data sent.
- `SentPackets = 1`：Total UDP packets sent.
- `ReceivedData = 2`：Total data received.
- `ReceivedPackets = 3`：Total UDP packets received.
