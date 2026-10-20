<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialQueryResultData

命名空间 `Godot`；class；继承 [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Object for storing the main query result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md). This must always be the first component requested.

## 构造函数

### OpenXRSpatialQueryResultData

```csharp
public OpenXRSpatialQueryResultData()
```

## 方法

### GetCapacity

```csharp
public long GetCapacity()
```

引擎名：`get_capacity`

Returns the number of entities that were retrieved.

### GetEntityId

```csharp
public ulong GetEntityId(long index)
```

引擎名：`get_entity_id`

Returns the entity id (`XrSpatialEntityIdEXT`) for the entity at this `index`.

### GetEntityState

```csharp
public OpenXRSpatialEntityTracker.EntityTrackingState GetEntityState(long index)
```

引擎名：`get_entity_state`

Returns the entity state for the entity at this `index`.
