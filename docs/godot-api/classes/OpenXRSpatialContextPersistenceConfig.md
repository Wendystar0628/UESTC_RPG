<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialContextPersistenceConfig

命名空间 `Godot`；class；继承 [`OpenXRStructureBase`](OpenXRStructureBase.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Configuration header for spatial persistence. Pass this to [`OpenXRSpatialEntityExtension.CreateSpatialContext`](OpenXRSpatialEntityExtension.md) as the next parameter to create a spatial context with spatial persistence capabilities.

## 构造函数

### OpenXRSpatialContextPersistenceConfig

```csharp
public OpenXRSpatialContextPersistenceConfig()
```

## 方法

### AddPersistenceContext

```csharp
public void AddPersistenceContext(Rid persistenceContext)
```

引擎名：`add_persistence_context`

Adds a persistence context to this configuration. You must add at least one persistence context to create a valid configuration. You can create a persistence context by calling [`OpenXRSpatialAnchorCapability.CreatePersistenceContext`](OpenXRSpatialAnchorCapability.md).

### GetPersistenceContexts

```csharp
public Array GetPersistenceContexts()
```

引擎名：`get_persistence_contexts`

Gets the persistence context(s) (as [`Rid`](Rid.md)s) received by `AddPersistenceContext()`.

### RemovePersistenceContext

```csharp
public void RemovePersistenceContext(Rid persistenceContext)
```

引擎名：`remove_persistence_context`

Removes a persistence context.
