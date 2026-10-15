<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRInterface

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class needs to be implemented to make an AR or VR platform available to Godot and these should be implemented as C++ modules or GDExtension modules. Part of the interface is exposed to GDScript so you can detect, enable and configure an AR or VR platform.

Interfaces should be written in such a way that simply enabling them will give us a working setup. You can query the available interfaces through [`XRServer`](XRServer.md).

## 属性

### ArIsAnchorDetectionEnabled

```csharp
public bool ArIsAnchorDetectionEnabled { get; set; }
```

引擎名：`ar_is_anchor_detection_enabled`

On an AR interface, `true` if anchor detection is enabled.

### EnvironmentBlendMode

```csharp
public XRInterface.EnvironmentBlendModeEnum EnvironmentBlendMode { get; set; }
```

引擎名：`environment_blend_mode`

Specify how XR should blend in the environment. This is specific to certain AR and passthrough devices where camera images are blended in by the XR compositor.

### InterfaceIsPrimary

```csharp
public bool InterfaceIsPrimary { get; set; }
```

引擎名：`interface_is_primary`

`true` if this is the primary interface.

### XRPlayAreaMode

```csharp
public XRInterface.PlayAreaMode XRPlayAreaMode { get; set; }
```

引擎名：`xr_play_area_mode`

The play area mode for this interface.

## 方法

### GetAnchorDetectionIsEnabled

```csharp
public bool GetAnchorDetectionIsEnabled()
```

引擎名：`get_anchor_detection_is_enabled`

### GetCameraFeedId

```csharp
public int GetCameraFeedId()
```

引擎名：`get_camera_feed_id`

If this is an AR interface that requires displaying a camera feed as the background, this method returns the feed ID in the [`CameraServer`](CameraServer.md) for this interface.

### GetCapabilities

```csharp
public uint GetCapabilities()
```

引擎名：`get_capabilities`

Returns a combination of [`XRInterface.Capabilities`](XRInterface.md) flags providing information about the capabilities of this interface.

### GetEnvironmentBlendMode

```csharp
public XRInterface.EnvironmentBlendModeEnum GetEnvironmentBlendMode()
```

引擎名：`get_environment_blend_mode`

### GetName

```csharp
public StringName GetName()
```

引擎名：`get_name`

Returns the name of this interface (`"OpenXR"`, `"OpenVR"`, `"OpenHMD"`, `"ARKit"`, etc.).

### GetPlayArea

```csharp
public Vector3[] GetPlayArea()
```

引擎名：`get_play_area`

Returns an array of vectors that represent the physical play area mapped to the virtual space around the [`XROrigin3D`](XROrigin3D.md) point. The points form a convex polygon that can be used to react to or visualize the play area. This returns an empty array if this feature is not supported or if the information is not yet available.

### GetPlayAreaMode

```csharp
public XRInterface.PlayAreaMode GetPlayAreaMode()
```

引擎名：`get_play_area_mode`

### GetProjectionForView

```csharp
public Projection GetProjectionForView(uint view, double aspect, double near, double far)
```

引擎名：`get_projection_for_view`

Returns the projection matrix for a view/eye.

### GetRenderTargetSize

```csharp
public Vector2 GetRenderTargetSize()
```

引擎名：`get_render_target_size`

Returns the resolution at which we should render our intermediate results before things like lens distortion are applied by the VR platform.

### GetSupportedEnvironmentBlendModes

```csharp
public Array GetSupportedEnvironmentBlendModes()
```

引擎名：`get_supported_environment_blend_modes`

Returns the an array of supported environment blend modes, see [`XRInterface.EnvironmentBlendModeEnum`](XRInterface.md).

### GetSystemInfo

```csharp
public Dictionary GetSystemInfo()
```

引擎名：`get_system_info`

Returns a [`Collections.Dictionary`](Collections.Dictionary.md) with extra system info. Interfaces are expected to return `XRRuntimeName` and `XRRuntimeVersion` providing info about the used XR runtime. Additional entries may be provided specific to an interface.

**Note:**This information may only be available after `Initialize()` was successfully called.

### GetTrackingStatus

