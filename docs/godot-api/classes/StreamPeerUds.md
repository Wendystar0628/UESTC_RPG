<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StreamPeerUds

命名空间 `Godot`；class；继承 [`StreamPeerSocket`](StreamPeerSocket.md) → [`StreamPeer`](StreamPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A stream peer that handles UNIX Domain Socket (UDS) connections. This object can be used to connect to UDS servers, or also is returned by a UDS server. Unix Domain Sockets provide inter-process communication on the same machine using the filesystem namespace.

**Note:** UNIX Domain Sockets are only available on UNIX-like systems (Linux, macOS, etc.) and are not supported on Windows.

## 构造函数

### StreamPeerUds

```csharp
public StreamPeerUds()
```

## 方法

### Bind

```csharp
public Error Bind(string path)
```

Opens the UDS socket, and binds it to the specified socket path.

This method is generally not needed, and only used to force the subsequent call to `ConnectToHost()` to use the specified `path` as the source address.

### ConnectToHost

```csharp
public Error ConnectToHost(string path)
```

Connects to the specified UNIX Domain Socket path. Returns [`Error.Ok`](Error.md) on success.

### GetConnectedPath

```csharp
public string GetConnectedPath()
```

Returns the socket path of this peer.
