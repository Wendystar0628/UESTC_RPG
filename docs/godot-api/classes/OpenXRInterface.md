<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRInterface

命名空间 `Godot`；class；继承 [`XRInterface`](XRInterface.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The OpenXR interface allows Godot to interact with OpenXR runtimes and make it possible to create XR experiences and games.

Due to the needs of OpenXR this interface works slightly different than other plugin based XR interfaces. It needs to be initialized when Godot starts. You need to enable OpenXR, settings for this can be found in your games project settings under the XR heading. You do need to mark a viewport for use with XR in order for Godot to know which render result should be output to the headset.

## 构造函数

### OpenXRInterface

```csharp
public OpenXRInterface()
```

## 属性

### DisplayRefreshRate

```csharp
public float DisplayRefreshRate { get; set; }
```

引擎名：`display_refresh_rate`

The display refresh rate for the current HMD. Only functional if this feature is supported by the OpenXR runtime and after the interface has been initialized.

### FoveationDynamic

```csharp
public bool FoveationDynamic { get; set; }
```

引擎名：`foveation_dynamic`

If `true`, enables dynamic foveation adjustment. The interface must be initialized before this is accessible. If enabled, foveation will automatically be adjusted between low and `FoveationLevel`.

### FoveationLevel

```csharp
public int FoveationLevel { get; set; }
```

引擎名：`foveation_level`

The foveation level, from `0` (off) to `3` (high). The interface must be initialized before this is accessible.

### FoveationWithSubsampledImages

```csharp
public bool FoveationWithSubsampledImages { get; set; }
```

引擎名：`foveation_with_subsampled_images`

If `true`, enables subsampled images with foveation, which can provide a performance boost on Vulkan.

### RenderTargetSizeMultiplier

```csharp
public double RenderTargetSizeMultiplier { get; set; }
```

引擎名：`render_target_size_multiplier`

The render size multiplier for the current HMD. Must be set before the interface has been initialized.

### VrsMinRadius

```csharp
public float VrsMinRadius { get; set; }
```

引擎名：`vrs_min_radius`

The minimum radius around the focal point where full quality is guaranteed if VRS is used as a percentage of screen size.

**Note:** Mobile and Forward+ renderers only. Requires [`Viewport.VrsMode`](Viewport.md) to be set to [`Viewport.VrsModeEnum.XR`](Viewport.md).

### VrsStrength

```csharp
public float VrsStrength { get; set; }
```

引擎名：`vrs_strength`

The strength used to calculate the VRS density map. The greater this value, the more noticeable VRS is. This improves performance at the cost of quality.

**Note:** Mobile and Forward+ renderers only. Requires [`Viewport.VrsMode`](Viewport.md) to be set to [`Viewport.VrsModeEnum.XR`](Viewport.md).

## 方法

### GetActionSets

```csharp
public Array GetActionSets()
```

引擎名：`get_action_sets`

Returns a list of action sets registered with Godot (loaded from the action map at runtime).

### GetAvailableDisplayRefreshRates

```csharp
public Array GetAvailableDisplayRefreshRates()
```

引擎名：`get_available_display_refresh_rates`

Returns a list of display refresh rates supported by the current HMD. Only returned if this feature is supported by the OpenXR runtime and after the interface has been initialized.

### GetDisplayRefreshRate

```csharp
public float GetDisplayRefreshRate()
```

引擎名：`get_display_refresh_rate`

### GetFoveationDynamic

```csharp
public bool GetFoveationDynamic()
```

引擎名：`get_foveation_dynamic`

### GetFoveationLevel

```csharp
public int GetFoveationLevel()
```

引擎名：`get_foveation_level`

### GetFoveationWithSubsampledImages

```csharp
public bool GetFoveationWithSubsampledImages()
```

引擎名：`get_foveation_with_subsampled_images`

### GetHandJointAngularVelocity

```csharp
public Vector3 GetHandJointAngularVelocity(OpenXRInterface.Hand hand, OpenXRInterface.HandJoints joint)
```

引擎名：`get_hand_joint_angular_velocity`

If handtracking is enabled, returns the angular velocity of a joint (`joint`) of a hand (`hand`) as provided by OpenXR. This is relative to [`XROrigin3D`](XROrigin3D.md)!

### GetHandJointFlags

```csharp
public OpenXRInterface.HandJointFlags GetHandJointFlags(OpenXRInterface.Hand hand, OpenXRInterface.HandJoints joint)
```

引擎名：`get_hand_joint_flags`

If handtracking is enabled, returns flags that inform us of the validity of the tracking data.

### GetHandJointLinearVelocity

```csharp
public Vector3 GetHandJointLinearVelocity(OpenXRInterface.Hand hand, OpenXRInterface.HandJoints joint)
```

引擎名：`get_hand_joint_linear_velocity`

If handtracking is enabled, returns the linear velocity of a joint (`joint`) of a hand (`hand`) as provided by OpenXR. This is relative to [`XROrigin3D`](XROrigin3D.md) without worldscale applied!

### GetHandJointPosition

```csharp
public Vector3 GetHandJointPosition(OpenXRInterface.Hand hand, OpenXRInterface.HandJoints joint)
```

引擎名：`get_hand_joint_position`

If handtracking is enabled, returns the position of a joint (`joint`) of a hand (`hand`) as provided by OpenXR. This is relative to [`XROrigin3D`](XROrigin3D.md) without worldscale applied!

### GetHandJointRadius

```csharp
public float GetHandJointRadius(OpenXRInterface.Hand hand, OpenXRInterface.HandJoints joint)
```

引擎名：`get_hand_joint_radius`

If handtracking is enabled, returns the radius of a joint (`joint`) of a hand (`hand`) as provided by OpenXR. This is without worldscale applied!

### GetHandJointRotation

```csharp
public Quaternion GetHandJointRotation(OpenXRInterface.Hand hand, OpenXRInterface.HandJoints joint)
```

引擎名：`get_hand_joint_rotation`

If handtracking is enabled, returns the rotation of a joint (`joint`) of a hand (`hand`) as provided by OpenXR.

### GetHandTrackingSource

```csharp
public OpenXRInterface.HandTrackedSource GetHandTrackingSource(OpenXRInterface.Hand hand)
```

引擎名：`get_hand_tracking_source`

If handtracking is enabled and hand tracking source is supported, gets the source of the hand tracking data for `hand`.

### GetMotionRange

```csharp
public OpenXRInterface.HandMotionRange GetMotionRange(OpenXRInterface.Hand hand)
```

引擎名：`get_motion_range`

If handtracking is enabled and motion range is supported, gets the currently configured motion range for `hand`.

### GetRenderTargetSizeMultiplier

```csharp
public double GetRenderTargetSizeMultiplier()
```

引擎名：`get_render_target_size_multiplier`

### GetSessionState

```csharp
public OpenXRInterface.SessionState GetSessionState()
```

引擎名：`get_session_state`

Returns the current state of our OpenXR session.

### GetVrsMinRadius

```csharp
public float GetVrsMinRadius()
```

引擎名：`get_vrs_min_radius`

### GetVrsStrength

```csharp
public float GetVrsStrength()
```

引擎名：`get_vrs_strength`

### IsActionSetActive

```csharp
public bool IsActionSetActive(string name)
```

引擎名：`is_action_set_active`

Returns `true` if the given action set is active.

### IsEyeGazeInteractionSupported

```csharp
public bool IsEyeGazeInteractionSupported()
```

引擎名：`is_eye_gaze_interaction_supported`

Returns the capabilities of the eye gaze interaction extension.

**Note:** This only returns a valid value after OpenXR has been initialized.

### IsFoveationSupported

```csharp
public bool IsFoveationSupported()
```

引擎名：`is_foveation_supported`

Returns `true` if OpenXR's foveation extension is supported. The interface must be initialized before this returns a valid value.

**Note:** When using the Vulkan rendering driver, [`Viewport.VrsMode`](Viewport.md) must be set to [`Viewport.VrsModeEnum.XR`](Viewport.md) to support foveation.

### IsHandInteractionSupported

```csharp
public bool IsHandInteractionSupported()
```

引擎名：`is_hand_interaction_supported`

Returns `true` if OpenXR's hand interaction profile is supported and enabled.

**Note:** This only returns a valid value after OpenXR has been initialized.

### IsHandTrackingSupported

```csharp
public bool IsHandTrackingSupported()
```

引擎名：`is_hand_tracking_supported`

Returns `true` if OpenXR's hand tracking is supported and enabled.

**Note:** This only returns a valid value after OpenXR has been initialized.

### IsUserPresenceSupported

```csharp
public bool IsUserPresenceSupported()
```

引擎名：`is_user_presence_supported`

Returns `true` if OpenXR's user presence extension is supported and enabled.

**Note:** This only returns a valid value after OpenXR has been initialized.

### IsUserPresent

```csharp
public bool IsUserPresent()
```

引擎名：`is_user_present`

Returns `true` if system has detected the presence of a user in the XR experience.

### SetActionSetActive

```csharp
public void SetActionSetActive(string name, bool active)
```

引擎名：`set_action_set_active`

Sets the given action set as active or inactive.

### SetCpuLevel

```csharp
public void SetCpuLevel(OpenXRInterface.PerfSettingsLevel level)
```

引擎名：`set_cpu_level`

Sets the CPU performance level of the OpenXR device.

### SetDisplayRefreshRate

```csharp
public void SetDisplayRefreshRate(float refreshRate)
```

引擎名：`set_display_refresh_rate`

### SetFoveationDynamic

```csharp
public void SetFoveationDynamic(bool foveationDynamic)
```

引擎名：`set_foveation_dynamic`

### SetFoveationLevel

```csharp
public void SetFoveationLevel(int foveationLevel)
```

引擎名：`set_foveation_level`

### SetFoveationWithSubsampledImages

```csharp
public void SetFoveationWithSubsampledImages(bool enabled)
```

引擎名：`set_foveation_with_subsampled_images`

### SetGpuLevel

```csharp
public void SetGpuLevel(OpenXRInterface.PerfSettingsLevel level)
```

引擎名：`set_gpu_level`

Sets the GPU performance level of the OpenXR device.

### SetMotionRange

```csharp
public void SetMotionRange(OpenXRInterface.Hand hand, OpenXRInterface.HandMotionRange motionRange)
```

引擎名：`set_motion_range`

If handtracking is enabled and motion range is supported, sets the currently configured motion range for `hand` to `motionRange`.

### SetRenderTargetSizeMultiplier

```csharp
public void SetRenderTargetSizeMultiplier(double multiplier)
```

引擎名：`set_render_target_size_multiplier`

### SetVrsMinRadius

```csharp
public void SetVrsMinRadius(float radius)
```

引擎名：`set_vrs_min_radius`

### SetVrsStrength

```csharp
public void SetVrsStrength(float strength)
```

引擎名：`set_vrs_strength`

## 事件（信号）

### CpuLevelChanged

```csharp
public event OpenXRInterface.CpuLevelChangedEventHandler CpuLevelChanged
```

引擎名：`cpu_level_changed`

Informs the device CPU performance level has changed in the specified subdomain.

### GpuLevelChanged

```csharp
public event OpenXRInterface.GpuLevelChangedEventHandler GpuLevelChanged
```

引擎名：`gpu_level_changed`

Informs the device GPU performance level has changed in the specified subdomain.

### InstanceExiting

```csharp
public event Action InstanceExiting
```

引擎名：`instance_exiting`

Informs our OpenXR instance is exiting.

### PoseRecentered

```csharp
public event Action PoseRecentered
```

引擎名：`pose_recentered`

Informs the user queued a recenter of the player position.

### RefreshRateChanged

```csharp
public event OpenXRInterface.RefreshRateChangedEventHandler RefreshRateChanged
```

引擎名：`refresh_rate_changed`

Informs the user the HMD refresh rate has changed.

**Note:** Only emitted if XR runtime supports the refresh rate extension.

### SessionBegun

```csharp
public event Action SessionBegun
```

引擎名：`session_begun`

Informs our OpenXR session has been started.

### SessionFocussed

```csharp
public event Action SessionFocussed
```

引擎名：`session_focussed`

Informs our OpenXR session now has focus, for example output is sent to the HMD and we're receiving XR input.

### SessionLossPending

```csharp
public event Action SessionLossPending
```

引擎名：`session_loss_pending`

Informs our OpenXR session is in the process of being lost.

### SessionStopping

```csharp
public event Action SessionStopping
```

引擎名：`session_stopping`

Informs our OpenXR session is stopping.

### SessionSynchronized

```csharp
public event Action SessionSynchronized
```

引擎名：`session_synchronized`

Informs our OpenXR session has been synchronized.

### SessionVisible

```csharp
public event Action SessionVisible
```

引擎名：`session_visible`

Informs our OpenXR session is now visible, for example output is sent to the HMD but we don't receive XR input.

### UserPresenceChanged

```csharp
public event OpenXRInterface.UserPresenceChangedEventHandler UserPresenceChanged
```

引擎名：`user_presence_changed`

Signal emitted when the user presence value changes.

**Note:** This signal will not be emitted during application startup and application shutdown. Developers should assume user presence is gained on startup and lost on shutdown.

## CpuLevelChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OpenXRInterface.CpuLevelChanged`](OpenXRInterface.md) event of a [`OpenXRInterface`](OpenXRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void CpuLevelChangedEventHandler(long subDomain, long fromLevel, long toLevel)
```

## GpuLevelChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OpenXRInterface.GpuLevelChanged`](OpenXRInterface.md) event of a [`OpenXRInterface`](OpenXRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void GpuLevelChangedEventHandler(long subDomain, long fromLevel, long toLevel)
```

## Hand

命名空间 `Godot`；enum

### 枚举值

- `Left = 0`：Left hand.
- `Right = 1`：Right hand.
- `Max = 2`：Maximum value for the hand enum.

## HandJointFlags

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No flags are set.
- `OrientationValid = 1`：If set, the orientation data is valid, otherwise, the orientation data is unreliable and should not be used.
- `OrientationTracked = 2`：If set, the orientation data comes from tracking data, otherwise, the orientation data contains predicted data.
- `PositionValid = 4`：If set, the positional data is valid, otherwise, the positional data is unreliable and should not be used.
- `PositionTracked = 8`：If set, the positional data comes from tracking data, otherwise, the positional data contains predicted data.
- `LinearVelocityValid = 16`：If set, our linear velocity data is valid, otherwise, the linear velocity data is unreliable and should not be used.
- `AngularVelocityValid = 32`：If set, our angular velocity data is valid, otherwise, the angular velocity data is unreliable and should not be used.

## HandJoints

命名空间 `Godot`；enum

### 枚举值

- `Palm = 0`：Palm joint.
- `Wrist = 1`：Wrist joint.
- `ThumbMetacarpal = 2`：Thumb metacarpal joint.
- `ThumbProximal = 3`：Thumb proximal joint.
- `ThumbDistal = 4`：Thumb distal joint.
- `ThumbTip = 5`：Thumb tip joint.
- `IndexMetacarpal = 6`：Index finger metacarpal joint.
- `IndexProximal = 7`：Index finger phalanx proximal joint.
- `IndexIntermediate = 8`：Index finger phalanx intermediate joint.
- `IndexDistal = 9`：Index finger phalanx distal joint.
- `IndexTip = 10`：Index finger tip joint.
- `MiddleMetacarpal = 11`：Middle finger metacarpal joint.
- `MiddleProximal = 12`：Middle finger phalanx proximal joint.
- `MiddleIntermediate = 13`：Middle finger phalanx intermediate joint.
- `MiddleDistal = 14`：Middle finger phalanx distal joint.
- `MiddleTip = 15`：Middle finger tip joint.
- `RingMetacarpal = 16`：Ring finger metacarpal joint.
- `RingProximal = 17`：Ring finger phalanx proximal joint.
- `RingIntermediate = 18`：Ring finger phalanx intermediate joint.
- `RingDistal = 19`：Ring finger phalanx distal joint.
- `RingTip = 20`：Ring finger tip joint.
- `LittleMetacarpal = 21`：Pinky finger metacarpal joint.
- `LittleProximal = 22`：Pinky finger phalanx proximal joint.
- `LittleIntermediate = 23`：Pinky finger phalanx intermediate joint.
- `LittleDistal = 24`：Pinky finger phalanx distal joint.
- `LittleTip = 25`：Pinky finger tip joint.
- `Max = 26`：Represents the size of the `OpenXRInterface.HandJoints` enum.

## HandMotionRange

命名空间 `Godot`；enum

### 枚举值

- `Unobstructed = 0`：Full hand range, if user closes their hands, we make a full fist.
- `ConformToController = 1`：Conform to controller, if user closes their hands, the tracked data conforms to the shape of the controller.
- `Max = 2`：Maximum value for the motion range enum.

## HandTrackedSource

命名空间 `Godot`；enum

### 枚举值

- `Unknown = 0`：The source of hand tracking data is unknown (the extension is likely unsupported).
- `Unobstructed = 1`：The source of hand tracking is unobstructed, this means that an accurate method of hand tracking is used, e.g.
- `Controller = 2`：The source of hand tracking is a controller, bone positions are inferred from controller inputs.
- `Max = 3`：Represents the size of the `OpenXRInterface.HandTrackedSource` enum.

## PerfSettingsLevel

命名空间 `Godot`；enum

### 枚举值

- `PowerSavings = 0`：The application has entered a non-XR section (head-locked / static screen), during which power savings are to be prioritized.
- `SustainedLow = 1`：The application has entered a low and stable complexity section, during which reducing power is more important than occasional late rendering frames.
- `SustainedHigh = 2`：The application has entered a high or dynamic complexity section, during which the XR Runtime strives for consistent XR compositing and frame rendering within a thermally sustainable range.
- `Boost = 3`：The application has entered a section with very high complexity, during which the XR Runtime is allowed to step up beyond the thermally sustainable range.

## PerfSettingsNotificationLevel

命名空间 `Godot`；enum

### 枚举值

- `Normal = 0`：The sub-domain has reached a level where no further actions other than currently applied are necessary.
- `Warning = 1`：The sub-domain has reached an early warning level where the application should start proactive mitigation actions.
- `Impaired = 2`：The sub-domain has reached a critical level where the application should start drastic mitigation actions.

## PerfSettingsSubDomain

命名空间 `Godot`；enum

### 枚举值

- `Compositing = 0`：The compositing performance within the runtime has reached a new level.
- `Rendering = 1`：The application rendering performance has reached a new level.
- `Thermal = 2`：The temperature of the device has reached a new level.

## RefreshRateChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OpenXRInterface.RefreshRateChanged`](OpenXRInterface.md) event of a [`OpenXRInterface`](OpenXRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void RefreshRateChangedEventHandler(double refreshRate)
```

## SessionState

命名空间 `Godot`；enum

### 枚举值

- `Unknown = 0`：The state of the session is unknown, we haven't tried setting up OpenXR yet.
- `Idle = 1`：The initial state after the OpenXR session is created or after the session is destroyed.
- `Ready = 2`：OpenXR is ready to begin our session.
- `Synchronized = 3`：The application has synched its frame loop with the runtime but we're not rendering anything.
- `Visible = 4`：The application has synched its frame loop with the runtime and we're rendering output to the user, however we receive no user input.
- `Focused = 5`：The application has synched its frame loop with the runtime, we're rendering output to the user and we're receiving XR input.
- `Stopping = 6`：Our session is being stopped.
- `LossPending = 7`：The session is about to be lost.
- `Exiting = 8`：The OpenXR instance is about to be destroyed and we're exiting.

## UserPresenceChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OpenXRInterface.UserPresenceChanged`](OpenXRInterface.md) event of a [`OpenXRInterface`](OpenXRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void UserPresenceChangedEventHandler(bool isUserPresent)
```
