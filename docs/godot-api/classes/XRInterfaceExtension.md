<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRInterfaceExtension

命名空间 `Godot`；class；继承 [`XRInterface`](XRInterface.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

External XR interface plugins should inherit from this class.

## 构造函数

### XRInterfaceExtension

```csharp
public XRInterfaceExtension()
```

## 方法

### AddBlit

```csharp
public void AddBlit(Rid renderTarget, Rect2 srcRect, Rect2I dstRect, bool useLayer, uint layer, bool applyLensDistortion, Vector2 eyeCenter, double k1, double k2, double upscale, double aspectRatio)
```

引擎名：`add_blit`

Blits our render results to screen optionally applying lens distortion. This can only be called while processing `_commit_views`.

### GetColorTexture

```csharp
public Rid GetColorTexture()
```

引擎名：`get_color_texture`

### GetDepthTexture

```csharp
public Rid GetDepthTexture()
```

引擎名：`get_depth_texture`

### GetRenderTargetTexture

```csharp
public Rid GetRenderTargetTexture(Rid renderTarget)
```

引擎名：`get_render_target_texture`

Returns a valid [`Rid`](Rid.md) for a texture to which we should render the current frame if supported by the interface.

### GetVelocityTexture

```csharp
public Rid GetVelocityTexture()
```

引擎名：`get_velocity_texture`

### _EndFrame

```csharp
public virtual void _EndFrame()
```

引擎名：`_end_frame`

Called if interface is active and queues have been submitted.

### _GetAnchorDetectionIsEnabled

```csharp
public virtual bool _GetAnchorDetectionIsEnabled()
```

引擎名：`_get_anchor_detection_is_enabled`

Return `true` if anchor detection is enabled for this interface.

### _GetCameraFeedId

```csharp
public virtual int _GetCameraFeedId()
```

引擎名：`_get_camera_feed_id`

Returns the camera feed ID for the [`CameraFeed`](CameraFeed.md) registered with the [`CameraServer`](CameraServer.md) that should be presented as the background on an AR capable device (if applicable).

### _GetCameraTransform

```csharp
public virtual Transform3D _GetCameraTransform()
```

引擎名：`_get_camera_transform`

Returns the [`Transform3D`](Transform3D.md) that positions the [`XRCamera3D`](XRCamera3D.md) in the world.

### _GetCapabilities

```csharp
public virtual uint _GetCapabilities()
```

引擎名：`_get_capabilities`

Returns the capabilities of this interface.

### _GetColorTexture

```csharp
public virtual Rid _GetColorTexture()
```

引擎名：`_get_color_texture`

Return color texture into which to render (if applicable).

### _GetDepthTexture

```csharp
public virtual Rid _GetDepthTexture()
```

引擎名：`_get_depth_texture`

Return depth texture into which to render (if applicable).

### _GetName

```csharp
public virtual StringName _GetName()
```

引擎名：`_get_name`

Returns the name of this interface.

### _GetPlayArea

```csharp
public virtual Vector3[] _GetPlayArea()
```

引擎名：`_get_play_area`

Returns a [`Vector3`](Vector3.md)[] that represents the play areas boundaries (if applicable).

### _GetPlayAreaMode

```csharp
public virtual XRInterface.PlayAreaMode _GetPlayAreaMode()
```

引擎名：`_get_play_area_mode`

Returns the play area mode that sets up our play area.

### _GetProjectionForView

```csharp
public virtual double[] _GetProjectionForView(uint view, double aspect, double zNear, double zFar)
```

引擎名：`_get_projection_for_view`

Returns the projection matrix for the given view as a `System.Double`[].

### _GetRenderTargetSize

```csharp
public virtual Vector2 _GetRenderTargetSize()
```

引擎名：`_get_render_target_size`

Returns the size of our render target for this interface, this overrides the size of the [`Viewport`](Viewport.md) marked as the xr viewport.

### _GetSuggestedPoseNames

```csharp
public virtual string[] _GetSuggestedPoseNames(StringName trackerName)
```

引擎名：`_get_suggested_pose_names`

Returns a `System.String`[] with pose names configured by this interface. Note that user configuration can override this list.

### _GetSuggestedTrackerNames

```csharp
public virtual string[] _GetSuggestedTrackerNames()
```

引擎名：`_get_suggested_tracker_names`

Returns a `System.String`[] with tracker names configured by this interface. Note that user configuration can override this list.

### _GetSystemInfo

```csharp
public virtual Dictionary _GetSystemInfo()
```

引擎名：`_get_system_info`

Returns a [`Collections.Dictionary`](Collections.Dictionary.md) with system information related to this interface.

### _GetTrackingStatus

```csharp
public virtual XRInterface.TrackingStatus _GetTrackingStatus()
```

引擎名：`_get_tracking_status`

Returns the current status of our tracking.

### _GetTransformForView

```csharp
public virtual Transform3D _GetTransformForView(uint view, Transform3D camTransform)
```

引擎名：`_get_transform_for_view`

Returns a [`Transform3D`](Transform3D.md) for a given view.

### _GetVelocityTexture

```csharp
public virtual Rid _GetVelocityTexture()
```

引擎名：`_get_velocity_texture`

Return velocity texture into which to render (if applicable).

### _GetViewCount

```csharp
public virtual uint _GetViewCount()
```

引擎名：`_get_view_count`

Returns the number of views this interface requires, 1 for mono, 2 for stereoscopic.

### _GetVrsTexture

```csharp
public virtual Rid _GetVrsTexture()
```

引擎名：`_get_vrs_texture`

### _GetVrsTextureFormat

```csharp
public virtual XRInterface.VrsTextureFormat _GetVrsTextureFormat()
```

引擎名：`_get_vrs_texture_format`

Returns the format of the texture returned by `_GetVrsTexture()`.

### _Initialize

```csharp
public virtual bool _Initialize()
```

引擎名：`_initialize`

Initializes the interface, returns `true` on success.

### _IsInitialized

```csharp
public virtual bool _IsInitialized()
```

引擎名：`_is_initialized`

Returns `true` if this interface has been initialized.

### _PostDrawViewport

```csharp
public virtual void _PostDrawViewport(Rid renderTarget, Rect2 screenRect)
```

引擎名：`_post_draw_viewport`

Called after the XR [`Viewport`](Viewport.md) draw logic has completed.

### _PreDrawViewport

```csharp
public virtual bool _PreDrawViewport(Rid renderTarget)
```

引擎名：`_pre_draw_viewport`

Called if this is our primary `XRInterfaceExtension` before we start processing a [`Viewport`](Viewport.md) for every active XR [`Viewport`](Viewport.md), returns `true` if that viewport should be rendered. An XR interface may return `false` if the user has taken off their headset and we can pause rendering.

### _PreRender

```csharp
public virtual void _PreRender()
```

引擎名：`_pre_render`

Called if this `XRInterfaceExtension` is active before rendering starts. Most XR interfaces will sync tracking at this point in time.

### _Process

```csharp
public virtual void _Process()
```

引擎名：`_process`

Called if this `XRInterfaceExtension` is active before our physics and game process is called. Most XR interfaces will update its [`XRPositionalTracker`](XRPositionalTracker.md)s at this point in time.

### _SetAnchorDetectionIsEnabled

```csharp
public virtual void _SetAnchorDetectionIsEnabled(bool enabled)
```

引擎名：`_set_anchor_detection_is_enabled`

Enables anchor detection on this interface if supported.

### _SetPlayAreaMode

```csharp
public virtual bool _SetPlayAreaMode(XRInterface.PlayAreaMode mode)
```

引擎名：`_set_play_area_mode`

Set the play area mode for this interface.

### _SupportsPlayAreaMode

```csharp
public virtual bool _SupportsPlayAreaMode(XRInterface.PlayAreaMode mode)
```

引擎名：`_supports_play_area_mode`

Returns `true` if this interface supports this play area mode.

### _TriggerHapticPulse

```csharp
public virtual void _TriggerHapticPulse(string actionName, StringName trackerName, double frequency, double amplitude, double durationSec, double delaySec)
```

引擎名：`_trigger_haptic_pulse`

Triggers a haptic pulse to be emitted on the specified tracker.

### _Uninitialize

```csharp
public virtual void _Uninitialize()
```

引擎名：`_uninitialize`

Uninitialize the interface.
