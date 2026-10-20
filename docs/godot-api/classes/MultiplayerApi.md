<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MultiplayerApi

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Base class for high-level multiplayer API implementations. See also [`MultiplayerPeer`](MultiplayerPeer.md).

By default, [`SceneTree`](SceneTree.md) has a reference to an implementation of this class and uses it to provide multiplayer capabilities (i.e. RPCs) across the whole scene.

It is possible to override the MultiplayerAPI instance used by specific tree branches by calling the [`SceneTree.SetMultiplayer`](SceneTree.md) method, effectively allowing to run both client and server in the same scene.

It is also possible to extend or replace the default implementation via scripting or native extensions. See [`MultiplayerApiExtension`](MultiplayerApiExtension.md) for details about extensions, [`SceneMultiplayer`](SceneMultiplayer.md) for the details about the default implementation.

## 属性

### MultiplayerPeer

```csharp
public MultiplayerPeer MultiplayerPeer { get; set; }
```

The peer object to handle the RPC system (effectively enabling networking when set). Depending on the peer itself, the MultiplayerAPI will become a network server (check with `IsServer()`) and will set root node's network mode to authority, or it will become a regular client peer. All child nodes are set to inherit the network mode by default. Handling of networking-related events (connection, disconnection, new clients) is done by connecting to MultiplayerAPI's signals.

## 方法

### CreateDefaultInterface

```csharp
public static MultiplayerApi CreateDefaultInterface()
```

Returns a new instance of the default MultiplayerAPI.

### GetDefaultInterface

```csharp
public static StringName GetDefaultInterface()
```

Returns the default MultiplayerAPI implementation class name. This is usually `"SceneMultiplayer"` when [`SceneMultiplayer`](SceneMultiplayer.md) is available. See `SetDefaultInterface()`.

### GetMultiplayerPeer

```csharp
public MultiplayerPeer GetMultiplayerPeer()
```

### GetPeers

```csharp
public int[] GetPeers()
```

Returns the peer IDs of all connected peers of this MultiplayerAPI's `MultiplayerPeer`.

### GetRemoteSenderId

```csharp
public int GetRemoteSenderId()
```

Returns the sender's peer ID for the RPC currently being executed.

