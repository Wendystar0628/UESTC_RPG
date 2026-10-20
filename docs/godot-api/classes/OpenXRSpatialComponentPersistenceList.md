<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialComponentPersistenceList

命名空间 `Godot`；class；继承 [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Object for storing the query persistence result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

## 构造函数

### OpenXRSpatialComponentPersistenceList

```csharp
public OpenXRSpatialComponentPersistenceList()
```

## 方法

### GetPersistentState

```csharp
public ulong GetPersistentState(long index)
```

引擎名：`get_persistent_state`

Returns the persistent state (`XrSpatialPersistenceStateEXT`) for the entity at this `index`.

### GetPersistentUuid

```csharp
public string GetPersistentUuid(long index)
```

引擎名：`get_persistent_uuid`

Returns the persistent uuid for the entity at this `index`.
