<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialPlaneTrackingCapability

命名空间 `Godot`；class；继承 [`OpenXRExtensionWrapper`](OpenXRExtensionWrapper.md) → [`GodotObject`](GodotObject.md)

This class handles the OpenXR plane tracking spatial entity extension.

## 构造函数

### OpenXRSpatialPlaneTrackingCapability

```csharp
public OpenXRSpatialPlaneTrackingCapability()
```

## 方法

### IsSupported

```csharp
public bool IsSupported()
```

引擎名：`is_supported`

Returns `true` if plane tracking is supported by the current device.

### StartEntityDiscovery

```csharp
public OpenXRFutureResult StartEntityDiscovery(Rid spatialContext, Array<OpenXRSpatialComponentData> componentData, OpenXRStructureBase nextSnapshotCreate = null, OpenXRStructureBase nextSnapshotQuery = null, Callable userCallback = default)
```

引擎名：`start_entity_discovery`

Calls [`OpenXRSpatialEntityExtension.DiscoverSpatialEntities`](OpenXRSpatialEntityExtension.md) and [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md) with the plane entities associated with `spatialContext`.

`componentData` are the [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md)s to discover for this plane capability.

If `nextSnapshotCreate` is non-null, then pass this to the `next` parameter in [`OpenXRSpatialEntityExtension.DiscoverSpatialEntities`](OpenXRSpatialEntityExtension.md).

If `nextSnapshotQuery` is non-null, then pass this to the `next` parameter in [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

`userCallback`, when non-null, is called with two parameters usually twice. The first parameter is the [`Rid`](Rid.md) of the discovery snapshot and the second parameter is a boolean where `false` indicates the discovery snapshot is about to be processed, and `true` indicates the discovery snapshot has been processed and `componentData` has valid data. The second call is skipped if an error was encountered.

The returned [`OpenXRFutureResult`](OpenXRFutureResult.md) is identical to the return from [`OpenXRSpatialEntityExtension.DiscoverSpatialEntities`](OpenXRSpatialEntityExtension.md).
