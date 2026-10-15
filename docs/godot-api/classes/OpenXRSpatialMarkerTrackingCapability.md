<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialMarkerTrackingCapability

命名空间 `Godot`；class；继承 [`OpenXRExtensionWrapper`](OpenXRExtensionWrapper.md) → [`GodotObject`](GodotObject.md)

This class handles the OpenXR marker tracking spatial entity extension.

## 构造函数

### OpenXRSpatialMarkerTrackingCapability

```csharp
public OpenXRSpatialMarkerTrackingCapability()
```

## 方法

### DoEntityUpdate

```csharp
public void DoEntityUpdate(Rid spatialContext, Array<OpenXRSpatialComponentData> componentData, OpenXRStructureBase nextSnapshotCreate = null, OpenXRStructureBase nextSnapshotQuery = null)
```

引擎名：`do_entity_update`

Calls [`OpenXRSpatialEntityExtension.UpdateSpatialEntities`](OpenXRSpatialEntityExtension.md) and [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md) with the marker entities associated with `spatialContext`.

`componentData` are the [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md)s to update for this marker capability.

If `nextSnapshotCreate` is non-null, then pass this to the `next` parameter in [`OpenXRSpatialEntityExtension.UpdateSpatialEntities`](OpenXRSpatialEntityExtension.md).

If `nextSnapshotQuery` is non-null, then pass this to the `next` parameter in [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

### IsAprilTagSupported

```csharp
public bool IsAprilTagSupported()
```

引擎名：`is_april_tag_supported`

Returns `true` if April tag marker tracking is supported by the current device.

### IsArucoSupported

```csharp
public bool IsArucoSupported()
```

引擎名：`is_aruco_supported`

Returns `true` if Aruco marker tracking is supported by the current device.

### IsMicroQrcodeSupported

```csharp
public bool IsMicroQrcodeSupported()
```

引擎名：`is_micro_qrcode_supported`

Returns `true` if micro QR code marker tracking is supported by the current device.

### IsQrcodeSupported

```csharp
public bool IsQrcodeSupported()
```

引擎名：`is_qrcode_supported`

Returns `true` if QR code marker tracking is supported by the current device.

### StartEntityDiscovery

```csharp
public OpenXRFutureResult StartEntityDiscovery(Rid spatialContext, Array<OpenXRSpatialComponentData> componentData, OpenXRStructureBase nextSnapshotCreate = null, OpenXRStructureBase nextSnapshotQuery = null, Callable userCallback = default)
```

引擎名：`start_entity_discovery`

Calls [`OpenXRSpatialEntityExtension.DiscoverSpatialEntities`](OpenXRSpatialEntityExtension.md) and [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md) with the marker entities associated with `spatialContext`.

`componentData` are the [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md)s to discover for this marker capability.

If `nextSnapshotCreate` is non-null, then pass this to the `next` parameter in [`OpenXRSpatialEntityExtension.DiscoverSpatialEntities`](OpenXRSpatialEntityExtension.md).

If `nextSnapshotQuery` is non-null, then pass this to the `next` parameter in [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

`userCallback`, when non-null, is called with two parameters usually twice. The first parameter is the [`Rid`](Rid.md) of the discovery snapshot and the second parameter is a boolean where `false` indicates the discovery snapshot is about to be processed, and `true` indicates the discovery snapshot has been processed and `componentData` has valid data. The second call is skipped if an error was encountered.

The returned [`OpenXRFutureResult`](OpenXRFutureResult.md) is identical to the return from [`OpenXRSpatialEntityExtension.DiscoverSpatialEntities`](OpenXRSpatialEntityExtension.md).
