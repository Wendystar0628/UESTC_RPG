<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StreamPeerTls

命名空间 `Godot`；class；继承 [`StreamPeer`](StreamPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A stream peer that handles TLS connections. This object can be used to connect to a TLS server or accept a single TLS client connection.

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## 构造函数

### StreamPeerTls

```csharp
public StreamPeerTls()
```

## 方法

### AcceptStream

```csharp
public Error AcceptStream(StreamPeer stream, TlsOptions serverOptions)
```

Accepts a peer connection as a server using the given `serverOptions`. See [`TlsOptions.Server`](TlsOptions.md).

### ConnectToStream

```csharp
public Error ConnectToStream(StreamPeer stream, string commonName, TlsOptions clientOptions = null)
```

Connects to a peer using an underlying [`StreamPeer`](StreamPeer.md) `stream` and verifying the remote certificate is correctly signed for the given `commonName`. You can pass the optional `clientOptions` parameter to customize the trusted certification authorities, or disable the common name verification. See [`TlsOptions.Client`](TlsOptions.md) and [`TlsOptions.ClientUnsafe`](TlsOptions.md).

### DisconnectFromStream

```csharp
public void DisconnectFromStream()
```

Disconnects from host.

### GetStatus

```csharp
public StreamPeerTls.Status GetStatus()
```

Returns the status of the connection.

### GetStream

```csharp
public StreamPeer GetStream()
```

Returns the underlying [`StreamPeer`](StreamPeer.md) connection, used in `AcceptStream()` or `ConnectToStream()`.

### Poll

```csharp
public void Poll()
```

Poll the connection to check for incoming bytes. Call this right before [`StreamPeer.GetAvailableBytes`](StreamPeer.md) for it to work properly.

## Status

命名空间 `Godot`；enum

### 枚举值

- `Disconnected = 0`：A status representing a [`StreamPeerTls`](StreamPeerTls.md) that is disconnected.
- `Handshaking = 1`：A status representing a [`StreamPeerTls`](StreamPeerTls.md) during handshaking.
- `Connected = 2`：A status representing a [`StreamPeerTls`](StreamPeerTls.md) that is connected to a host.
- `Error = 3`：A status representing a [`StreamPeerTls`](StreamPeerTls.md) in error state.
- `ErrorHostnameMismatch = 4`：An error status that shows a mismatch in the TLS certificate domain presented by the host and the domain requested for validation.
