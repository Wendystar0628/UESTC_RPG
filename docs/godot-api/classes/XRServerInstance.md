<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRServerInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The AR/VR server is the heart of our Advanced and Virtual Reality solution and handles all the processing.

## 属性

### CameraLockedToOrigin

```csharp
public bool CameraLockedToOrigin { get; set; }
```

If set to `true`, the scene will be rendered as if the camera is locked to the [`XROrigin3D`](XROrigin3D.md).

**Note:** This doesn't provide a very comfortable experience for users. This setting exists for doing benchmarking or automated testing, where you want to control what is rendered via code.

### PrimaryInterface

```csharp
public XRInterface PrimaryInterface { get; set; }
```

The primary [`XRInterface`](XRInterface.md) currently bound to the [`XRServer`](XRServer.md).

### WorldOrigin

```csharp
public Transform3D WorldOrigin { get; set; }
```

The current origin of our tracking space in the virtual world. This is used by the renderer to properly position the camera with new tracking data.

**Note:** This property is managed by the current [`XROrigin3D`](XROrigin3D.md) node. It is exposed for access from GDExtensions.

### WorldScale

```csharp
public double WorldScale { get; set; }
```

The scale of the game world compared to the real world. By default, most AR/VR platforms assume that 1 game unit corresponds to 1 real world meter.

## 方法

### AddInterface

```csharp
public void AddInterface(XRInterface interface)
```

Registers an [`XRInterface`](XRInterface.md) object.

### AddTracker

```csharp
public void AddTracker(XRPositionalTracker tracker)
```

Registers a new [`XRTracker`](XRTracker.md) that tracks a physical object.

### AddTracker

```csharp
public void AddTracker(XRTracker tracker)
```

Registers a new [`XRTracker`](XRTracker.md) that tracks a physical object.

### CenterOnHmd

```csharp
public void CenterOnHmd(XRServer.RotationMode rotationMode, bool keepHeight)
```

This is an important function to understand correctly. AR and VR platforms all handle positioning slightly differently.

For platforms that do not offer spatial tracking, our origin point `(0, 0, 0)` is the location of our HMD, but you have little control over the direction the player is facing in the real world.

For platforms that do offer spatial tracking, our origin point depends very much on the system. For OpenVR, our origin point is usually the center of the tracking space, on the ground. For other platforms, it's often the location of the tracking camera.

This method allows you to center your tracker on the location of the HMD. It will take the current location of the HMD and use that to adjust all your tracking data; in essence, realigning the real world to your player's current position in the game world.

For this method to produce usable results, tracking information must be available. This often takes a few frames after starting your game.

You should call this method after a few seconds have passed. For example, when the user requests a realignment of the display holding a designated button on a controller for a short period of time, or when implementing a teleport mechanism.

### ClearReferenceFrame

```csharp
public void ClearReferenceFrame()
```

Clears the reference frame that was set by previous calls to `CenterOnHmd()`.

### FindInterface

```csharp
public XRInterface FindInterface(string name)
```

Finds an interface by its `name`. For example, if your project uses capabilities of an AR/VR platform, you can find the interface for that platform by name and initialize it.

### GetHmdTransform

```csharp
public Transform3D GetHmdTransform()
```

Returns the primary interface's transformation.

### GetInterface

```csharp
public XRInterface GetInterface(int idx)
```

Returns the interface registered at the given `idx` index in the list of interfaces.

### GetInterfaceCount

```csharp
public int GetInterfaceCount()
```

Returns the number of interfaces currently registered with the AR/VR server. If your project supports multiple AR/VR platforms, you can look through the available interface, and either present the user with a selection or simply try to initialize each interface and use the first one that returns `true`.

### GetInterfaces

```csharp
public Array<Dictionary> GetInterfaces()
```

Returns a list of available interfaces the ID and name of each interface.

### GetPrimaryInterface

```csharp
public XRInterface GetPrimaryInterface()
```

### GetReferenceFrame

```csharp
public Transform3D GetReferenceFrame()
```

Returns the reference frame transform. Mostly used internally and exposed for GDExtension build interfaces.

