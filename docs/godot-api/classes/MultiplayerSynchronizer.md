<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MultiplayerSynchronizer

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

By default, `MultiplayerSynchronizer` synchronizes configured properties to all peers.

Visibility can be handled directly with `SetVisibilityFor()` or as-needed with `AddVisibilityFilter()` and `UpdateVisibility()`.

[`MultiplayerSpawner`](MultiplayerSpawner.md)s will handle nodes according to visibility of synchronizers as long as the node at `RootPath` was spawned by one.

Internally, `MultiplayerSynchronizer` uses [`MultiplayerApi.ObjectConfigurationAdd`](MultiplayerApi.md) to notify synchronization start passing the [`Node`](Node.md) at `RootPath` as the `object` and itself as the `configuration`, and uses [`MultiplayerApi.ObjectConfigurationRemove`](MultiplayerApi.md) to notify synchronization end in a similar way.

**Note:** Synchronization is not supported for [`GodotObject`](GodotObject.md) type properties, like [`Resource`](Resource.md). Properties that are unique to each peer, like the instance IDs of [`GodotObject`](GodotObject.md)s (see [`GodotObject.GetInstanceId`](GodotObject.md)) or [`Rid`](Rid.md)s, will also not work in synchronization.

## 构造函数

### MultiplayerSynchronizer

```csharp
public MultiplayerSynchronizer()
```

## 属性

### DeltaInterval

```csharp
public double DeltaInterval { get; set; }
```

引擎名：`delta_interval`

Time interval between delta synchronizations. Used when the replication is set to [`SceneReplicationConfig.ReplicationMode.OnChange`](SceneReplicationConfig.md). If set to `0.0` (the default), delta synchronizations happen every network process frame.

### PublicVisibility

```csharp
public bool PublicVisibility { get; set; }
```

引擎名：`public_visibility`

Whether synchronization should be visible to all peers by default. See `SetVisibilityFor()` and `AddVisibilityFilter()` for ways of configuring fine-grained visibility options.

### ReplicationConfig

```csharp
public SceneReplicationConfig ReplicationConfig { get; set; }
```

引擎名：`replication_config`

Resource containing which properties to synchronize.

### ReplicationInterval

```csharp
public double ReplicationInterval { get; set; }
```

引擎名：`replication_interval`

Time interval between synchronizations. Used when the replication is set to [`SceneReplicationConfig.ReplicationMode.Always`](SceneReplicationConfig.md). If set to `0.0` (the default), synchronizations happen every network process frame.

### RootPath

```csharp
public NodePath RootPath { get; set; }
```

引擎名：`root_path`

Node path that replicated properties are relative to.

If `RootPath` was spawned by a [`MultiplayerSpawner`](MultiplayerSpawner.md), the node will be also be spawned and despawned based on this synchronizer visibility options.

### VisibilityUpdateMode

```csharp
public MultiplayerSynchronizer.VisibilityUpdateModeEnum VisibilityUpdateMode { get; set; }
```

引擎名：`visibility_update_mode`

Specifies when visibility filters are updated.

## 方法

### AddVisibilityFilter

```csharp
public void AddVisibilityFilter(Callable filter)
```

引擎名：`add_visibility_filter`

Adds a peer visibility filter for this synchronizer.

`filter` should take a peer ID `System.Int32` and return a `System.Boolean`.

### GetDeltaInterval

```csharp
public double GetDeltaInterval()
```

引擎名：`get_delta_interval`

### GetReplicationConfig

```csharp
public SceneReplicationConfig GetReplicationConfig()
```

引擎名：`get_replication_config`

### GetReplicationInterval

```csharp
public double GetReplicationInterval()
```

引擎名：`get_replication_interval`

### GetRootPath

```csharp
public NodePath GetRootPath()
```

引擎名：`get_root_path`

### GetVisibilityFor

```csharp
public bool GetVisibilityFor(int peer)
```

引擎名：`get_visibility_for`

Queries the current visibility for peer `peer`.

### GetVisibilityUpdateMode

```csharp
public MultiplayerSynchronizer.VisibilityUpdateModeEnum GetVisibilityUpdateMode()
```

引擎名：`get_visibility_update_mode`

### IsVisibilityPublic

```csharp
public bool IsVisibilityPublic()
```

引擎名：`is_visibility_public`

### RemoveVisibilityFilter

```csharp
public void RemoveVisibilityFilter(Callable filter)
```

引擎名：`remove_visibility_filter`

Removes a peer visibility filter from this synchronizer.

### SetDeltaInterval

```csharp
public void SetDeltaInterval(double milliseconds)
```

引擎名：`set_delta_interval`

### SetReplicationConfig

```csharp
public void SetReplicationConfig(SceneReplicationConfig config)
```

引擎名：`set_replication_config`

### SetReplicationInterval

```csharp
public void SetReplicationInterval(double milliseconds)
```

引擎名：`set_replication_interval`

### SetRootPath

```csharp
public void SetRootPath(NodePath path)
```

引擎名：`set_root_path`

### SetVisibilityFor

```csharp
public void SetVisibilityFor(int peer, bool visible)
```

引擎名：`set_visibility_for`

Sets the visibility of `peer` to `visible`. If `peer` is `0`, the value of `PublicVisibility` will be updated instead.

### SetVisibilityPublic

```csharp
public void SetVisibilityPublic(bool visible)
```

引擎名：`set_visibility_public`

### SetVisibilityUpdateMode

```csharp
public void SetVisibilityUpdateMode(MultiplayerSynchronizer.VisibilityUpdateModeEnum mode)
```

引擎名：`set_visibility_update_mode`

### UpdateVisibility

```csharp
public void UpdateVisibility(int forPeer = 0)
```

引擎名：`update_visibility`

Updates the visibility of `forPeer` according to visibility filters. If `forPeer` is `0` (the default), all peers' visibilties are updated.

## 事件（信号）

### DeltaSynchronized

```csharp
public event Action DeltaSynchronized
```

引擎名：`delta_synchronized`

Emitted when a new delta synchronization state is received by this synchronizer after the properties have been updated.

### Synchronized

```csharp
public event Action Synchronized
```

引擎名：`synchronized`

Emitted when a new synchronization state is received by this synchronizer after the properties have been updated.

### VisibilityChanged

```csharp
public event MultiplayerSynchronizer.VisibilityChangedEventHandler VisibilityChanged
```

引擎名：`visibility_changed`

Emitted when visibility of `forPeer` is updated. See `UpdateVisibility()`.

## VisibilityChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`MultiplayerSynchronizer.VisibilityChanged`](MultiplayerSynchronizer.md) event of a [`MultiplayerSynchronizer`](MultiplayerSynchronizer.md) class.

### 签名

#### Invoke

```csharp
delegate void VisibilityChangedEventHandler(long forPeer)
```

## VisibilityUpdateModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Idle = 0`：Visibility filters are updated during process frames (see [`Node.NotificationInternalProcess`](Node.md)).
- `Physics = 1`：Visibility filters are updated during physics frames (see [`Node.NotificationInternalPhysicsProcess`](Node.md)).
- `None = 2`：Visibility filters are not updated automatically, and must be updated manually by calling [`MultiplayerSynchronizer.UpdateVisibility`](MultiplayerSynchronizer.md).
