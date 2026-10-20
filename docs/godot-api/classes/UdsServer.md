<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# UdsServer

命名空间 `Godot`；class；继承 [`SocketServer`](SocketServer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A Unix Domain Socket (UDS) server. Listens to connections on a socket path and returns a [`StreamPeerUds`](StreamPeerUds.md) when it gets an incoming connection. Unix Domain Sockets provide inter-process communication on the same machine using the filesystem namespace.

**Note:** Unix Domain Sockets are only available on Unix-like systems (Linux, macOS, etc.) and are not supported on Windows.

## 构造函数

### UdsServer

```csharp
public UdsServer()
```

## 方法

### Listen

```csharp
public Error Listen(string path)
```

Listens on the socket at `path`. The socket file will be created at the specified path.

**Note:** The socket file must not already exist at the specified path. You may need to remove any existing socket file before calling this method.

### TakeConnection

```csharp
public StreamPeerUds TakeConnection()
```

If a connection is available, returns a StreamPeerUDS with the connection.
