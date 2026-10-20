<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialComponentMarkerList

命名空间 `Godot`；class；继承 [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Object for storing the queries marker result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

## 构造函数

### OpenXRSpatialComponentMarkerList

```csharp
public OpenXRSpatialComponentMarkerList()
```

## 方法

### GetMarkerData

```csharp
public Variant GetMarkerData(Rid snapshot, long index)
```

引擎名：`get_marker_data`

Returns either a `System.String` or a `System.Byte`[] buffer with data for the marker at this `index`. Only applicable for QR code markers.

### GetMarkerId

```csharp
public uint GetMarkerId(long index)
```

引擎名：`get_marker_id`

Returns the marker ID for the marker at this `index`. Only applicable for Aruco or April Tag markers.

### GetMarkerType

```csharp
public OpenXRSpatialComponentMarkerList.MarkerType GetMarkerType(long index)
```

引擎名：`get_marker_type`

Returns the marker type for the marker at this `index`.

## MarkerType

命名空间 `Godot`；enum

### 枚举值

- `Unknown = 0`：Unknown or unset marker type.
- `Qrcode = 1`：Marker based on a QR code.
- `MicroQrcode = 2`：Marker based on a micro QR code.
- `Aruco = 3`：Marker based on an Aruco code.
- `AprilTag = 4`：Marker based on an April Tag.
- `Max = 5`：Maximum value for this enum.
