<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialEntityExtension

命名空间 `Godot`；class；继承 [`OpenXRExtensionWrapper`](OpenXRExtensionWrapper.md) → [`GodotObject`](GodotObject.md)

OpenXR extension that handles spatial entities and, when enabled, allows querying those spatial entities. This extension will also automatically manage [`XRTracker`](XRTracker.md) objects for static entities.

## 构造函数

### OpenXRSpatialEntityExtension

```csharp
public OpenXRSpatialEntityExtension()
```

## 方法

### AddSpatialEntity

```csharp
public Rid AddSpatialEntity(Rid spatialContext, ulong entityId, ulong entity)
```

引擎名：`add_spatial_entity`

Registers an entity that was created directly on the OpenXR runtime.

### CreateSpatialContext

```csharp
public OpenXRFutureResult CreateSpatialContext(Array<OpenXRSpatialCapabilityConfigurationBaseHeader> capabilityConfigurations, OpenXRStructureBase next = null, Callable userCallback = default)
```

引擎名：`create_spatial_context`

Creates a new spatial context that handles entities for the provided capability configurations. `capabilityConfigurations` is an array of [`OpenXRSpatialCapabilityConfigurationBaseHeader`](OpenXRSpatialCapabilityConfigurationBaseHeader.md) with the needed capability configuration data.

`next` is an optional parameter that can contain additional information for creating our spatial context.

**Note:** This is an asynchronous method and returns an [`OpenXRFutureResult`](OpenXRFutureResult.md) object with which to track the status, discarding this object will not cancel the creation process. On success `userCallback` will be called if specified. The result data for this function is the [`Rid`](Rid.md) for our spatial context.

### DiscoverSpatialEntities

```csharp
public OpenXRFutureResult DiscoverSpatialEntities(Rid spatialContext, ReadOnlySpan<long> componentTypes, OpenXRStructureBase next, Callable userCallback)
```

引擎名：`discover_spatial_entities`

Starts a new discovery query, this will gather all objects tracked by the `spatialContext` that have at least one of the component types specified in `componentTypes`.

`next` is an optional parameter that can contain additional information for executing the discovery query.

**Note:** This is an asynchronous method and returns an [`OpenXRFutureResult`](OpenXRFutureResult.md) object with which to track the status, discarding this object will not cancel the discovery process. On success `userCallback` will be called if specified. The result data for this function is the [`Rid`](Rid.md) for our snapshot.

### DiscoverSpatialEntities

```csharp
public OpenXRFutureResult DiscoverSpatialEntities(Rid spatialContext, long[] componentTypes, OpenXRStructureBase next = null, Callable userCallback = default)
```

引擎名：`discover_spatial_entities`

Starts a new discovery query, this will gather all objects tracked by the `spatialContext` that have at least one of the component types specified in `componentTypes`.

`next` is an optional parameter that can contain additional information for executing the discovery query.

**Note:** This is an asynchronous method and returns an [`OpenXRFutureResult`](OpenXRFutureResult.md) object with which to track the status, discarding this object will not cancel the discovery process. On success `userCallback` will be called if specified. The result data for this function is the [`Rid`](Rid.md) for our snapshot.

### DiscoverSpatialEntitiesWithComponentData

```csharp
public OpenXRFutureResult DiscoverSpatialEntitiesWithComponentData(Rid spatialContext, Array<OpenXRSpatialComponentData> componentData, OpenXRStructureBase next = null, Callable userCallback = default)
```

引擎名：`discover_spatial_entities_with_component_data`

Convenience method when the caller only has an [`Collections.Array`](Collections.Array.md) of [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md) and needs to discover spatial entities.

### FindSpatialEntity

```csharp
public Rid FindSpatialEntity(ulong entityId)
```

引擎名：`find_spatial_entity`

Returns the [`Rid`](Rid.md) for the specified spatial entity ID.

### FreeSpatialContext

```csharp
public void FreeSpatialContext(Rid spatialContext)
```

引擎名：`free_spatial_context`

Frees a spatial context previously created when calling `CreateSpatialContext()`. If the spatial context creation is still ongoing, the asynchronous process is cancelled.

### FreeSpatialEntity

