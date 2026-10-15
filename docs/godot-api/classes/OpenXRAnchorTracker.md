<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRAnchorTracker

命名空间 `Godot`；class；继承 [`OpenXRSpatialEntityTracker`](OpenXRSpatialEntityTracker.md) → [`XRPositionalTracker`](XRPositionalTracker.md) → [`XRTracker`](XRTracker.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Positional tracker for our OpenXR spatial entity anchor extension, it tracks a user defined location in real space and maps it to our virtual space.

## 构造函数

### OpenXRAnchorTracker

```csharp
public OpenXRAnchorTracker()
```

## 属性

### Uuid

```csharp
public string Uuid { get; set; }
```

引擎名：`uuid`

The UUID provided for persistent anchors.

## 方法

### GetUuid

```csharp
public string GetUuid()
```

引擎名：`get_uuid`

### HasUuid

```csharp
public bool HasUuid()
```

引擎名：`has_uuid`

Returns `true` if a non-zero UUID is set.

### SetUuid

```csharp
public void SetUuid(string uuid)
```

引擎名：`set_uuid`

## 事件（信号）

### UuidChanged

```csharp
public event Action UuidChanged
```

引擎名：`uuid_changed`

Emitted when the UUID for this anchor was changed.
