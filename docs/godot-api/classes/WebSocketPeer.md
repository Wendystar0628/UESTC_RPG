<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# WebSocketPeer

命名空间 `Godot`；class；继承 [`PacketPeer`](PacketPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class represents WebSocket connection, and can be used as a WebSocket client ([RFC 6455](https://datatracker.ietf.org/doc/html/rfc6455)-compliant) or as a remote peer of a WebSocket server.

You can send WebSocket binary frames using [`PacketPeer.PutPacket`](PacketPeer.md), and WebSocket text frames using `Send()` (prefer text frames when interacting with text-based API). You can check the frame type of the last packet via `WasStringPacket()`.

To start a WebSocket client, first call `ConnectToUrl()`, then regularly call `Poll()` (e.g. during [`Node`](Node.md) process). You can query the socket state via `GetReadyState()`, get the number of pending packets using [`PacketPeer.GetAvailablePacketCount`](PacketPeer.md), and retrieve them via [`PacketPeer.GetPacket`](PacketPeer.md).

To use the peer as part of a WebSocket server refer to `AcceptStream()` and the online tutorial.

## 构造函数

### WebSocketPeer

```csharp
public WebSocketPeer()
```

## 属性

### HandshakeHeaders

```csharp
public string[] HandshakeHeaders { get; set; }
```

引擎名：`handshake_headers`

The extra HTTP headers to be sent during the WebSocket handshake.

**Note:** Not supported in Web exports due to browsers' restrictions.

### HeartbeatInterval

```csharp
public double HeartbeatInterval { get; set; }
```

引擎名：`heartbeat_interval`

The interval (in seconds) at which the peer will automatically send WebSocket "ping" control frames. When set to `0`, no "ping" control frames will be sent.

**Note:** Has no effect in Web exports due to browser restrictions.

### InboundBufferSize

```csharp
public int InboundBufferSize { get; set; }
```

引擎名：`inbound_buffer_size`

The size of the input buffer in bytes (roughly the maximum amount of memory that will be allocated for the inbound packets).

### MaxQueuedPackets

```csharp
public int MaxQueuedPackets { get; set; }
```

引擎名：`max_queued_packets`

The maximum amount of packets that will be allowed in the queues (both inbound and outbound).

### OutboundBufferSize

```csharp
public int OutboundBufferSize { get; set; }
```

引擎名：`outbound_buffer_size`

The size of the input buffer in bytes (roughly the maximum amount of memory that will be allocated for the outbound packets).

### SupportedProtocols

```csharp
public string[] SupportedProtocols { get; set; }
```

引擎名：`supported_protocols`

The WebSocket sub-protocols allowed during the WebSocket handshake.

## 方法

### AcceptStream

```csharp
public Error AcceptStream(StreamPeer stream)
```

引擎名：`accept_stream`

Accepts a peer connection performing the HTTP handshake as a WebSocket server. The `stream` must be a valid TCP stream retrieved via [`TcpServer.TakeConnection`](TcpServer.md), or a TLS stream accepted via [`StreamPeerTls.AcceptStream`](StreamPeerTls.md).

**Note:** Not supported in Web exports due to browsers' restrictions.

### Close

```csharp
public void Close(int code = 1000, string reason = "")
```

引擎名：`close`

Closes this WebSocket connection.

`code` is the status code for the closure (see [RFC 6455 section 7.4](https://datatracker.ietf.org/doc/html/rfc6455#section-7.4.1) for a list of valid status codes). If `code` is negative, the connection will be closed immediately without notifying the remote peer.

`reason` is the human-readable reason for closing the connection. It can be any UTF-8 string that's smaller than 123 bytes.

**Note:** To achieve a clean closure, you will need to keep polling until [`WebSocketPeer.State.Closed`](WebSocketPeer.md) is reached.

**Note:** The Web export might not support all status codes. Please refer to browser-specific documentation for more details.

### ConnectToUrl

```csharp
public Error ConnectToUrl(string url, TlsOptions tlsClientOptions = null)
```

引擎名：`connect_to_url`

Connects to the given URL. TLS certificates will be verified against the hostname when connecting using the `wss://` protocol. You can pass the optional `tlsClientOptions` parameter to customize the trusted certification authorities, or disable the common name verification. See [`TlsOptions.Client`](TlsOptions.md) and [`TlsOptions.ClientUnsafe`](TlsOptions.md).

**Note:** This method is non-blocking, and will return [`Error.Ok`](Error.md) before the connection is established as long as the provided parameters are valid and the peer is not in an invalid state (e.g. already connected). Regularly call `Poll()` (e.g. during [`Node`](Node.md) process) and check the result of `GetReadyState()` to know whether the connection succeeds or fails.

**Note:** To avoid mixed content warnings or errors in Web, you may have to use a `url` that starts with `wss://` (secure) instead of `ws://`. When doing so, make sure to use the fully qualified domain name that matches the one defined in the server's TLS certificate. Do not connect directly via the IP address for `wss://` connections, as it won't match with the TLS certificate.

### GetCloseCode

```csharp
public int GetCloseCode()
```

引擎名：`get_close_code`

Returns the received WebSocket close frame status code, or `-1` when the connection was not cleanly closed. Only call this method when `GetReadyState()` returns [`WebSocketPeer.State.Closed`](WebSocketPeer.md).

### GetCloseReason

```csharp
public string GetCloseReason()
```

引擎名：`get_close_reason`

Returns the received WebSocket close frame status reason string. Only call this method when `GetReadyState()` returns [`WebSocketPeer.State.Closed`](WebSocketPeer.md).

### GetConnectedHost

```csharp
public string GetConnectedHost()
```

引擎名：`get_connected_host`

Returns the IP address of the connected peer.

**Note:** Not available in the Web export.

### GetConnectedPort

```csharp
public ushort GetConnectedPort()
```

引擎名：`get_connected_port`

Returns the remote port of the connected peer.

**Note:** Not available in the Web export.

### GetCurrentOutboundBufferedAmount

```csharp
public int GetCurrentOutboundBufferedAmount()
```

引擎名：`get_current_outbound_buffered_amount`

Returns the current amount of data in the outbound websocket buffer. **Note:** Web exports use WebSocket.bufferedAmount, while other platforms use an internal buffer.

### GetHandshakeHeaders

```csharp
public string[] GetHandshakeHeaders()
```

引擎名：`get_handshake_headers`

### GetHeartbeatInterval

```csharp
public double GetHeartbeatInterval()
```

引擎名：`get_heartbeat_interval`

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

### GetReadyState

```csharp
public WebSocketPeer.State GetReadyState()
```

引擎名：`get_ready_state`

Returns the ready state of the connection.

### GetRequestedUrl

```csharp
public string GetRequestedUrl()
```

引擎名：`get_requested_url`

Returns the URL requested by this peer. The URL is derived from the `url` passed to `ConnectToUrl()` or from the HTTP headers when acting as server (i.e. when using `AcceptStream()`).

### GetSelectedProtocol

```csharp
public string GetSelectedProtocol()
```

引擎名：`get_selected_protocol`

Returns the selected WebSocket sub-protocol for this connection or an empty string if the sub-protocol has not been selected yet.

### GetSupportedProtocols

```csharp
public string[] GetSupportedProtocols()
```

引擎名：`get_supported_protocols`

### Poll

```csharp
public void Poll()
```

引擎名：`poll`

Updates the connection state and receive incoming packets. Call this function regularly to keep it in a clean state.

### Send

```csharp
public Error Send(ReadOnlySpan<byte> message, WebSocketPeer.WriteMode writeMode)
```

引擎名：`send`

Sends the given `message` using the desired `writeMode`. When sending a `System.String`, prefer using `SendText()`.

### Send

```csharp
public Error Send(byte[] message, WebSocketPeer.WriteMode writeMode = WebSocketPeer.WriteMode.Binary)
```

引擎名：`send`

Sends the given `message` using the desired `writeMode`. When sending a `System.String`, prefer using `SendText()`.

### SendText

```csharp
public Error SendText(string message)
```

引擎名：`send_text`

Sends the given `message` using WebSocket text mode. Prefer this method over [`PacketPeer.PutPacket`](PacketPeer.md) when interacting with third-party text-based API (e.g. when using [`Json`](Json.md) formatted messages).

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

### SetHeartbeatInterval

```csharp
public void SetHeartbeatInterval(double interval)
```

引擎名：`set_heartbeat_interval`

### SetInboundBufferSize

```csharp
public void SetInboundBufferSize(int bufferSize)
```

引擎名：`set_inbound_buffer_size`

### SetMaxQueuedPackets

```csharp
public void SetMaxQueuedPackets(int bufferSize)
```

引擎名：`set_max_queued_packets`

### SetNoDelay

```csharp
public void SetNoDelay(bool enabled)
```

引擎名：`set_no_delay`

Disable Nagle's algorithm on the underlying TCP socket (default). See [`StreamPeerTcp.SetNoDelay`](StreamPeerTcp.md) for more information.

**Note:** Not available in the Web export.

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

### WasStringPacket

```csharp
public bool WasStringPacket()
```

引擎名：`was_string_packet`

Returns `true` if the last received packet was sent as a text payload. See [`WebSocketPeer.WriteMode`](WebSocketPeer.md).

## State

命名空间 `Godot`；enum

### 枚举值

- `Connecting = 0`：Socket has been created.
- `Open = 1`：The connection is open and ready to communicate.
- `Closing = 2`：The connection is in the process of closing.
- `Closed = 3`：The connection is closed or couldn't be opened.

## WriteMode

命名空间 `Godot`；enum

### 枚举值

- `Text = 0`：Specifies that WebSockets messages should be transferred as text payload (only valid UTF-8 is allowed).
- `Binary = 1`：Specifies that WebSockets messages should be transferred as binary payload (any byte combination is allowed).
