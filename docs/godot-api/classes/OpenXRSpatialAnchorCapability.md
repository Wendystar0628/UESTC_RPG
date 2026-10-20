<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialAnchorCapability

命名空间 `Godot`；class；继承 [`OpenXRExtensionWrapper`](OpenXRExtensionWrapper.md) → [`GodotObject`](GodotObject.md)

This is an internal class that handles the OpenXR anchor spatial entity extension.

## 构造函数

### OpenXRSpatialAnchorCapability

```csharp
public OpenXRSpatialAnchorCapability()
```

## 方法

### CreateDefaultPersistenceContext

```csharp
public OpenXRFutureResult CreateDefaultPersistenceContext(Callable userCallback = default)
```

引擎名：`create_default_persistence_context`

Calls `CreatePersistenceContext()` with a configuration that likely works with the XR runtime.

`userCallback` is called when the context is created.

### CreateNewAnchor

```csharp
public OpenXRAnchorTracker CreateNewAnchor(Transform3D transform, Rid spatialContext = default, OpenXRStructureBase next = null)
```

引擎名：`create_new_anchor`

Creates a new anchor that will be tracked by the XR runtime. The `transform` should be a transform in the local space of your [`XROrigin3D`](XROrigin3D.md) node. If `spatialContext` is not specified the default will be used, this requires `ProjectSettings.xr/openxr/extensions/spatial_entity/enable_builtin_anchor_detection` to be set. The returned tracker will track the location in case our reference space changes.

`next` must be a valid next object for the `XrSpatialAnchorCreateInfoEXT` chain.

### CreateNewAnchor

```csharp
public OpenXRAnchorTracker CreateNewAnchor(Transform3D transform, Rid spatialContext)
```

引擎名：`create_new_anchor`

Creates a new anchor that will be tracked by the XR runtime. The `transform` should be a transform in the local space of your [`XROrigin3D`](XROrigin3D.md) node. If `spatialContext` is not specified the default will be used, this requires `ProjectSettings.xr/openxr/extensions/spatial_entity/enable_builtin_anchor_detection` to be set. The returned tracker will track the location in case our reference space changes.

`next` must be a valid next object for the `XrSpatialAnchorCreateInfoEXT` chain.

### CreatePersistenceContext

```csharp
public OpenXRFutureResult CreatePersistenceContext(OpenXRSpatialAnchorCapability.PersistenceScope scope, Callable userCallback = default)
```

引擎名：`create_persistence_context`

Creates a new persistence context for storing persistent data.

**Note:** This is an asynchronous method and returns an [`OpenXRFutureResult`](OpenXRFutureResult.md) object with which to track the status, discarding this object will not cancel the creation process. On success `userCallback` will be called if specified. The result value for this function is the [`Rid`](Rid.md) for our persistence context.

### DoEntityUpdate

```csharp
public void DoEntityUpdate(Rid spatialContext, Array<OpenXRSpatialComponentData> componentData, OpenXRStructureBase nextSnapshotCreate = null, OpenXRStructureBase nextSnapshotQuery = null)
```

引擎名：`do_entity_update`

Calls [`OpenXRSpatialEntityExtension.UpdateSpatialEntities`](OpenXRSpatialEntityExtension.md) and [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md) with the anchor entities associated with `spatialContext`.

`componentData` are the [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md)s to update for this anchor capability.

If `nextSnapshotCreate` is non-null, then pass this to the `next` parameter in [`OpenXRSpatialEntityExtension.UpdateSpatialEntities`](OpenXRSpatialEntityExtension.md).

