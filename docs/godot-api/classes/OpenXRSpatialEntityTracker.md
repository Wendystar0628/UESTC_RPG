<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialEntityTracker

命名空间 `Godot`；class；继承 [`XRPositionalTracker`](XRPositionalTracker.md) → [`XRTracker`](XRTracker.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

These are trackers created and managed by OpenXR's spatial entity extensions that give access to specific data related to OpenXR's spatial entities. They will always be of type `TRACKER_ANCHOR`.

## 构造函数

### OpenXRSpatialEntityTracker

```csharp
public OpenXRSpatialEntityTracker()
```

## 属性

### Entity

```csharp
public Rid Entity { get; set; }
```

引擎名：`entity`

The spatial entity associated with this tracker.

### SpatialTrackingState

```csharp
public OpenXRSpatialEntityTracker.EntityTrackingState SpatialTrackingState { get; set; }
```

引擎名：`spatial_tracking_state`

The spatial tracking state for this tracker.

## 方法

### AddNext

```csharp
public void AddNext(OpenXRStructureBase next)
```

引擎名：`add_next`

Adds a new [`OpenXRStructureBase`](OpenXRStructureBase.md) to the next-chain.

`GetNext()` will return this `next` until either `AddNext()` is called again or it's removed in `RemoveNext()`.

### GetEntity

```csharp
public Rid GetEntity()
```

引擎名：`get_entity`

### GetNext

```csharp
public OpenXRStructureBase GetNext()
```

引擎名：`get_next`

Gets the head [`OpenXRStructureBase`](OpenXRStructureBase.md) in the next-chain.

See also `AddNext()` and `RemoveNext()`.

### GetSpatialContext

```csharp
public Rid GetSpatialContext()
```

引擎名：`get_spatial_context`

Gets the spatial context used to create this `OpenXRSpatialEntityTracker`.

### GetSpatialTrackingState

```csharp
public OpenXRSpatialEntityTracker.EntityTrackingState GetSpatialTrackingState()
```

引擎名：`get_spatial_tracking_state`

### RemoveNext

```csharp
public void RemoveNext(OpenXRStructureBase next)
```

引擎名：`remove_next`

Removes a `next` object previously added in `AddNext()` from the next-chain.

### SetEntity

```csharp
public void SetEntity(Rid entity)
```

引擎名：`set_entity`

### SetSpatialContext

```csharp
public void SetSpatialContext(Rid spatialContext)
```

引擎名：`set_spatial_context`

Sets the spatial context used to create this tracker.

### SetSpatialTrackingState

```csharp
public void SetSpatialTrackingState(OpenXRSpatialEntityTracker.EntityTrackingState spatialTrackingState)
```

引擎名：`set_spatial_tracking_state`

## 事件（信号）

### NextChanged

```csharp
public event Action NextChanged
```

引擎名：`next_changed`

Emitted when the next-chain changes, from either `AddNext()` or `RemoveNext()`.

### SpatialTrackingStateChanged

```csharp
public event OpenXRSpatialEntityTracker.SpatialTrackingStateChangedEventHandler SpatialTrackingStateChanged
```

引擎名：`spatial_tracking_state_changed`

## EntityTrackingState

命名空间 `Godot`；enum

### 枚举值

- `Stopped = 1`：This anchor has stopped tracking.
- `Paused = 2`：Tracking is currently paused.
- `Tracking = 3`：This anchor is currently being tracked.

## SpatialTrackingStateChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OpenXRSpatialEntityTracker.SpatialTrackingStateChanged`](OpenXRSpatialEntityTracker.md) event of a [`OpenXRSpatialEntityTracker`](OpenXRSpatialEntityTracker.md) class.

### 签名

#### Invoke

```csharp
delegate void SpatialTrackingStateChangedEventHandler(long spatialTrackingState)
```