**Note:** This method returns `0` when called outside of an RPC. As such, the original peer ID may be lost when code execution is delayed (such as with GDScript's `await` keyword).

### GetUniqueId

```csharp
public int GetUniqueId()
```

Returns the unique peer ID of this MultiplayerAPI's `MultiplayerPeer`.

### HasMultiplayerPeer

```csharp
public bool HasMultiplayerPeer()
```

Returns `true` if there is a `MultiplayerPeer` set.

### IsServer

```csharp
public bool IsServer()
```

Returns `true` if this MultiplayerAPI's `MultiplayerPeer` is valid and in server mode (listening for connections).

### ObjectConfigurationAdd

```csharp
public Error ObjectConfigurationAdd(GodotObject object, Variant configuration)
```

Notifies the MultiplayerAPI of a new `configuration` for the given `object`. This method is used internally by [`SceneTree`](SceneTree.md) to configure the root path for this MultiplayerAPI (passing `null` and a valid [`NodePath`](NodePath.md) as `configuration`). This method can be further used by MultiplayerAPI implementations to provide additional features, refer to specific implementation (e.g. [`SceneMultiplayer`](SceneMultiplayer.md)) for details on how they use it.

**Note:** This method is mostly relevant when extending or overriding the MultiplayerAPI behavior via [`MultiplayerApiExtension`](MultiplayerApiExtension.md).

### ObjectConfigurationRemove

```csharp
public Error ObjectConfigurationRemove(GodotObject object, Variant configuration)
```

Notifies the MultiplayerAPI to remove a `configuration` for the given `object`. This method is used internally by [`SceneTree`](SceneTree.md) to configure the root path for this MultiplayerAPI (passing `null` and an empty [`NodePath`](NodePath.md) as `configuration`). This method can be further used by MultiplayerAPI implementations to provide additional features, refer to specific implementation (e.g. [`SceneMultiplayer`](SceneMultiplayer.md)) for details on how they use it.

**Note:** This method is mostly relevant when extending or overriding the MultiplayerAPI behavior via [`MultiplayerApiExtension`](MultiplayerApiExtension.md).

### Poll

```csharp
public Error Poll()
```

Method used for polling the MultiplayerAPI. You only need to worry about this if you set [`SceneTree.MultiplayerPoll`](SceneTree.md) to `false`. By default, [`SceneTree`](SceneTree.md) will poll its MultiplayerAPI(s) for you.

**Note:** This method results in RPCs being called, so they will be executed in the same context of this function (e.g. `_process`, `physics`, [`GodotThread`](GodotThread.md)).

### Rpc

```csharp
public Error Rpc(int peer, GodotObject object, StringName method, Array arguments = null)
```

Sends an RPC to the target `peer`. The given `method` will be called on the remote `object` with the provided `arguments`. The RPC may also be called locally depending on the implementation and RPC configuration. See [`Node.Rpc`](Node.md) and [`Node.RpcConfig`](Node.md).

**Note:** Prefer using [`Node.Rpc`](Node.md), [`Node.RpcId`](Node.md), or `my_method.rpc(peer, arg1, arg2, ...)` (in GDScript), since they are faster. This method is mostly useful in conjunction with [`MultiplayerApiExtension`](MultiplayerApiExtension.md) when extending or replacing the multiplayer capabilities.

### SetDefaultInterface

```csharp
public static void SetDefaultInterface(StringName interfaceName)
```

Sets the default MultiplayerAPI implementation class. This method can be used by modules and extensions to configure which implementation will be used by [`SceneTree`](SceneTree.md) when the engine starts.

### SetMultiplayerPeer

```csharp
public void SetMultiplayerPeer(MultiplayerPeer peer)
```

## 事件（信号）

### ConnectedToServer

```csharp
public event Action ConnectedToServer
```

Emitted when this MultiplayerAPI's `MultiplayerPeer` successfully connected to a server. Only emitted on clients.

### ConnectionFailed

```csharp
public event Action ConnectionFailed
```

Emitted when this MultiplayerAPI's `MultiplayerPeer` fails to establish a connection to a server. Only emitted on clients.

### PeerConnected

```csharp
public event MultiplayerApi.PeerConnectedEventHandler PeerConnected
```

Emitted when this MultiplayerAPI's `MultiplayerPeer` connects with a new peer. ID is the peer ID of the new peer. Clients get notified when other clients connect to the same server. Upon connecting to a server, a client also receives this signal for the server (with ID being 1).

### PeerDisconnected

```csharp
public event MultiplayerApi.PeerDisconnectedEventHandler PeerDisconnected
```

Emitted when this MultiplayerAPI's `MultiplayerPeer` disconnects from a peer. Clients get notified when other clients disconnect from the same server.

### ServerDisconnected

```csharp
public event Action ServerDisconnected
```

Emitted when this MultiplayerAPI's `MultiplayerPeer` disconnects from server. Only emitted on clients.

## PeerConnectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`MultiplayerApi.PeerConnected`](MultiplayerApi.md) event of a [`MultiplayerApi`](MultiplayerApi.md) class.

### 签名

#### Invoke

```csharp
delegate void PeerConnectedEventHandler(long id)
```

## PeerDisconnectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`MultiplayerApi.PeerDisconnected`](MultiplayerApi.md) event of a [`MultiplayerApi`](MultiplayerApi.md) class.

### 签名

#### Invoke

```csharp
delegate void PeerDisconnectedEventHandler(long id)
```

## RpcMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Used with [`Node.RpcConfig`](Node.md) to disable a method or property for all RPC calls, making it unavailable.
- `AnyPeer = 1`：Used with [`Node.RpcConfig`](Node.md) to set a method to be callable remotely by any peer.
- `Authority = 2`：Used with [`Node.RpcConfig`](Node.md) to set a method to be callable remotely only by the current multiplayer authority (which is the server by default).
