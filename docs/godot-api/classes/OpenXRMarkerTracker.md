<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRMarkerTracker

命名空间 `Godot`；class；继承 [`OpenXRSpatialEntityTracker`](OpenXRSpatialEntityTracker.md) → [`XRPositionalTracker`](XRPositionalTracker.md) → [`XRTracker`](XRTracker.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Spatial entity tracker for our OpenXR spatial entity marker tracking extension. These trackers identify entities in our real space detected by a visual marker such as a QRCode or Aruco code, and map their location to our virtual space.

## 构造函数

### OpenXRMarkerTracker

```csharp
public OpenXRMarkerTracker()
```

## 属性

### BoundsSize

```csharp
public Vector2 BoundsSize { get; set; }
```

引擎名：`bounds_size`

The bounds size for this marker.

### MarkerId

```csharp
public uint MarkerId { get; set; }
```

引擎名：`marker_id`

The marker ID for this marker, this is only returned for Aruco and April Tag markers. Call `GetMarkerData()` for QRCode markers.

### MarkerType

```csharp
public OpenXRSpatialComponentMarkerList.MarkerType MarkerType { get; set; }
```

引擎名：`marker_type`

The type of marker.

## 方法

### GetBoundsSize

```csharp
public Vector2 GetBoundsSize()
```

引擎名：`get_bounds_size`

### GetMarkerData

```csharp
public Variant GetMarkerData()
```

引擎名：`get_marker_data`

Returns the marker data for this marker. This can return a `System.String` or `System.Byte`[]. Only applicable to QR Code based markers.

### GetMarkerId

```csharp
public uint GetMarkerId()
```

引擎名：`get_marker_id`

### GetMarkerType

```csharp
public OpenXRSpatialComponentMarkerList.MarkerType GetMarkerType()
```

引擎名：`get_marker_type`

### SetBoundsSize

```csharp
public void SetBoundsSize(Vector2 boundsSize)
```

引擎名：`set_bounds_size`

### SetMarkerData

```csharp
public void SetMarkerData(Variant markerData)
```

引擎名：`set_marker_data`

Sets the marker data for this marker.

**Note:** This should only be set by marker discovery logic.

### SetMarkerId

```csharp
public void SetMarkerId(uint markerId)
```

引擎名：`set_marker_id`

### SetMarkerType

```csharp
public void SetMarkerType(OpenXRSpatialComponentMarkerList.MarkerType markerType)
```

引擎名：`set_marker_type`
