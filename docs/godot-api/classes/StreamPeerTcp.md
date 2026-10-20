<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StreamPeerTcp

命名空间 `Godot`；class；继承 [`StreamPeerSocket`](StreamPeerSocket.md) → [`StreamPeer`](StreamPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A stream peer that handles TCP connections. This object can be used to connect to TCP servers, or also is returned by a TCP server.

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## 构造函数

### StreamPeerTcp

```csharp
public StreamPeerTcp()
```

## 方法

### Bind

```csharp
public Error Bind(int port, string host = "*")
```

Opens the TCP socket, and binds it to the specified local address.

This method is generally not needed, and only used to force the subsequent call to `ConnectToHost()` to use the specified `host` and `port` as source address. This can be desired in some NAT punchthrough techniques, or when forcing the source network interface.

### ConnectToHost

```csharp
public Error ConnectToHost(string host, int port)
```

Connects to the specified `host:port` pair. A hostname will be resolved if valid. Returns [`Error.Ok`](Error.md) on success.

### GetConnectedHost

```csharp
public string GetConnectedHost()
```

Returns the IP of this peer.

### GetConnectedPort

```csharp
public int GetConnectedPort()
```

Returns the port of this peer.

### GetLocalPort

```csharp
public int GetLocalPort()
```

Returns the local port to which this peer is bound.

### SetNoDelay

```csharp
public void SetNoDelay(bool enabled)
```

If `enabled` is `true`, packets will be sent immediately. If `enabled` is `false` (the default), packet transfers will be delayed and combined using [Nagle's algorithm](https://en.wikipedia.org/wiki/Nagle%27s_algorithm).

**Note:** It's recommended to leave this disabled for applications that send large packets or need to transfer a lot of data, as enabling this can decrease the total available bandwidth.
