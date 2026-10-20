<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MultiplayerApiExtension

命名空间 `Godot`；class；继承 [`MultiplayerApi`](MultiplayerApi.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class can be used to extend or replace the default [`MultiplayerApi`](MultiplayerApi.md) implementation via script or extensions.

The following example extend the default implementation ([`SceneMultiplayer`](SceneMultiplayer.md)) by logging every RPC being made, and every object being configured for replication.

Then in your main scene or in an autoload call [`SceneTree.SetMultiplayer`](SceneTree.md) to start using your custom [`MultiplayerApi`](MultiplayerApi.md):

Native extensions can alternatively use the [`MultiplayerApi.SetDefaultInterface`](MultiplayerApi.md) method during initialization to configure themselves as the default implementation.

## 构造函数

### MultiplayerApiExtension

```csharp
public MultiplayerApiExtension()
```

## 方法

### _GetMultiplayerPeer

```csharp
public virtual MultiplayerPeer _GetMultiplayerPeer()
```

Called when the [`MultiplayerApi.MultiplayerPeer`](MultiplayerApi.md) is retrieved.

### _GetPeerIds

```csharp
public virtual int[] _GetPeerIds()
```

Callback for [`MultiplayerApi.GetPeers`](MultiplayerApi.md).

### _GetRemoteSenderId

```csharp
public virtual int _GetRemoteSenderId()
```

Callback for [`MultiplayerApi.GetRemoteSenderId`](MultiplayerApi.md).

### _GetUniqueId

```csharp
public virtual int _GetUniqueId()
```

Callback for [`MultiplayerApi.GetUniqueId`](MultiplayerApi.md).

### _ObjectConfigurationAdd

```csharp
public virtual Error _ObjectConfigurationAdd(GodotObject object, Variant configuration)
```

Callback for [`MultiplayerApi.ObjectConfigurationAdd`](MultiplayerApi.md).

### _ObjectConfigurationRemove

```csharp
public virtual Error _ObjectConfigurationRemove(GodotObject object, Variant configuration)
```

Callback for [`MultiplayerApi.ObjectConfigurationRemove`](MultiplayerApi.md).

### _Poll

```csharp
public virtual Error _Poll()
```

Callback for [`MultiplayerApi.Poll`](MultiplayerApi.md).

### _Rpc

```csharp
public virtual Error _Rpc(int peer, GodotObject object, StringName method, Array args)
```

Callback for [`MultiplayerApi.Rpc`](MultiplayerApi.md).

### _SetMultiplayerPeer

```csharp
public virtual void _SetMultiplayerPeer(MultiplayerPeer multiplayerPeer)
```

Called when the [`MultiplayerApi.MultiplayerPeer`](MultiplayerApi.md) is set.
