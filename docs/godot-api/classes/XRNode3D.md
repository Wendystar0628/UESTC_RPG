<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRNode3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node can be bound to a specific pose of an [`XRPositionalTracker`](XRPositionalTracker.md) and will automatically have its [`Node3D.Transform`](Node3D.md) updated by the [`XRServer`](XRServer.md). Nodes of this type must be added as children of the [`XROrigin3D`](XROrigin3D.md) node.

## 构造函数

### XRNode3D

```csharp
public XRNode3D()
```

## 属性

### Pose

```csharp
public StringName Pose { get; set; }
```

引擎名：`pose`

The name of the pose we're bound to. Which poses a tracker supports is not known during design time.

Godot defines number of standard pose names such as `aim` and `grip` but other may be configured within a given [`XRInterface`](XRInterface.md).

### ShowWhenTracked

```csharp
public bool ShowWhenTracked { get; set; }
```

引擎名：`show_when_tracked`

Enables showing the node when tracking starts, and hiding the node when tracking is lost.

### Tracker

```csharp
public StringName Tracker { get; set; }
```

引擎名：`tracker`

The name of the tracker we're bound to. Which trackers are available is not known during design time.

Godot defines a number of standard trackers such as `left_hand` and `right_hand` but others may be configured within a given [`XRInterface`](XRInterface.md).

## 方法

### GetHasTrackingData

```csharp
public bool GetHasTrackingData()
```

引擎名：`get_has_tracking_data`

Returns `true` if the `Tracker` has current tracking data for the `Pose` being tracked.

### GetIsActive

```csharp
public bool GetIsActive()
```

引擎名：`get_is_active`

Returns `true` if the `Tracker` has been registered and the `Pose` is being tracked.

### GetPose

```csharp
public XRPose GetPose()
```

引擎名：`get_pose`

Returns the [`XRPose`](XRPose.md) containing the current state of the pose being tracked. This gives access to additional properties of this pose.

### GetPoseName

```csharp
public StringName GetPoseName()
```

引擎名：`get_pose_name`

### GetShowWhenTracked

```csharp
public bool GetShowWhenTracked()
```

引擎名：`get_show_when_tracked`

### GetTracker

```csharp
public StringName GetTracker()
```

引擎名：`get_tracker`

### SetPoseName

```csharp
public void SetPoseName(StringName pose)
```

引擎名：`set_pose_name`

### SetShowWhenTracked

```csharp
public void SetShowWhenTracked(bool show)
```

引擎名：`set_show_when_tracked`

### SetTracker

```csharp
public void SetTracker(StringName trackerName)
```

引擎名：`set_tracker`

### TriggerHapticPulse

```csharp
public void TriggerHapticPulse(string actionName, double frequency, double amplitude, double durationSec, double delaySec)
```

引擎名：`trigger_haptic_pulse`

Triggers a haptic pulse on a device associated with this interface.

`actionName` is the name of the action for this pulse.

`frequency` is the frequency of the pulse, set to `0.0` to have the system use a default frequency.

`amplitude` is the amplitude of the pulse between `0.0` and `1.0`.

`durationSec` is the duration of the pulse in seconds.

`delaySec` is a delay in seconds before the pulse is given.

## 事件（信号）

### TrackingChanged

```csharp
public event XRNode3D.TrackingChangedEventHandler TrackingChanged
```

引擎名：`tracking_changed`

Emitted when the `Tracker` starts or stops receiving updated tracking data for the `Pose` being tracked. The `tracking` argument indicates whether the tracker is getting updated tracking data.

## TrackingChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRNode3D.TrackingChanged`](XRNode3D.md) event of a [`XRNode3D`](XRNode3D.md) class.

### 签名

#### Invoke

```csharp
delegate void TrackingChangedEventHandler(bool tracking)
```
