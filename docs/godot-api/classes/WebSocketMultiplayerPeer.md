<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# WebSocketMultiplayerPeer

命名空间 `Godot`；class；继承 [`MultiplayerPeer`](MultiplayerPeer.md) → [`PacketPeer`](PacketPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Base class for WebSocket server and client, allowing them to be used as multiplayer peer for the [`MultiplayerApi`](MultiplayerApi.md).

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## 构造函数

### WebSocketMultiplayerPeer

```csharp
public WebSocketMultiplayerPeer()
```

## 属性

### HandshakeHeaders

```csharp
public string[] HandshakeHeaders { get; set; }
```

引擎名：`handshake_headers`

The extra headers to use during handshake. See [`WebSocketPeer.HandshakeHeaders`](WebSocketPeer.md) for more details.

### HandshakeTimeout

```csharp
public float HandshakeTimeout { get; set; }
```

引擎名：`handshake_timeout`

The maximum time each peer can stay in a connecting state before being dropped.

### InboundBufferSize

```csharp
public int InboundBufferSize { get; set; }
```

引擎名：`inbound_buffer_size`

The inbound buffer size for connected peers. See [`WebSocketPeer.InboundBufferSize`](WebSocketPeer.md) for more details.

### MaxQueuedPackets

```csharp
public int MaxQueuedPackets { get; set; }
```

引擎名：`max_queued_packets`

The maximum number of queued packets for connected peers. See [`WebSocketPeer.MaxQueuedPackets`](WebSocketPeer.md) for more details.

### OutboundBufferSize

```csharp
public int OutboundBufferSize { get; set; }
```

引擎名：`outbound_buffer_size`

The outbound buffer size for connected peers. See [`WebSocketPeer.OutboundBufferSize`](WebSocketPeer.md) for more details.

### SupportedProtocols

```csharp
public string[] SupportedProtocols { get; set; }
```

引擎名：`supported_protocols`

The supported WebSocket sub-protocols. See [`WebSocketPeer.SupportedProtocols`](WebSocketPeer.md) for more details.

## 方法

### CreateClient

```csharp
public Error CreateClient(string url, TlsOptions tlsClientOptions = null)
```

引擎名：`create_client`

Starts a new multiplayer client connecting to the given `url`. TLS certificates will be verified against the hostname when connecting using the `wss://` protocol. You can pass the optional `tlsClientOptions` parameter to customize the trusted certification authorities, or disable the common name verification. See [`TlsOptions.Client`](TlsOptions.md) and [`TlsOptions.ClientUnsafe`](TlsOptions.md).

**Note:** It is recommended to specify the scheme part of the URL, i.e. the `url` should start with either `ws://` or `wss://`.

### CreateServer

```csharp
public Error CreateServer(int port, string bindAddress = "*", TlsOptions tlsServerOptions = null)
```

引擎名：`create_server`

Starts a new multiplayer server listening on the given `port`. You can optionally specify a `bindAddress`, and provide valid `tlsServerOptions` to use TLS. See [`TlsOptions.Server`](TlsOptions.md).

### GetHandshakeHeaders

```csharp
public string[] GetHandshakeHeaders()
```

引擎名：`get_handshake_headers`

### GetHandshakeTimeout

```csharp
public float GetHandshakeTimeout()
```

引擎名：`get_handshake_timeout`

### GetInboundBufferSize

```csharp
public int GetInboundBufferSize()
```

引擎名：`get_inbound_buffer_size`

### GetMaxQueuedPackets

```csharp
public int GetMaxQueuedPackets()
```

引擎名：`get_max_queued_packets`

### GetOutboundBufferSize

```csharp
public int GetOutboundBufferSize()
```

引擎名：`get_outbound_buffer_size`

### GetPeer

```csharp
public WebSocketPeer GetPeer(int peerId)
```

引擎名：`get_peer`

Returns the [`WebSocketPeer`](WebSocketPeer.md) associated to the given `peerId`.

### GetPeerAddress

```csharp
public string GetPeerAddress(int id)
```

引擎名：`get_peer_address`

Returns the IP address of the given peer.

### GetPeerPort

```csharp
public int GetPeerPort(int id)
```

引擎名：`get_peer_port`

Returns the remote port of the given peer.

### GetSupportedProtocols

```csharp
public string[] GetSupportedProtocols()
```

引擎名：`get_supported_protocols`

### SetHandshakeHeaders

```csharp
public void SetHandshakeHeaders(ReadOnlySpan<string> protocols)
```

引擎名：`set_handshake_headers`

### SetHandshakeHeaders

```csharp
public void SetHandshakeHeaders(string[] protocols)
```

引擎名：`set_handshake_headers`

### SetHandshakeTimeout

```csharp
public void SetHandshakeTimeout(float timeout)
```

引擎名：`set_handshake_timeout`

### SetInboundBufferSize

```csharp
public void SetInboundBufferSize(int bufferSize)
```

引擎名：`set_inbound_buffer_size`

### SetMaxQueuedPackets

```csharp
public void SetMaxQueuedPackets(int maxQueuedPackets)
```

引擎名：`set_max_queued_packets`

### SetOutboundBufferSize

```csharp
public void SetOutboundBufferSize(int bufferSize)
```

引擎名：`set_outbound_buffer_size`

### SetSupportedProtocols

```csharp
public void SetSupportedProtocols(ReadOnlySpan<string> protocols)
```

引擎名：`set_supported_protocols`

### SetSupportedProtocols

```csharp
public void SetSupportedProtocols(string[] protocols)
```

引擎名：`set_supported_protocols`
