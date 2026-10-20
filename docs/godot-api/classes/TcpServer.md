<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TcpServer

命名空间 `Godot`；class；继承 [`SocketServer`](SocketServer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A TCP server. Listens to connections on a port and returns a [`StreamPeerTcp`](StreamPeerTcp.md) when it gets an incoming connection.

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## 构造函数

### TcpServer

```csharp
public TcpServer()
```

## 方法

### GetLocalPort

```csharp
public int GetLocalPort()
```

Returns the local port this server is listening to.

### Listen

```csharp
public Error Listen(ushort port, string bindAddress = "*")
```

Listen on the `port` binding to `bindAddress`.

If `bindAddress` is set as `"*"` (default), the server will listen on all available addresses (both IPv4 and IPv6).

If `bindAddress` is set as `"0.0.0.0"` (for IPv4) or `"::"` (for IPv6), the server will listen on all available addresses matching that IP type.

If `bindAddress` is set to any valid address (e.g. `"192.168.1.101"`, `"::1"`, etc.), the server will only listen on the interface with that address (or fail if no interface with the given address exists).

### TakeConnection

```csharp
public StreamPeerTcp TakeConnection()
```

If a connection is available, returns a StreamPeerTCP with the connection.
