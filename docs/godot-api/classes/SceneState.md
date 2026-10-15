<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SceneState

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Maintains a list of resources, nodes, exported and overridden properties, and built-in scripts associated with a scene. They cannot be modified from a `SceneState`, only accessed. Useful for peeking into what a [`PackedScene`](PackedScene.md) contains without instantiating it.

This class cannot be instantiated directly, it is retrieved for a given scene as the result of [`PackedScene.GetState`](PackedScene.md).

## 方法

### GetBaseSceneState

```csharp
public SceneState GetBaseSceneState()
```

引擎名：`get_base_scene_state`

Returns the `SceneState` of the scene that this scene inherits from, or `null` if it doesn't inherit from any scene.

### GetConnectionBinds

```csharp
public Array GetConnectionBinds(int idx)
```

引擎名：`get_connection_binds`

Returns the list of bound parameters for the signal at `idx`.

### GetConnectionCount

```csharp
public int GetConnectionCount()
```

引擎名：`get_connection_count`

Returns the number of signal connections in the scene.

The `idx` argument used to query connection metadata in other `get_connection_*` methods in the interval `[0, get_connection_count() - 1]`.

### GetConnectionFlags

```csharp
public int GetConnectionFlags(int idx)
```

引擎名：`get_connection_flags`

Returns the connection flags for the signal at `idx`. See [`GodotObject.ConnectFlags`](GodotObject.md) constants.

### GetConnectionMethod

```csharp
public StringName GetConnectionMethod(int idx)
```

引擎名：`get_connection_method`

Returns the method connected to the signal at `idx`.

### GetConnectionSignal

```csharp
public StringName GetConnectionSignal(int idx)
```

引擎名：`get_connection_signal`

Returns the name of the signal at `idx`.

### GetConnectionSource

```csharp
public NodePath GetConnectionSource(int idx)
```

引擎名：`get_connection_source`

Returns the path to the node that owns the signal at `idx`, relative to the root node.

### GetConnectionTarget

```csharp
public NodePath GetConnectionTarget(int idx)
```

引擎名：`get_connection_target`

Returns the path to the node that owns the method connected to the signal at `idx`, relative to the root node.

### GetConnectionUnbinds

```csharp
public int GetConnectionUnbinds(int idx)
```

引擎名：`get_connection_unbinds`

Returns the number of unbound parameters for the signal at `idx`.

### GetNodeCount

```csharp
public int GetNodeCount()
```

引擎名：`get_node_count`

Returns the number of nodes in the scene.

The `idx` argument used to query node data in other `get_node_*` methods in the interval `[0, get_node_count() - 1]`.

### GetNodeGroups

```csharp
public string[] GetNodeGroups(int idx)
```

引擎名：`get_node_groups`

Returns the list of group names associated with the node at `idx`.

### GetNodeIndex

```csharp
public int GetNodeIndex(int idx)
```

引擎名：`get_node_index`

Returns the node's index, which is its position relative to its siblings. This is only relevant and saved in scenes for cases where new nodes are added to an instantiated or inherited scene among siblings from the base scene. Despite the name, this index is not related to the `idx` argument used here and in other methods.

### GetNodeInstance

```csharp
public PackedScene GetNodeInstance(int idx)
```

引擎名：`get_node_instance`

Returns a [`PackedScene`](PackedScene.md) for the node at `idx` (i.e. the whole branch starting at this node, with its child nodes and resources), or `null` if the node is not an instance.

### GetNodeInstancePlaceholder

```csharp
public string GetNodeInstancePlaceholder(int idx)
```

引擎名：`get_node_instance_placeholder`

Returns the path to the represented scene file if the node at `idx` is an [`InstancePlaceholder`](InstancePlaceholder.md).

### GetNodeName

```csharp
public StringName GetNodeName(int idx)
```

引擎名：`get_node_name`

Returns the name of the node at `idx`.

### GetNodeOwnerPath

```csharp
public NodePath GetNodeOwnerPath(int idx)
```

引擎名：`get_node_owner_path`

Returns the path to the owner of the node at `idx`, relative to the root node.

### GetNodePath

```csharp
public NodePath GetNodePath(int idx, bool forParent = false)
```

引擎名：`get_node_path`

Returns the path to the node at `idx`.

If `forParent` is `true`, returns the path of the `idx` node's parent instead.

### GetNodePropertyCount

```csharp
public int GetNodePropertyCount(int idx)
```

引擎名：`get_node_property_count`

Returns the number of exported or overridden properties for the node at `idx`.

The `prop_idx` argument used to query node property data in other `get_node_property_*` methods in the interval `[0, get_node_property_count() - 1]`.

### GetNodePropertyName

```csharp
public StringName GetNodePropertyName(int idx, int propIdx)
```

引擎名：`get_node_property_name`

Returns the name of the property at `propIdx` for the node at `idx`.

### GetNodePropertyValue

```csharp
public Variant GetNodePropertyValue(int idx, int propIdx)
```

引擎名：`get_node_property_value`

Returns the value of the property at `propIdx` for the node at `idx`.

### GetNodeType

```csharp
public StringName GetNodeType(int idx)
```

引擎名：`get_node_type`

Returns the type of the node at `idx`.

### GetPath

```csharp
public string GetPath()
```

引擎名：`get_path`

Returns the resource path to the represented [`PackedScene`](PackedScene.md).

### IsNodeInstancePlaceholder

```csharp
public bool IsNodeInstancePlaceholder(int idx)
```

引擎名：`is_node_instance_placeholder`

Returns `true` if the node at `idx` is an [`InstancePlaceholder`](InstancePlaceholder.md).

## GenEditState

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：If passed to [`PackedScene.Instantiate`](PackedScene.md), blocks edits to the scene state.
- `Instance = 1`：If passed to [`PackedScene.Instantiate`](PackedScene.md), provides inherited scene resources to the local scene.
- `Main = 2`：If passed to [`PackedScene.Instantiate`](PackedScene.md), provides local scene resources to the local scene.
- `MainInherited = 3`：If passed to [`PackedScene.Instantiate`](PackedScene.md), it's similar to `Main`, but for the case where the scene is being instantiated to be the base of another one.
