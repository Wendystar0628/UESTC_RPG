<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRPose

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

XR runtimes often identify multiple locations on devices such as controllers that are spatially tracked.

Orientation, location, linear velocity and angular velocity are all provided for each pose by the XR runtime. This object contains this state of a pose.

## 构造函数

### XRPose

```csharp
public XRPose()
```

## 属性

### AngularVelocity

```csharp
public Vector3 AngularVelocity { get; set; }
```

引擎名：`angular_velocity`

The angular velocity for this pose.

### HasTrackingData

```csharp
public bool HasTrackingData { get; set; }
```

引擎名：`has_tracking_data`

If `true` our tracking data is up to date. If `false` we're no longer receiving new tracking data and our state is whatever that last valid state was.

### LinearVelocity

```csharp
public Vector3 LinearVelocity { get; set; }
```

引擎名：`linear_velocity`

The linear velocity of this pose.

### Name

```csharp
public StringName Name { get; set; }
```

引擎名：`name`

The name of this pose. Usually, this name is derived from an action map set up by the user. Godot also suggests some pose names that [`XRInterface`](XRInterface.md) objects are expected to implement:

- `root` is the root location, often used for tracked objects that do not have further nodes.

- `aim` is the tip of a controller with its orientation pointing outwards, often used for raycasts.

- `grip` is the location where the user grips the controller.

- `skeleton` is the root location for a hand mesh, when using hand tracking and an animated skeleton is supplied by the XR runtime.

### TrackingConfidence

```csharp
public XRPose.TrackingConfidenceEnum TrackingConfidence { get; set; }
```

引擎名：`tracking_confidence`

The tracking confidence for this pose, provides insight on how accurate the spatial positioning of this record is.

### Transform

```csharp
public Transform3D Transform { get; set; }
```

引擎名：`transform`

The transform containing the original and transform as reported by the XR runtime.

## 方法

### GetAdjustedTransform

```csharp
public Transform3D GetAdjustedTransform()
```

引擎名：`get_adjusted_transform`

Returns the `Transform` with world scale and our reference frame applied. This is the transform used to position [`XRNode3D`](XRNode3D.md) objects.

### GetAngularVelocity

```csharp
public Vector3 GetAngularVelocity()
```

引擎名：`get_angular_velocity`

### GetHasTrackingData

```csharp
public bool GetHasTrackingData()
```

引擎名：`get_has_tracking_data`

### GetLinearVelocity

```csharp
public Vector3 GetLinearVelocity()
```

引擎名：`get_linear_velocity`

### GetName

```csharp
public StringName GetName()
```

引擎名：`get_name`

### GetTrackingConfidence

```csharp
public XRPose.TrackingConfidenceEnum GetTrackingConfidence()
```

引擎名：`get_tracking_confidence`

### GetTransform

```csharp
public Transform3D GetTransform()
```

引擎名：`get_transform`

### SetAngularVelocity

```csharp
public void SetAngularVelocity(Vector3 velocity)
```

引擎名：`set_angular_velocity`

### SetHasTrackingData

```csharp
public void SetHasTrackingData(bool hasTrackingData)
```

引擎名：`set_has_tracking_data`

### SetLinearVelocity

```csharp
public void SetLinearVelocity(Vector3 velocity)
```

引擎名：`set_linear_velocity`

### SetName

```csharp
public void SetName(StringName name)
```

引擎名：`set_name`

### SetTrackingConfidence

```csharp
public void SetTrackingConfidence(XRPose.TrackingConfidenceEnum trackingConfidence)
```

引擎名：`set_tracking_confidence`

### SetTransform

```csharp
public void SetTransform(Transform3D transform)
```

引擎名：`set_transform`

## TrackingConfidenceEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No tracking information is available for this pose.
- `Low = 1`：Tracking information may be inaccurate or estimated.
- `High = 2`：Tracking information is considered accurate and up to date.