If `nextSnapshotQuery` is non-null, then pass this to the `next` parameter in [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

### FreePersistenceContext

```csharp
public void FreePersistenceContext(Rid persistenceContext)
```

引擎名：`free_persistence_context`

Frees a persistence context previously created with `CreatePersistenceContext()`.

### GetPersistenceContextHandle

```csharp
public ulong GetPersistenceContextHandle(Rid persistenceContext)
```

引擎名：`get_persistence_context_handle`

Returns the internal handle for this persistence context.

**Note:** For GDExtension implementations.

### IsPersistenceScopeSupported

```csharp
public bool IsPersistenceScopeSupported(OpenXRSpatialAnchorCapability.PersistenceScope scope)
```

引擎名：`is_persistence_scope_supported`

Returns `true` if this persistence scope is supported by our spatial anchor capability.

**Note:** Only valid after an OpenXR instance has been created.

### IsSpatialAnchorSupported

```csharp
public bool IsSpatialAnchorSupported()
```

引擎名：`is_spatial_anchor_supported`

Returns `true` if spatial anchors are supported by the hardware. Only returns a valid value after OpenXR has been initialized.

### IsSpatialPersistenceSupported

```csharp
public bool IsSpatialPersistenceSupported()
```

引擎名：`is_spatial_persistence_supported`

Returns `true` if persistent spatial anchors are supported by the hardware. Only returns a valid value after OpenXR has been initialized.

### PersistAnchor

```csharp
public OpenXRFutureResult PersistAnchor(OpenXRAnchorTracker anchorTracker, Rid persistenceContext = default, Callable userCallback = default)
```

引擎名：`persist_anchor`

Changes this anchor into a persistent anchor. This means its location will be stored on the device and the anchor will be restored the next time your application starts. If `persistenceContext` is not specified the default will be used, this requires `ProjectSettings.xr/openxr/extensions/spatial_entity/enable_builtin_anchor_detection` to be set.

**Note:** This is an asynchronous method and returns an [`OpenXRFutureResult`](OpenXRFutureResult.md) object with which to track the status, discarding this object will not cancel the creation process. On success `userCallback` will be called if specified. The result value for this function is a boolean which will be set to `true` on successful completion.

### RemoveAnchor

```csharp
public void RemoveAnchor(OpenXRAnchorTracker anchorTracker)
```

引擎名：`remove_anchor`

Remove an anchor previously created with `CreateNewAnchor()`. If this anchor was persistent you must first call `UnpersistAnchor()` and await its callback.

### StartEntityDiscovery

```csharp
public OpenXRFutureResult StartEntityDiscovery(Rid spatialContext, Array<OpenXRSpatialComponentData> componentData, OpenXRStructureBase nextSnapshotCreate = null, OpenXRStructureBase nextSnapshotQuery = null, Callable userCallback = default)
```

引擎名：`start_entity_discovery`

Calls [`OpenXRSpatialEntityExtension.DiscoverSpatialEntities`](OpenXRSpatialEntityExtension.md) and [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md) with the anchor entities associated with `spatialContext`.

`componentData` are the [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md)s to discover for this anchor capability.

If `nextSnapshotCreate` is non-null, then pass this to the `next` parameter in [`OpenXRSpatialEntityExtension.DiscoverSpatialEntities`](OpenXRSpatialEntityExtension.md).

If `nextSnapshotQuery` is non-null, then pass this to the `next` parameter in [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

`userCallback`, when non-null, is called with two parameters usually twice. The first parameter is the [`Rid`](Rid.md) of the discovery snapshot and the second parameter is a boolean where `false` indicates the discovery snapshot is about to be processed, and `true` indicates the discovery snapshot has been processed and `componentData` has valid data. The second call is skipped if an error was encountered.

The returned [`OpenXRFutureResult`](OpenXRFutureResult.md) is identical to the return from [`OpenXRSpatialEntityExtension.DiscoverSpatialEntities`](OpenXRSpatialEntityExtension.md).

### UnpersistAnchor

```csharp
public OpenXRFutureResult UnpersistAnchor(OpenXRAnchorTracker anchorTracker, Rid persistenceContext = default, Callable userCallback = default)
```

引擎名：`unpersist_anchor`

Removes the persistent data from this anchor. The runtime will not recreate the anchor when your application restarts. If `persistenceContext` is not specified the default will be used, this requires `ProjectSettings.xr/openxr/extensions/spatial_entity/enabled` to be set.

**Note:** This is an asynchronous method and returns an [`OpenXRFutureResult`](OpenXRFutureResult.md) object with which to track the status, discarding this object will not cancel the creation process. On success `userCallback` will be called if specified. The result value for this function is a boolean which will be set to `true` on successful completion.

## PersistenceScope

命名空间 `Godot`；enum

### 枚举值

- `SystemManaged = 1`：Provides the application with read-only access (i.e.
- `LocalAnchors = 1000781000`：Persistence operations and data access is limited to spatial anchors, on the same device, for the same user and same app (using [`OpenXRSpatialAnchorCapability.PersistAnchor`](OpenXRSpatialAnchorCapability.md) and [`Open…
