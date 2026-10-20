<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRHandTracker

命名空间 `Godot`；class；继承 [`XRPositionalTracker`](XRPositionalTracker.md) → [`XRTracker`](XRTracker.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A hand tracking system will create an instance of this object and add it to the [`XRServer`](XRServer.md). This tracking system will then obtain skeleton data, convert it to the Godot Humanoid hand skeleton and store this data on the `XRHandTracker` object.

Use [`XRHandModifier3D`](XRHandModifier3D.md) to animate a hand mesh using hand tracking data.

## 构造函数

### XRHandTracker

```csharp
public XRHandTracker()
```

## 属性

### HandTrackingSource

```csharp
public XRHandTracker.HandTrackingSourceEnum HandTrackingSource { get; set; }
```

引擎名：`hand_tracking_source`

The source of the hand tracking data.

### HasTrackingData

```csharp
public bool HasTrackingData { get; set; }
```

引擎名：`has_tracking_data`

If `true`, the hand tracking data is valid.

## 方法

### GetHandJointAngularVelocity

```csharp
public Vector3 GetHandJointAngularVelocity(XRHandTracker.HandJoint joint)
```

引擎名：`get_hand_joint_angular_velocity`

Returns the angular velocity for the given hand joint.

### GetHandJointFlags

```csharp
public XRHandTracker.HandJointFlags GetHandJointFlags(XRHandTracker.HandJoint joint)
```

引擎名：`get_hand_joint_flags`

Returns flags about the validity of the tracking data for the given hand joint.

### GetHandJointLinearVelocity

```csharp
public Vector3 GetHandJointLinearVelocity(XRHandTracker.HandJoint joint)
```

引擎名：`get_hand_joint_linear_velocity`

Returns the linear velocity for the given hand joint.

### GetHandJointRadius

```csharp
public float GetHandJointRadius(XRHandTracker.HandJoint joint)
```

引擎名：`get_hand_joint_radius`

Returns the radius of the given hand joint.

### GetHandJointTransform

```csharp
public Transform3D GetHandJointTransform(XRHandTracker.HandJoint joint)
```

引擎名：`get_hand_joint_transform`

Returns the transform for the given hand joint.

### GetHandTrackingSource

```csharp
public XRHandTracker.HandTrackingSourceEnum GetHandTrackingSource()
```

引擎名：`get_hand_tracking_source`

### GetHasTrackingData

```csharp
public bool GetHasTrackingData()
```

引擎名：`get_has_tracking_data`

### SetHandJointAngularVelocity

```csharp
public void SetHandJointAngularVelocity(XRHandTracker.HandJoint joint, Vector3 angularVelocity)
```

引擎名：`set_hand_joint_angular_velocity`

Sets the angular velocity for the given hand joint.

### SetHandJointFlags

```csharp
public void SetHandJointFlags(XRHandTracker.HandJoint joint, XRHandTracker.HandJointFlags flags)
```

引擎名：`set_hand_joint_flags`

Sets flags about the validity of the tracking data for the given hand joint.

### SetHandJointLinearVelocity

```csharp
public void SetHandJointLinearVelocity(XRHandTracker.HandJoint joint, Vector3 linearVelocity)
```

引擎名：`set_hand_joint_linear_velocity`

Sets the linear velocity for the given hand joint.

### SetHandJointRadius

```csharp
public void SetHandJointRadius(XRHandTracker.HandJoint joint, float radius)
```

引擎名：`set_hand_joint_radius`

Sets the radius of the given hand joint.

### SetHandJointTransform

```csharp
public void SetHandJointTransform(XRHandTracker.HandJoint joint, Transform3D transform)
```

引擎名：`set_hand_joint_transform`

Sets the transform for the given hand joint.

### SetHandTrackingSource

```csharp
public void SetHandTrackingSource(XRHandTracker.HandTrackingSourceEnum source)
```

引擎名：`set_hand_tracking_source`

### SetHasTrackingData

```csharp
public void SetHasTrackingData(bool hasData)
```

引擎名：`set_has_tracking_data`

## HandJoint

命名空间 `Godot`；enum

### 枚举值

- `Palm = 0`：Palm joint.
- `Wrist = 1`：Wrist joint.
- `ThumbMetacarpal = 2`：Thumb metacarpal joint.
- `ThumbPhalanxProximal = 3`：Thumb phalanx proximal joint.
- `ThumbPhalanxDistal = 4`：Thumb phalanx distal joint.
- `ThumbTip = 5`：Thumb tip joint.
- `IndexFingerMetacarpal = 6`：Index finger metacarpal joint.
- `IndexFingerPhalanxProximal = 7`：Index finger phalanx proximal joint.
- `IndexFingerPhalanxIntermediate = 8`：Index finger phalanx intermediate joint.
- `IndexFingerPhalanxDistal = 9`：Index finger phalanx distal joint.
- `IndexFingerTip = 10`：Index finger tip joint.
- `MiddleFingerMetacarpal = 11`：Middle finger metacarpal joint.
- `MiddleFingerPhalanxProximal = 12`：Middle finger phalanx proximal joint.
- `MiddleFingerPhalanxIntermediate = 13`：Middle finger phalanx intermediate joint.
- `MiddleFingerPhalanxDistal = 14`：Middle finger phalanx distal joint.
- `MiddleFingerTip = 15`：Middle finger tip joint.
- `RingFingerMetacarpal = 16`：Ring finger metacarpal joint.
- `RingFingerPhalanxProximal = 17`：Ring finger phalanx proximal joint.
- `RingFingerPhalanxIntermediate = 18`：Ring finger phalanx intermediate joint.
- `RingFingerPhalanxDistal = 19`：Ring finger phalanx distal joint.
- `RingFingerTip = 20`：Ring finger tip joint.
- `PinkyFingerMetacarpal = 21`：Pinky finger metacarpal joint.
- `PinkyFingerPhalanxProximal = 22`：Pinky finger phalanx proximal joint.
- `PinkyFingerPhalanxIntermediate = 23`：Pinky finger phalanx intermediate joint.
- `PinkyFingerPhalanxDistal = 24`：Pinky finger phalanx distal joint.
- `PinkyFingerTip = 25`：Pinky finger tip joint.
- `Max = 26`：Represents the size of the `XRHandTracker.HandJoint` enum.

## HandJointFlags

命名空间 `Godot`；enum

### 枚举值

- `OrientationValid = 1`：The hand joint's orientation data is valid.
- `OrientationTracked = 2`：The hand joint's orientation is actively tracked.
- `PositionValid = 4`：The hand joint's position data is valid.
- `PositionTracked = 8`：The hand joint's position is actively tracked.
- `LinearVelocityValid = 16`：The hand joint's linear velocity data is valid.
- `AngularVelocityValid = 32`：The hand joint's angular velocity data is valid.

## HandTrackingSourceEnum

命名空间 `Godot`；enum

### 枚举值

- `Unknown = 0`：The source of hand tracking data is unknown.
- `Unobstructed = 1`：The source of hand tracking data is unobstructed, meaning that an accurate method of hand tracking is used.
- `Controller = 2`：The source of hand tracking data is a controller, meaning that joint positions are inferred from controller inputs.
- `NotTracked = 3`：No hand tracking data is tracked, this either means the hand is obscured, the controller is turned off, or tracking is not supported for the current input type.
- `Max = 4`：Represents the size of the `XRHandTracker.HandTrackingSourceEnum` enum.