```csharp
public XRInterface.TrackingStatus GetTrackingStatus()
```

引擎名：`get_tracking_status`

If supported, returns the status of our tracking. This will allow you to provide feedback to the user whether there are issues with positional tracking.

### GetTransformForView

```csharp
public Transform3D GetTransformForView(uint view, Transform3D camTransform)
```

引擎名：`get_transform_for_view`

Returns the transform for a view/eye.

`view` is the view/eye index.

`camTransform` is the transform that maps device coordinates to scene coordinates, typically the [`Node3D.GlobalTransform`](Node3D.md) of the current XROrigin3D.

### GetViewCount

```csharp
public uint GetViewCount()
```

引擎名：`get_view_count`

Returns the number of views that need to be rendered for this device. 1 for Monoscopic, 2 for Stereoscopic.

### Initialize

```csharp
public bool Initialize()
```

引擎名：`initialize`

Call this to initialize this interface. The first interface that is initialized is identified as the primary interface and it will be used for rendering output.

After initializing the interface you want to use you then need to enable the AR/VR mode of a viewport and rendering should commence.

**Note:** You must enable the XR mode on the main viewport for any device that uses the main output of Godot, such as for mobile VR.

If you do this for a platform that handles its own output (such as OpenVR) Godot will show just one eye without distortion on screen. Alternatively, you can add a separate viewport node to your scene and enable AR/VR on that viewport. It will be used to output to the HMD, leaving you free to do anything you like in the main window, such as using a separate camera as a spectator camera or rendering something completely different.

While currently not used, you can activate additional interfaces. You may wish to do this if you want to track controllers from other platforms. However, at this point in time only one interface can render to an HMD.

### IsInitialized

```csharp
public bool IsInitialized()
```

引擎名：`is_initialized`

Returns `true` if this interface has been initialized.

### IsPassthroughEnabled

```csharp
public bool IsPassthroughEnabled()
```

引擎名：`is_passthrough_enabled`

Returns `true` if passthrough is enabled.

### IsPassthroughSupported

```csharp
public bool IsPassthroughSupported()
```

引擎名：`is_passthrough_supported`

Returns `true` if this interface supports passthrough.

### IsPrimary

```csharp
public bool IsPrimary()
```

引擎名：`is_primary`

### SetAnchorDetectionIsEnabled

```csharp
public void SetAnchorDetectionIsEnabled(bool enable)
```

引擎名：`set_anchor_detection_is_enabled`

### SetEnvironmentBlendMode

```csharp
public bool SetEnvironmentBlendMode(XRInterface.EnvironmentBlendModeEnum mode)
```

引擎名：`set_environment_blend_mode`

Sets the active environment blend mode.

`mode` is the environment blend mode starting with the next frame.

**Note:** Not all runtimes support all environment blend modes, so it is important to check this at startup. For example:

```csharp
func _ready():
var xr_interface = XRServer.find_interface("OpenXR")
if xr_interface and xr_interface.is_initialized():
var vp = get_viewport()
vp.use_xr = true
var acceptable_modes = [XRInterface.XR_ENV_BLEND_MODE_OPAQUE, XRInterface.XR_ENV_BLEND_MODE_ADDITIVE]
var modes = xr_interface.get_supported_environment_blend_modes()
for mode in acceptable_modes:
if mode in modes:
xr_interface.set_environment_blend_mode(mode)
break

```

### SetPlayAreaMode

```csharp
public bool SetPlayAreaMode(XRInterface.PlayAreaMode mode)
```

引擎名：`set_play_area_mode`

Sets the active play area mode, will return `false` if the mode can't be used with this interface.

**Note:** Changing this after the interface has already been initialized can be jarring for the player, so it's recommended to recenter on the HMD with [`XRServer.CenterOnHmd`](XRServer.md) (if switching to [`XRInterface.PlayAreaMode.Stage`](XRInterface.md)) or make the switch during a scene change.

### SetPrimary

```csharp
public void SetPrimary(bool primary)
```

引擎名：`set_primary`

### StartPassthrough

```csharp
public bool StartPassthrough()
```

引擎名：`start_passthrough`

Starts passthrough, will return `false` if passthrough couldn't be started.

