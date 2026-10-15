<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StreamPeerSocket

命名空间 `Godot`；class；继承 [`StreamPeer`](StreamPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

StreamPeerSocket is an abstract base class that defines common behavior for socket-based streams.

## 方法

### DisconnectFromHost

```csharp
public void DisconnectFromHost()
```

引擎名：`disconnect_from_host`

Disconnects from host.

### GetStatus

```csharp
public StreamPeerSocket.Status GetStatus()
```

引擎名：`get_status`

Returns the status of the connection.

### Poll

```csharp
public Error Poll()
```

引擎名：`poll`

Polls the socket, updating its state. See `GetStatus()`.

## Status

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：The initial status of the [`StreamPeerSocket`](StreamPeerSocket.md).
- `Connecting = 1`：A status representing a [`StreamPeerSocket`](StreamPeerSocket.md) that is connecting to a host.
- `Connected = 2`：A status representing a [`StreamPeerSocket`](StreamPeerSocket.md) that is connected to a host.
- `Error = 3`：A status representing a [`StreamPeerSocket`](StreamPeerSocket.md) in error state.
