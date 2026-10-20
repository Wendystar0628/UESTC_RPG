<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRServer

命名空间 `Godot`；static class

The AR/VR server is the heart of our Advanced and Virtual Reality solution and handles all the processing.

## 属性

### CameraLockedToOrigin

```csharp
public static bool CameraLockedToOrigin { get; set; }
```

引擎名：`camera_locked_to_origin`

If set to `true`, the scene will be rendered as if the camera is locked to the [`XROrigin3D`](XROrigin3D.md).

**Note:** This doesn't provide a very comfortable experience for users. This setting exists for doing benchmarking or automated testing, where you want to control what is rendered via code.

### PrimaryInterface

```csharp
public static XRInterface PrimaryInterface { get; set; }
```

引擎名：`primary_interface`

The primary [`XRInterface`](XRInterface.md) currently bound to the `XRServer`.

### Singleton

```csharp
public static XRServerInstance Singleton { get; }
```

### WorldOrigin

```csharp
public static Transform3D WorldOrigin { get; set; }
```

引擎名：`world_origin`

The current origin of our tracking space in the virtual world. This is used by the renderer to properly position the camera with new tracking data.

**Note:** This property is managed by the current [`XROrigin3D`](XROrigin3D.md) node. It is exposed for access from GDExtensions.

### WorldScale

```csharp
public static double WorldScale { get; set; }
```

引擎名：`world_scale`

The scale of the game world compared to the real world. By default, most AR/VR platforms assume that 1 game unit corresponds to 1 real world meter.

## 方法

### AddInterface

```csharp
public static void AddInterface(XRInterface interface)
```

引擎名：`add_interface`

Registers an [`XRInterface`](XRInterface.md) object.

### AddTracker

```csharp
public static void AddTracker(XRPositionalTracker tracker)
```

引擎名：`add_tracker`

Registers a new [`XRTracker`](XRTracker.md) that tracks a physical object.

### AddTracker

```csharp
public static void AddTracker(XRTracker tracker)
```

引擎名：`add_tracker`

Registers a new [`XRTracker`](XRTracker.md) that tracks a physical object.

### CenterOnHmd

```csharp
public static void CenterOnHmd(XRServer.RotationMode rotationMode, bool keepHeight)
```

引擎名：`center_on_hmd`

This is an important function to understand correctly. AR and VR platforms all handle positioning slightly differently.

For platforms that do not offer spatial tracking, our origin point `(0, 0, 0)` is the location of our HMD, but you have little control over the direction the player is facing in the real world.

For platforms that do offer spatial tracking, our origin point depends very much on the system. For OpenVR, our origin point is usually the center of the tracking space, on the ground. For other platforms, it's often the location of the tracking camera.

This method allows you to center your tracker on the location of the HMD. It will take the current location of the HMD and use that to adjust all your tracking data; in essence, realigning the real world to your player's current position in the game world.

For this method to produce usable results, tracking information must be available. This often takes a few frames after starting your game.

You should call this method after a few seconds have passed. For example, when the user requests a realignment of the display holding a designated button on a controller for a short period of time, or when implementing a teleport mechanism.

### ClearReferenceFrame

```csharp
public static void ClearReferenceFrame()
```

引擎名：`clear_reference_frame`

Clears the reference frame that was set by previous calls to `CenterOnHmd()`.

### FindInterface

```csharp
public static XRInterface FindInterface(string name)
```

引擎名：`find_interface`

Finds an interface by its `name`. For example, if your project uses capabilities of an AR/VR platform, you can find the interface for that platform by name and initialize it.

### GetHmdTransform

```csharp
public static Transform3D GetHmdTransform()
```

引擎名：`get_hmd_transform`

Returns the primary interface's transformation.

### GetInterface

```csharp
public static XRInterface GetInterface(int idx)
```

引擎名：`get_interface`

Returns the interface registered at the given `idx` index in the list of interfaces.

### GetInterfaceCount

```csharp
public static int GetInterfaceCount()
```

引擎名：`get_interface_count`

Returns the number of interfaces currently registered with the AR/VR server. If your project supports multiple AR/VR platforms, you can look through the available interface, and either present the user with a selection or simply try to initialize each interface and use the first one that returns `true`.

### GetInterfaces

```csharp
public static Array<Dictionary> GetInterfaces()
```

引擎名：`get_interfaces`

Returns a list of available interfaces the ID and name of each interface.

### GetPrimaryInterface

```csharp
public static XRInterface GetPrimaryInterface()
```

引擎名：`get_primary_interface`

### GetReferenceFrame

```csharp
public static Transform3D GetReferenceFrame()
```

引擎名：`get_reference_frame`

Returns the reference frame transform. Mostly used internally and exposed for GDExtension build interfaces.

### GetTracker

```csharp
public static XRTracker GetTracker(StringName trackerName)
```

引擎名：`get_tracker`

Returns the positional tracker with the given `trackerName`.

### GetTrackers

```csharp
public static Dictionary GetTrackers(int trackerTypes)
```

引擎名：`get_trackers`

Returns a dictionary of trackers for `trackerTypes`.

### GetWorldOrigin

```csharp
public static Transform3D GetWorldOrigin()
```

引擎名：`get_world_origin`

### GetWorldScale

```csharp
public static double GetWorldScale()
```

引擎名：`get_world_scale`

### IsCameraLockedToOrigin