**Note:** The viewport used for XR must have a transparent background, otherwise passthrough may not properly render.

### StopPassthrough

```csharp
public void StopPassthrough()
```

引擎名：`stop_passthrough`

Stops passthrough.

### SupportsPlayAreaMode

```csharp
public bool SupportsPlayAreaMode(XRInterface.PlayAreaMode mode)
```

引擎名：`supports_play_area_mode`

Call this to find out if a given play area mode is supported by this interface.

### TriggerHapticPulse

```csharp
public void TriggerHapticPulse(string actionName, StringName trackerName, double frequency, double amplitude, double durationSec, double delaySec)
```

引擎名：`trigger_haptic_pulse`

Triggers a haptic pulse on a device associated with this interface.

`actionName` is the name of the action for this pulse.

`trackerName` is optional and can be used to direct the pulse to a specific device provided that device is bound to this haptic.

`frequency` is the frequency of the pulse, set to `0.0` to have the system use a default frequency.

`amplitude` is the amplitude of the pulse between `0.0` and `1.0`.

`durationSec` is the duration of the pulse in seconds.

`delaySec` is a delay in seconds before the pulse is given.

### Uninitialize

```csharp
public void Uninitialize()
```

引擎名：`uninitialize`

Turns the interface off.

## 事件（信号）

### PlayAreaChanged

```csharp
public event XRInterface.PlayAreaChangedEventHandler PlayAreaChanged
```

引擎名：`play_area_changed`

Emitted when the play area is changed. This can be a result of the player resetting the boundary or entering a new play area, the player changing the play area mode, the world scale changing or the player resetting their headset orientation.

## Capabilities

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No XR capabilities.
- `Mono = 1`：This interface can work with normal rendering output (non-HMD based AR).
- `Stereo = 2`：This interface supports stereoscopic rendering.
- `Quad = 4`：This interface supports quad rendering (not yet supported by Godot).
- `Vr = 8`：This interface supports VR.
- `Ar = 16`：This interface supports AR (video background and real world tracking).
- `External = 32`：This interface outputs to an external device.

## EnvironmentBlendModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Opaque = 0`：Opaque blend mode.
- `Additive = 1`：Additive blend mode.
- `AlphaBlend = 2`：Alpha blend mode.

## PlayAreaChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRInterface.PlayAreaChanged`](XRInterface.md) event of a [`XRInterface`](XRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void PlayAreaChangedEventHandler(long mode)
```

## PlayAreaMode

命名空间 `Godot`；enum

### 枚举值

- `Unknown = 0`：Play area mode not set or not available.
- `Area3Dof = 1`：Play area only supports orientation tracking, no positional tracking, area will center around player.
- `Sitting = 2`：Player is in seated position, limited positional tracking, fixed guardian around player.
- `Roomscale = 3`：Player is free to move around, full positional tracking.
- `Stage = 4`：Same as `Roomscale` but origin point is fixed to the center of the physical space.
- `Custom = 2147483647`：Custom play area set by a GDExtension.

## TrackingStatus

命名空间 `Godot`；enum

### 枚举值

- `NormalTracking = 0`：Tracking is behaving as expected.
- `ExcessiveMotion = 1`：Tracking is hindered by excessive motion (the player is moving faster than tracking can keep up).
- `InsufficientFeatures = 2`：Tracking is hindered by insufficient features, it's too dark (for camera-based tracking), player is blocked, etc.
- `UnknownTracking = 3`：We don't know the status of the tracking or this interface does not provide feedback.
- `NotTracking = 4`：Tracking is not functional (camera not plugged in or obscured, lighthouses turned off, etc.).

## VrsTextureFormat

命名空间 `Godot`；enum

### 枚举值

- `Unified = 0`：The texture format is the same as returned by [`Xrvrs.MakeVrsTexture`](Xrvrs.md).
- `FragmentShadingRate = 1`：The texture format is the same as expected by the Vulkan `VK_KHR_fragment_shading_rate` extension.
- `FragmentDensityMap = 2`：The texture format is the same as expected by the Vulkan `VK_EXT_fragment_density_map` extension.
