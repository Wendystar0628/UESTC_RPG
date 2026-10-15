<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRPositionalTracker

命名空间 `Godot`；class；继承 [`XRTracker`](XRTracker.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An instance of this object represents a device that is tracked, such as a controller or anchor point. HMDs aren't represented here as they are handled internally.

As controllers are turned on and the [`XRInterface`](XRInterface.md) detects them, instances of this object are automatically added to this list of active tracking objects accessible through the [`XRServer`](XRServer.md).

The [`XRNode3D`](XRNode3D.md) and [`XRAnchor3D`](XRAnchor3D.md) both consume objects of this type and should be used in your project. The positional trackers are just under-the-hood objects that make this all work. These are mostly exposed so that GDExtension-based interfaces can interact with them.

## 构造函数

### XRPositionalTracker

```csharp
public XRPositionalTracker()
```

## 属性

### Hand

```csharp
public XRPositionalTracker.TrackerHand Hand { get; set; }
```

引擎名：`hand`

Defines which hand this tracker relates to.

### Profile

```csharp
public string Profile { get; set; }
```

引擎名：`profile`

The profile associated with this tracker, interface dependent but will indicate the type of controller being tracked.

## 方法

### GetInput

```csharp
public Variant GetInput(StringName name)
```

引擎名：`get_input`

Returns an input for this tracker. It can return a boolean, float or [`Vector2`](Vector2.md) value depending on whether the input is a button, trigger or thumbstick/thumbpad.

### GetPose

```csharp
public XRPose GetPose(StringName name)
```

引擎名：`get_pose`

Returns the current [`XRPose`](XRPose.md) state object for the bound `name` pose.

### GetTrackerHand

```csharp
public XRPositionalTracker.TrackerHand GetTrackerHand()
```

引擎名：`get_tracker_hand`

### GetTrackerProfile

```csharp
public string GetTrackerProfile()
```

引擎名：`get_tracker_profile`

### HasPose

```csharp
public bool HasPose(StringName name)
```

引擎名：`has_pose`

Returns `true` if the tracker is available and is currently tracking the bound `name` pose.

### InvalidatePose

```csharp
public void InvalidatePose(StringName name)
```

引擎名：`invalidate_pose`

Marks this pose as invalid, we don't clear the last reported state but it allows users to decide if trackers need to be hidden if we lose tracking or just remain at their last known position.

### SetInput

```csharp
public void SetInput(StringName name, Variant value)
```

引擎名：`set_input`

Changes the value for the given input. This method is called by an [`XRInterface`](XRInterface.md) implementation and should not be used directly.

### SetPose

```csharp
public void SetPose(StringName name, Transform3D transform, Vector3 linearVelocity, Vector3 angularVelocity, XRPose.TrackingConfidenceEnum trackingConfidence)
```

引擎名：`set_pose`

Sets the transform, linear velocity, angular velocity and tracking confidence for the given pose. This method is called by an [`XRInterface`](XRInterface.md) implementation and should not be used directly.

### SetTrackerHand

```csharp
public void SetTrackerHand(XRPositionalTracker.TrackerHand hand)
```

引擎名：`set_tracker_hand`

### SetTrackerProfile

```csharp
public void SetTrackerProfile(string profile)
```

引擎名：`set_tracker_profile`

## 事件（信号）

### ButtonPressed

```csharp
public event XRPositionalTracker.ButtonPressedEventHandler ButtonPressed
```

引擎名：`button_pressed`

Emitted when a button on this tracker is pressed. Note that many XR runtimes allow other inputs to be mapped to buttons.

### ButtonReleased

```csharp
public event XRPositionalTracker.ButtonReleasedEventHandler ButtonReleased
```

引擎名：`button_released`

Emitted when a button on this tracker is released.

### InputFloatChanged

```csharp
public event XRPositionalTracker.InputFloatChangedEventHandler InputFloatChanged
```

引擎名：`input_float_changed`

Emitted when a trigger or similar input on this tracker changes value.

### InputVector2Changed

```csharp
public event XRPositionalTracker.InputVector2ChangedEventHandler InputVector2Changed
```

引擎名：`input_vector2_changed`

Emitted when a thumbstick or thumbpad on this tracker moves.

### PoseChanged

```csharp
public event XRPositionalTracker.PoseChangedEventHandler PoseChanged
```

引擎名：`pose_changed`

Emitted when the state of a pose tracked by this tracker changes.

### PoseLostTracking

```csharp
public event XRPositionalTracker.PoseLostTrackingEventHandler PoseLostTracking
```

引擎名：`pose_lost_tracking`

Emitted when a pose tracked by this tracker stops getting updated tracking data.

### ProfileChanged

```csharp
public event XRPositionalTracker.ProfileChangedEventHandler ProfileChanged
```

引擎名：`profile_changed`

Emitted when the profile of our tracker changes.

## ButtonPressedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRPositionalTracker.ButtonPressed`](XRPositionalTracker.md) event of a [`XRPositionalTracker`](XRPositionalTracker.md) class.

### 签名

#### Invoke

```csharp
delegate void ButtonPressedEventHandler(string actionName)
```

## ButtonReleasedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRPositionalTracker.ButtonReleased`](XRPositionalTracker.md) event of a [`XRPositionalTracker`](XRPositionalTracker.md) class.

### 签名

#### Invoke

```csharp
delegate void ButtonReleasedEventHandler(string actionName)
```

## InputFloatChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRPositionalTracker.InputFloatChanged`](XRPositionalTracker.md) event of a [`XRPositionalTracker`](XRPositionalTracker.md) class.

### 签名

#### Invoke

```csharp
delegate void InputFloatChangedEventHandler(string actionName, double value)
```

## InputVector2ChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRPositionalTracker.InputVector2Changed`](XRPositionalTracker.md) event of a [`XRPositionalTracker`](XRPositionalTracker.md) class.

### 签名

#### Invoke

```csharp
delegate void InputVector2ChangedEventHandler(string actionName, Vector2 vector)
```

## PoseChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRPositionalTracker.PoseChanged`](XRPositionalTracker.md) event of a [`XRPositionalTracker`](XRPositionalTracker.md) class.

### 签名

#### Invoke

```csharp
delegate void PoseChangedEventHandler(XRPose pose)
```

## PoseLostTrackingEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRPositionalTracker.PoseLostTracking`](XRPositionalTracker.md) event of a [`XRPositionalTracker`](XRPositionalTracker.md) class.

### 签名

#### Invoke

```csharp
delegate void PoseLostTrackingEventHandler(XRPose pose)
```

## ProfileChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRPositionalTracker.ProfileChanged`](XRPositionalTracker.md) event of a [`XRPositionalTracker`](XRPositionalTracker.md) class.

### 签名

#### Invoke

```csharp
delegate void ProfileChangedEventHandler(string role)
```

## TrackerHand

命名空间 `Godot`；enum

### 枚举值

- `Unknown = 0`：The hand this tracker is held in is unknown or not applicable.
- `Left = 1`：This tracker is the left hand controller.
- `Right = 2`：This tracker is the right hand controller.
- `Max = 3`：Represents the size of the `XRPositionalTracker.TrackerHand` enum.