```csharp
public static bool IsCameraLockedToOrigin()
```

引擎名：`is_camera_locked_to_origin`

### RemoveInterface

```csharp
public static void RemoveInterface(XRInterface interface)
```

引擎名：`remove_interface`

Removes this `interface`.

### RemoveTracker

```csharp
public static void RemoveTracker(XRPositionalTracker tracker)
```

引擎名：`remove_tracker`

Removes this `tracker`.

### RemoveTracker

```csharp
public static void RemoveTracker(XRTracker tracker)
```

引擎名：`remove_tracker`

Removes this `tracker`.

### SetCameraLockedToOrigin

```csharp
public static void SetCameraLockedToOrigin(bool enabled)
```

引擎名：`set_camera_locked_to_origin`

### SetPrimaryInterface

```csharp
public static void SetPrimaryInterface(XRInterface interface)
```

引擎名：`set_primary_interface`

### SetWorldOrigin

```csharp
public static void SetWorldOrigin(Transform3D worldOrigin)
```

引擎名：`set_world_origin`

### SetWorldScale

```csharp
public static void SetWorldScale(double scale)
```

引擎名：`set_world_scale`

## 事件（信号）

### InterfaceAdded

```csharp
public event XRServer.InterfaceAddedEventHandler InterfaceAdded
```

引擎名：`interface_added`

Emitted when a new interface has been added.

### InterfaceRemoved

```csharp
public event XRServer.InterfaceRemovedEventHandler InterfaceRemoved
```

引擎名：`interface_removed`

Emitted when an interface is removed.

### ReferenceFrameChanged

```csharp
public event Action ReferenceFrameChanged
```

引擎名：`reference_frame_changed`

Emitted when the reference frame transform changes.

### TrackerAdded

```csharp
public event XRServer.TrackerAddedEventHandler TrackerAdded
```

引擎名：`tracker_added`

Emitted when a new tracker has been added. If you don't use a fixed number of controllers or if you're using [`XRAnchor3D`](XRAnchor3D.md)s for an AR solution, it is important to react to this signal to add the appropriate [`XRController3D`](XRController3D.md) or [`XRAnchor3D`](XRAnchor3D.md) nodes related to this new tracker.

### TrackerRemoved

```csharp
public event XRServer.TrackerRemovedEventHandler TrackerRemoved
```

引擎名：`tracker_removed`

Emitted when a tracker is removed. You should remove any [`XRController3D`](XRController3D.md) or [`XRAnchor3D`](XRAnchor3D.md) points if applicable. This is not mandatory, the nodes simply become inactive and will be made active again when a new tracker becomes available (i.e. a new controller is switched on that takes the place of the previous one).

### TrackerUpdated

```csharp
public event XRServer.TrackerUpdatedEventHandler TrackerUpdated
```

引擎名：`tracker_updated`

Emitted when an existing tracker has been updated. This can happen if the user switches controllers.

### WorldOriginChanged

```csharp
public event Action WorldOriginChanged
```

引擎名：`world_origin_changed`

Emitted when the world origin transform changes.

## InterfaceAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRServer.InterfaceAdded`](XRServer.md) event of a [`XRServer`](XRServer.md) class.

### 签名

#### Invoke

```csharp
delegate void InterfaceAddedEventHandler(StringName interfaceName)
```

## InterfaceRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRServer.InterfaceRemoved`](XRServer.md) event of a [`XRServer`](XRServer.md) class.

### 签名

#### Invoke

```csharp
delegate void InterfaceRemovedEventHandler(StringName interfaceName)
```

## RotationMode

命名空间 `Godot`；enum

### 枚举值

- `ResetFullRotation = 0`：Fully reset the orientation of the HMD.
- `ResetButKeepTilt = 1`：Resets the orientation but keeps the tilt of the device.
- `DontResetRotation = 2`：Does not reset the orientation of the HMD, only the position of the player gets centered.

## TrackerAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRServer.TrackerAdded`](XRServer.md) event of a [`XRServer`](XRServer.md) class.

### 签名

#### Invoke

```csharp
delegate void TrackerAddedEventHandler(StringName trackerName, long type)
```

## TrackerRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRServer.TrackerRemoved`](XRServer.md) event of a [`XRServer`](XRServer.md) class.

### 签名

#### Invoke

```csharp
delegate void TrackerRemovedEventHandler(StringName trackerName, long type)
```

## TrackerType

命名空间 `Godot`；enum

### 枚举值

- `Head = 1`：The tracker tracks the location of the player's head.
- `Controller = 2`：The tracker tracks the location of a controller.
- `Basestation = 4`：The tracker tracks the location of a base station.
- `Anchor = 8`：The tracker tracks the location and size of an AR anchor.
- `Hand = 16`：The tracker tracks the location and joints of a hand.
- `Body = 32`：The tracker tracks the location and joints of a body.
- `Face = 64`：The tracker tracks the expressions of a face.
- `AnyKnown = 127`：Used internally to filter trackers of any known type.
- `Unknown = 128`：Used internally if we haven't set the tracker type yet.
- `Any = 255`：Used internally to select all trackers.

## TrackerUpdatedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRServer.TrackerUpdated`](XRServer.md) event of a [`XRServer`](XRServer.md) class.

### 签名

#### Invoke

```csharp
delegate void TrackerUpdatedEventHandler(StringName trackerName, long type)
```