```csharp
public void FreeSpatialEntity(Rid entity)
```

引擎名：`free_spatial_entity`

Frees an entity previously created when calling `AddSpatialEntity()` or `MakeSpatialEntity()`.

### FreeSpatialSnapshot

```csharp
public void FreeSpatialSnapshot(Rid spatialSnapshot)
```

引擎名：`free_spatial_snapshot`

Frees a spatial snapshot previously created when calling `DiscoverSpatialEntities()`. If the spatial snapshot creation is still ongoing, the asynchronous process is cancelled.

### GetFloatBuffer

```csharp
public float[] GetFloatBuffer(Rid spatialSnapshot, ulong bufferId)
```

引擎名：`get_float_buffer`

Returns a buffer with floats from a buffer that was retrieved when taking a snapshot.

### GetSpatialContextHandle

```csharp
public ulong GetSpatialContextHandle(Rid spatialContext)
```

引擎名：`get_spatial_context_handle`

Returns the OpenXR spatial context handle for this snapshot.

**Note:** This method is intended to be used from GDExtensions that implement spatial entity capability handlers.

### GetSpatialContextReady

```csharp
public bool GetSpatialContextReady(Rid spatialContext)
```

引擎名：`get_spatial_context_ready`

Returns `true` if the spatial context finished its creation and is ready to be used.

### GetSpatialEntityContext

```csharp
public Rid GetSpatialEntityContext(Rid entity)
```

引擎名：`get_spatial_entity_context`

Returns the spatial context for this entity.

### GetSpatialEntityId

```csharp
public ulong GetSpatialEntityId(Rid entity)
```

引擎名：`get_spatial_entity_id`

Returns the internal `XrSpatialEntityIdEXT` associated with the entity.

### GetSpatialSnapshotContext

```csharp
public Rid GetSpatialSnapshotContext(Rid spatialSnapshot)
```

引擎名：`get_spatial_snapshot_context`

Returns the spatial context related to this spatial snapshot.

### GetSpatialSnapshotHandle

```csharp
public ulong GetSpatialSnapshotHandle(Rid spatialSnapshot)
```

引擎名：`get_spatial_snapshot_handle`

Returns the OpenXR spatial snapshot handle for this snapshot.

**Note:** This method is intended to be used from GDExtensions that implement spatial entity capability handlers.

### GetString

```csharp
public string GetString(Rid spatialSnapshot, ulong bufferId)
```

引擎名：`get_string`

Returns a string from a buffer that was retrieved when taking a snapshot.

### GetUint16Buffer

```csharp
public int[] GetUint16Buffer(Rid spatialSnapshot, ulong bufferId)
```

引擎名：`get_uint16_buffer`

Returns a buffer with 16 bit ints from a buffer that was retrieved when taking a snapshot.

### GetUint32Buffer

```csharp
public int[] GetUint32Buffer(Rid spatialSnapshot, ulong bufferId)
```

引擎名：`get_uint32_buffer`

Returns a buffer with 32 bit ints from a buffer that was retrieved when taking a snapshot.

### GetUint8Buffer

```csharp
public byte[] GetUint8Buffer(Rid spatialSnapshot, ulong bufferId)
```

引擎名：`get_uint8_buffer`

Returns a buffer with 8 bit ints from a buffer that was retrieved when taking a snapshot.

### GetVector2Buffer

```csharp
public Vector2[] GetVector2Buffer(Rid spatialSnapshot, ulong bufferId)
```

引擎名：`get_vector2_buffer`

Returns a buffer with [`Vector2`](Vector2.md) entries from a buffer that was retrieved when taking a snapshot.

### GetVector3Buffer

```csharp
public Vector3[] GetVector3Buffer(Rid spatialSnapshot, ulong bufferId)
```

引擎名：`get_vector3_buffer`

Returns a buffer with [`Vector3`](Vector3.md) entries from a buffer that was retrieved when taking a snapshot.

### MakeSpatialEntity

```csharp
public Rid MakeSpatialEntity(Rid spatialContext, ulong entityId)
```

引擎名：`make_spatial_entity`

Creates a new entity for this `entityId`. The `spatialContext` should match the context that discovered the entity.

### QuerySnapshot

