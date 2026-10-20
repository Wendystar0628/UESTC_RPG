<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PacketPeerDtls

命名空间 `Godot`；class；继承 [`PacketPeer`](PacketPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class represents a DTLS peer connection. It can be used to connect to a DTLS server, and is returned by [`DtlsServer.TakeConnection`](DtlsServer.md).

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

**Warning:** TLS certificate revocation and certificate pinning are currently not supported. Revoked certificates are accepted as long as they are otherwise valid. If this is a concern, you may want to use automatically managed certificates with a short validity period.

## 构造函数

### PacketPeerDtls

```csharp
public PacketPeerDtls()
```

## 方法

### ConnectToPeer

```csharp
public Error ConnectToPeer(PacketPeerUdp packetPeer, string hostname, TlsOptions clientOptions = null)
```

Connects a `packetPeer` beginning the DTLS handshake using the underlying [`PacketPeerUdp`](PacketPeerUdp.md) which must be connected (see [`PacketPeerUdp.ConnectToHost`](PacketPeerUdp.md)). You can optionally specify the `clientOptions` to be used while verifying the TLS connections. See [`TlsOptions.Client`](TlsOptions.md) and [`TlsOptions.ClientUnsafe`](TlsOptions.md).

### DisconnectFromPeer

```csharp
public void DisconnectFromPeer()
```

Disconnects this peer, terminating the DTLS session.

### GetStatus

```csharp
public PacketPeerDtls.Status GetStatus()
```

Returns the status of the connection.

### Poll

```csharp
public void Poll()
```

Poll the connection to check for incoming packets. Call this frequently to update the status and keep the connection working.

## Status

命名空间 `Godot`；enum

### 枚举值

- `Disconnected = 0`：A status representing a [`PacketPeerDtls`](PacketPeerDtls.md) that is disconnected.
- `Handshaking = 1`：A status representing a [`PacketPeerDtls`](PacketPeerDtls.md) that is currently performing the handshake with a remote peer.
- `Connected = 2`：A status representing a [`PacketPeerDtls`](PacketPeerDtls.md) that is connected to a remote peer.
- `Error = 3`：A status representing a [`PacketPeerDtls`](PacketPeerDtls.md) in a generic error state.
- `ErrorHostnameMismatch = 4`：An error status that shows a mismatch in the DTLS certificate domain presented by the host and the domain requested for validation.