### GetTracker

```csharp
public XRTracker GetTracker(StringName trackerName)
```

Returns the positional tracker with the given `trackerName`.

### GetTrackers

```csharp
public Dictionary GetTrackers(int trackerTypes)
```

Returns a dictionary of trackers for `trackerTypes`.

### GetWorldOrigin

```csharp
public Transform3D GetWorldOrigin()
```

### GetWorldScale

```csharp
public double GetWorldScale()
```

### IsCameraLockedToOrigin

```csharp
public bool IsCameraLockedToOrigin()
```

### RemoveInterface

```csharp
public void RemoveInterface(XRInterface interface)
```

Removes this `interface`.

### RemoveTracker

```csharp
public void RemoveTracker(XRPositionalTracker tracker)
```

Removes this `tracker`.

### RemoveTracker

```csharp
public void RemoveTracker(XRTracker tracker)
```

Removes this `tracker`.

### SetCameraLockedToOrigin

```csharp
public void SetCameraLockedToOrigin(bool enabled)
```

### SetPrimaryInterface

```csharp
public void SetPrimaryInterface(XRInterface interface)
```

### SetWorldOrigin

```csharp
public void SetWorldOrigin(Transform3D worldOrigin)
```

### SetWorldScale

```csharp
public void SetWorldScale(double scale)
```

## 事件（信号）

### InterfaceAdded

```csharp
public event XRServerInstance.InterfaceAddedEventHandler InterfaceAdded
```

Emitted when a new interface has been added.

### InterfaceRemoved

```csharp
public event XRServerInstance.InterfaceRemovedEventHandler InterfaceRemoved
```

Emitted when an interface is removed.

### ReferenceFrameChanged

```csharp
public event Action ReferenceFrameChanged
```

Emitted when the reference frame transform changes.

### TrackerAdded

```csharp
public event XRServerInstance.TrackerAddedEventHandler TrackerAdded
```

Emitted when a new tracker has been added. If you don't use a fixed number of controllers or if you're using [`XRAnchor3D`](XRAnchor3D.md)s for an AR solution, it is important to react to this signal to add the appropriate [`XRController3D`](XRController3D.md) or [`XRAnchor3D`](XRAnchor3D.md) nodes related to this new tracker.

### TrackerRemoved

```csharp
public event XRServerInstance.TrackerRemovedEventHandler TrackerRemoved
```

Emitted when a tracker is removed. You should remove any [`XRController3D`](XRController3D.md) or [`XRAnchor3D`](XRAnchor3D.md) points if applicable. This is not mandatory, the nodes simply become inactive and will be made active again when a new tracker becomes available (i.e. a new controller is switched on that takes the place of the previous one).

### TrackerUpdated

```csharp
public event XRServerInstance.TrackerUpdatedEventHandler TrackerUpdated
```

Emitted when an existing tracker has been updated. This can happen if the user switches controllers.

### WorldOriginChanged

```csharp
public event Action WorldOriginChanged
```

Emitted when the world origin transform changes.

## InterfaceAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRServerInstance.InterfaceAdded`](XRServerInstance.md) event of a [`XRServerInstance`](XRServerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void InterfaceAddedEventHandler(StringName interfaceName)
```

## InterfaceRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRServerInstance.InterfaceRemoved`](XRServerInstance.md) event of a [`XRServerInstance`](XRServerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void InterfaceRemovedEventHandler(StringName interfaceName)
```

## TrackerAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRServerInstance.TrackerAdded`](XRServerInstance.md) event of a [`XRServerInstance`](XRServerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void TrackerAddedEventHandler(StringName trackerName, long type)
```

## TrackerRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRServerInstance.TrackerRemoved`](XRServerInstance.md) event of a [`XRServerInstance`](XRServerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void TrackerRemovedEventHandler(StringName trackerName, long type)
```

## TrackerUpdatedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRServerInstance.TrackerUpdated`](XRServerInstance.md) event of a [`XRServerInstance`](XRServerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void TrackerUpdatedEventHandler(StringName trackerName, long type)
```
