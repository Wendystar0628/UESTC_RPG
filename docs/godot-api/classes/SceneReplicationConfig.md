<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SceneReplicationConfig

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

## 构造函数

### SceneReplicationConfig

```csharp
public SceneReplicationConfig()
```

## 方法

### AddProperty

```csharp
public void AddProperty(NodePath path, int index = -1)
```

引擎名：`add_property`

Adds the property identified by the given `path` to the list of the properties being synchronized, optionally passing an `index`.

**Note:** For details on restrictions and limitations on property synchronization, see [`MultiplayerSynchronizer`](MultiplayerSynchronizer.md).

### GetProperties

```csharp
public Array<NodePath> GetProperties()
```

引擎名：`get_properties`

Returns a list of synchronized property [`NodePath`](NodePath.md)s.

### HasProperty

```csharp
public bool HasProperty(NodePath path)
```

引擎名：`has_property`

Returns `true` if the given `path` is configured for synchronization.

### PropertyGetIndex

```csharp
public int PropertyGetIndex(NodePath path)
```

引擎名：`property_get_index`

Finds the index of the given `path`.

### PropertyGetReplicationMode

```csharp
public SceneReplicationConfig.ReplicationMode PropertyGetReplicationMode(NodePath path)
```

引擎名：`property_get_replication_mode`

Returns the replication mode for the property identified by the given `path`.

### PropertyGetSpawn

```csharp
public bool PropertyGetSpawn(NodePath path)
```

引擎名：`property_get_spawn`

Returns `true` if the property identified by the given `path` is configured to be synchronized on spawn.

### PropertyGetSync

```csharp
public bool PropertyGetSync(NodePath path)
```

引擎名：`property_get_sync`

Returns `true` if the property identified by the given `path` is configured to be synchronized on process.

### PropertyGetWatch

```csharp
public bool PropertyGetWatch(NodePath path)
```

引擎名：`property_get_watch`

Returns `true` if the property identified by the given `path` is configured to be reliably synchronized when changes are detected on process.

### PropertySetReplicationMode

```csharp
public void PropertySetReplicationMode(NodePath path, SceneReplicationConfig.ReplicationMode mode)
```

引擎名：`property_set_replication_mode`

Sets the synchronization mode for the property identified by the given `path`.

### PropertySetSpawn

```csharp
public void PropertySetSpawn(NodePath path, bool enabled)
```

引擎名：`property_set_spawn`

Sets whether the property identified by the given `path` is configured to be synchronized on spawn.

### PropertySetSync

```csharp
public void PropertySetSync(NodePath path, bool enabled)
```

引擎名：`property_set_sync`

Sets whether the property identified by the given `path` is configured to be synchronized on process.

### PropertySetWatch

```csharp
public void PropertySetWatch(NodePath path, bool enabled)
```

引擎名：`property_set_watch`

Sets whether the property identified by the given `path` is configured to be reliably synchronized when changes are detected on process.

### RemoveProperty

```csharp
public void RemoveProperty(NodePath path)
```

引擎名：`remove_property`

Removes the property identified by the given `path` from the configuration.

## ReplicationMode

命名空间 `Godot`；enum

### 枚举值

- `Never = 0`：Do not keep the given property synchronized.
- `Always = 1`：Replicate the given property on process by constantly sending updates using unreliable transfer mode.
- `OnChange = 2`：Replicate the given property on process by sending updates using reliable transfer mode when its value changes.