```csharp
public bool QuerySnapshot(Rid spatialSnapshot, Array<OpenXRSpatialComponentData> componentData, OpenXRStructureBase next = null)
```

引擎名：`query_snapshot`

Queries the snapshot data. This will find all entities in the snapshot that contain all requested components in `componentData`. The objects held within `componentData` will then be populated with the queried data. `componentData` must always have an object of [`OpenXRSpatialQueryResultData`](OpenXRSpatialQueryResultData.md) as the first entry.

`next` is an optional parameter that can contain additional information passed when setting our query conditions.

### SupportsCapability

```csharp
public bool SupportsCapability(OpenXRSpatialEntityExtension.Capability capability)
```

引擎名：`supports_capability`

Returns `true` if this spatial entity `capability` is supported by the hardware used.

### SupportsComponentType

```csharp
public bool SupportsComponentType(OpenXRSpatialEntityExtension.Capability capability, OpenXRSpatialEntityExtension.ComponentType componentType)
```

引擎名：`supports_component_type`

Returns `true` if this `capability` supports the `componentType`.

### UpdateSpatialEntities

```csharp
public Rid UpdateSpatialEntities(Rid spatialContext, Array<Rid> entities, ReadOnlySpan<long> componentTypes, OpenXRStructureBase next)
```

引擎名：`update_spatial_entities`

Performs a snapshot for a limited number of entities. This is NOT an asynchronous method and will return the snapshot immediately.

### UpdateSpatialEntities

```csharp
public Rid UpdateSpatialEntities(Rid spatialContext, Array<Rid> entities, long[] componentTypes, OpenXRStructureBase next = null)
```

引擎名：`update_spatial_entities`

Performs a snapshot for a limited number of entities. This is NOT an asynchronous method and will return the snapshot immediately.

## 事件（信号）

### SpatialDiscoveryRecommended

```csharp
public event OpenXRSpatialEntityExtension.SpatialDiscoveryRecommendedEventHandler SpatialDiscoveryRecommended
```

引擎名：`spatial_discovery_recommended`

Emitted when OpenXR recommends running a discovery query because entities managed by this spatial context have (likely) changed.

## Capability

命名空间 `Godot`；enum

### 枚举值

- `PlaneTracking = 1000741000`：Plane tracking capability.
- `MarkerTrackingQrCode = 1000743000`：QR code based marker tracking capability.
- `MarkerTrackingMicroQrCode = 1000743001`：Micro QR code based marker tracking capability.
- `MarkerTrackingArucoMarker = 1000743002`：Aruco marker based marker tracking capability.
- `MarkerTrackingAprilTag = 1000743003`：April tag based marker tracking capability.
- `Anchor = 1000762000`：Anchor capability.

## ComponentType

命名空间 `Godot`；enum

### 枚举值

- `Bounded2D = 1`：Component that provides the 2D bounds for a spatial entity.
- `Bounded3D = 2`：Component that provides the 3D bounds for a spatial entity.
- `Parent = 3`：Component that provides the XrSpatialEntityIdEXT of the parent for a spatial entity.
- `Mesh3D = 4`：Component that provides a 3D mesh for a spatial entity.
- `PlaneAlignment = 1000741000`：Component that provides the plane alignment enum for a spatial entity.
- `Mesh2D = 1000741001`：Component that provides a 2D mesh for a spatial entity.
- `Polygon2D = 1000741002`：Component that provides a 2D boundary polygon for a spatial entity.
- `PlaneSemanticLabel = 1000741003`：Component that provides a semantic label for a plane.
- `Marker = 1000743000`：A component describing the marker type, ID and location.
- `Anchor = 1000762000`：Component that provides the location for an anchor.
- `Persistence = 1000763000`：Component that provides the persisted UUID for a spatial entity.

## SpatialDiscoveryRecommendedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OpenXRSpatialEntityExtension.SpatialDiscoveryRecommended`](OpenXRSpatialEntityExtension.md) event of a [`OpenXRSpatialEntityExtension`](OpenXRSpatialEntityExtension.md) class.

### 签名

#### Invoke

```csharp
delegate void SpatialDiscoveryRecommendedEventHandler(Rid spatialContext)
```
