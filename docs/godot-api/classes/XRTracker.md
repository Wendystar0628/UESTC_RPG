<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRTracker

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This object is the base of all XR trackers.

## 属性

### Description

```csharp
public string Description { get; set; }
```

引擎名：`description`

The description of this tracker.

### Name

```csharp
public StringName Name { get; set; }
```

引擎名：`name`

The unique name of this tracker. The trackers that are available differ between various XR runtimes and can often be configured by the user. Godot maintains a number of reserved names that it expects the [`XRInterface`](XRInterface.md) to implement if applicable:

- `"head"` identifies the [`XRPositionalTracker`](XRPositionalTracker.md) of the player's head

- `"left_hand"` identifies the [`XRControllerTracker`](XRControllerTracker.md) in the player's left hand

- `"right_hand"` identifies the [`XRControllerTracker`](XRControllerTracker.md) in the player's right hand

- `"/user/hand_tracker/left"` identifies the [`XRHandTracker`](XRHandTracker.md) for the player's left hand

- `"/user/hand_tracker/right"` identifies the [`XRHandTracker`](XRHandTracker.md) for the player's right hand

- `"/user/body_tracker"` identifies the [`XRBodyTracker`](XRBodyTracker.md) for the player's body

- `"/user/face_tracker"` identifies the [`XRFaceTracker`](XRFaceTracker.md) for the player's face

### Type

```csharp
public XRServer.TrackerType Type { get; set; }
```

引擎名：`type`

The type of tracker.

## 方法

### GetTrackerDesc

```csharp
public string GetTrackerDesc()
```

引擎名：`get_tracker_desc`

### GetTrackerName

```csharp
public StringName GetTrackerName()
```

引擎名：`get_tracker_name`

### GetTrackerType

```csharp
public XRServer.TrackerType GetTrackerType()
```

引擎名：`get_tracker_type`

### SetTrackerDesc

```csharp
public void SetTrackerDesc(string description)
```

引擎名：`set_tracker_desc`

### SetTrackerName

```csharp
public void SetTrackerName(StringName name)
```

引擎名：`set_tracker_name`

### SetTrackerType

```csharp
public void SetTrackerType(XRServer.TrackerType type)
```

引擎名：`set_tracker_type`
