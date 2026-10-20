<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SocketServer

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A socket server.

## 方法

### IsConnectionAvailable

```csharp
public bool IsConnectionAvailable()
```

引擎名：`is_connection_available`

Returns `true` if a connection is available for taking.

### IsListening

```csharp
public bool IsListening()
```

引擎名：`is_listening`

Returns `true` if the server is currently listening for connections.

### Stop

```csharp
public void Stop()
```

引擎名：`stop`

Stops listening.

### TakeSocketConnection

```csharp
public StreamPeerSocket TakeSocketConnection()
```

引擎名：`take_socket_connection`

If a connection is available, returns a StreamPeerSocket with the connection.
