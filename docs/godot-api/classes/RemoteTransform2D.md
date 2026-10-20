<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RemoteTransform2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

RemoteTransform2D pushes its own [`Transform2D`](Transform2D.md) to another [`Node2D`](Node2D.md) derived node (called the remote node) in the scene.

It can be set to update another node's position, rotation and/or scale. It can use either global or local coordinates.

## 构造函数

### RemoteTransform2D

```csharp
public RemoteTransform2D()
```

## 属性

### RemotePath

```csharp
public NodePath RemotePath { get; set; }
```

引擎名：`remote_path`

The [`NodePath`](NodePath.md) to the remote node, relative to the RemoteTransform2D's position in the scene.

### UpdatePosition

```csharp
public bool UpdatePosition { get; set; }
```

引擎名：`update_position`

If `true`, the remote node's position is updated.

### UpdateRotation

```csharp
public bool UpdateRotation { get; set; }
```

引擎名：`update_rotation`

If `true`, the remote node's rotation is updated.

### UpdateScale

```csharp
public bool UpdateScale { get; set; }
```

引擎名：`update_scale`

If `true`, the remote node's scale is updated.

### UseGlobalCoordinates

```csharp
public bool UseGlobalCoordinates { get; set; }
```

引擎名：`use_global_coordinates`

If `true`, global coordinates are used. If `false`, local coordinates are used.

## 方法

### ForceUpdateCache

```csharp
public void ForceUpdateCache()
```

引擎名：`force_update_cache`

`RemoteTransform2D` caches the remote node. It may not notice if the remote node disappears; `ForceUpdateCache()` forces it to update the cache again.

### GetRemoteNode

```csharp
public NodePath GetRemoteNode()
```

引擎名：`get_remote_node`

### GetUpdatePosition

```csharp
public bool GetUpdatePosition()
```

引擎名：`get_update_position`

### GetUpdateRotation

```csharp
public bool GetUpdateRotation()
```

引擎名：`get_update_rotation`

### GetUpdateScale

```csharp
public bool GetUpdateScale()
```

引擎名：`get_update_scale`

### GetUseGlobalCoordinates

```csharp
public bool GetUseGlobalCoordinates()
```

引擎名：`get_use_global_coordinates`

### SetRemoteNode

```csharp
public void SetRemoteNode(NodePath path)
```

引擎名：`set_remote_node`

### SetUpdatePosition

```csharp
public void SetUpdatePosition(bool updateRemotePosition)
```

引擎名：`set_update_position`

### SetUpdateRotation

```csharp
public void SetUpdateRotation(bool updateRemoteRotation)
```

引擎名：`set_update_rotation`

### SetUpdateScale

```csharp
public void SetUpdateScale(bool updateRemoteScale)
```

引擎名：`set_update_scale`

### SetUseGlobalCoordinates

```csharp
public void SetUseGlobalCoordinates(bool useGlobalCoordinates)
```

引擎名：`set_use_global_coordinates`
