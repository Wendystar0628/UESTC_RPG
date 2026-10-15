<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# WebXRInterface

命名空间 `Godot`；class；继承 [`XRInterface`](XRInterface.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

WebXR is an open standard that allows creating VR and AR applications that run in the web browser.

As such, this interface is only available when running in Web exports.

WebXR supports a wide range of devices, from the very capable (like Valve Index, HTC Vive, Oculus Rift and Quest) down to the much less capable (like Google Cardboard, Oculus Go, GearVR, or plain smartphones).

Since WebXR is based on JavaScript, it makes extensive use of callbacks, which means that `WebXRInterface` is forced to use signals, where other XR interfaces would instead use functions that return a result immediately. This makes `WebXRInterface` quite a bit more complicated to initialize than other XR interfaces.

Here's the minimum code required to start an immersive VR session:

```csharp
extends Node3D

var webxr_interface
var vr_supported = false

func _ready():
# We assume this node has a button as a child.
# This button is for the user to consent to entering immersive VR mode.
$Button.pressed.connect(self._on_button_pressed)

webxr_interface = XRServer.find_interface("WebXR")
if webxr_interface:
# WebXR uses a lot of asynchronous callbacks, so we connect to various
# signals in order to receive them.
webxr_interface.session_supported.connect(self._webxr_session_supported)
webxr_interface.session_started.connect(self._webxr_session_started)
webxr_interface.session_ended.connect(self._webxr_session_ended)
webxr_interface.session_failed.connect(self._webxr_session_failed)

# This returns immediately - our _webxr_session_supported() method
# (which we connected to the "session_supported" signal above) will
# be called sometime later to let us know if it's supported or not.
webxr_interface.is_session_supported("immersive-vr")

func _webxr_session_supported(session_mode, supported):
if session_mode == 'immersive-vr':
vr_supported = supported

func _on_button_pressed():
if not vr_supported:
OS.alert("Your browser doesn't support VR")
return

# We want an immersive VR session, as opposed to AR ('immersive-ar') or a
# simple 3DoF viewer ('viewer').
webxr_interface.session_mode = 'immersive-vr'
# 'bounded-floor' is room scale, 'local-floor' is a standing or sitting
# experience (it puts you 1.6m above the ground if you have 3DoF headset),
# whereas as 'local' puts you down at the XROrigin.
# This list means it'll first try to request 'bounded-floor', then
# fallback on 'local-floor' and ultimately 'local', if nothing else is
# supported.
webxr_interface.requested_reference_space_types = 'bounded-floor, local-floor, local'
# In order to use 'local-floor' or 'bounded-floor' we must also
# mark the features as required or optional. By including 'hand-tracking'
# as an optional feature, it will be enabled if supported.
webxr_interface.required_features = 'local-floor'
webxr_interface.optional_features = 'bounded-floor, hand-tracking'

# This will return false if we're unable to even request the session,
# however, it can still fail asynchronously later in the process, so we
# only know if it's really succeeded or failed when our
# _webxr_session_started() or _webxr_session_failed() methods are called.
if not webxr_interface.initialize():
OS.alert("Failed to initialize")
return

func _webxr_session_started():
$Button.visible = false
# This tells Godot to start rendering to the headset.
get_viewport().use_xr = true
# This will be the reference space type you ultimately got, out of the
# types that you requested above. This is useful if you want the game to
# work a little differently in 'bounded-floor' versus 'local-floor'.
print("Reference space type: ", webxr_interface.reference_space_type)
# This will be the list of features that were successfully enabled
# (except on browsers that don't support this property).
print("Enabled features: ", webxr_interface.enabled_features)

func _webxr_session_ended():
$Button.visible = true
# If the user exits immersive mode, then we tell Godot to render to the web
# page again.
get_viewport().use_xr = false

func _webxr_session_failed(message):
OS.alert("Failed to initialize: " + message)

```

There are a couple ways to handle "controller" input:

- Using [`XRController3D`](XRController3D.md) nodes and their [`XRController3D.ButtonPressed`](XRController3D.md) and [`XRController3D.ButtonReleased`](XRController3D.md) signals. This is how controllers are typically handled in XR apps in Godot, however, this will only work with advanced VR controllers like the Oculus Touch or Index controllers, for example.

- Using the `Select`, `Squeeze` and related signals. This method will work for both advanced VR controllers, and non-traditional input sources like a tap on the screen, a spoken voice command or a button press on the device itself.

You can use both methods to allow your game or app to support a wider or narrower set of devices and input methods, or to allow more advanced interactions with more advanced devices.

## 属性

### EnabledFeatures

```csharp
public string EnabledFeatures { get; }
```

引擎名：`enabled_features`

A comma-separated list of features that were successfully enabled by [`XRInterface.Initialize`](XRInterface.md) when setting up the WebXR session.

This may include features requested by setting `RequiredFeatures` and `OptionalFeatures`, and will only be available after `SessionStarted` has been emitted.

**Note:** This may not be support by all web browsers, in which case it will be an empty string.

### OptionalFeatures

```csharp
public string OptionalFeatures { get; set; }
```

引擎名：`optional_features`

A comma-seperated list of optional features used by [`XRInterface.Initialize`](XRInterface.md) when setting up the WebXR session.

If a user's browser or device doesn't support one of the given features, initialization will continue, but you won't be able to use the requested feature.

This doesn't have any effect on the interface when already initialized.

See the MDN documentation on [WebXR's session features](https://developer.mozilla.org/en-US/docs/Web/API/XRSystem/requestSession#session_features) for a list of possible values.

### ReferenceSpaceType

```csharp
public string ReferenceSpaceType { get; }
```

引擎名：`reference_space_type`

The reference space type (from the list of requested types set in the `RequestedReferenceSpaceTypes` property), that was ultimately used by [`XRInterface.Initialize`](XRInterface.md) when setting up the WebXR session.

Possible values come from [WebXR's XRReferenceSpaceType](https://developer.mozilla.org/en-US/docs/Web/API/XRReferenceSpaceType). If you want to use a particular reference space type, it must be listed in either `RequiredFeatures` or `OptionalFeatures`.

### RequestedReferenceSpaceTypes

```csharp
public string RequestedReferenceSpaceTypes { get; set; }
```

引擎名：`requested_reference_space_types`

A comma-seperated list of reference space types used by [`XRInterface.Initialize`](XRInterface.md) when setting up the WebXR session.

The reference space types are requested in order, and the first one supported by the user's device or browser will be used. The `ReferenceSpaceType` property contains the reference space type that was ultimately selected.

This doesn't have any effect on the interface when already initialized.

Possible values come from [WebXR's XRReferenceSpaceType](https://developer.mozilla.org/en-US/docs/Web/API/XRReferenceSpaceType). If you want to use a particular reference space type, it must be listed in either `RequiredFeatures` or `OptionalFeatures`.

### RequiredFeatures

```csharp
public string RequiredFeatures { get; set; }
```

引擎名：`required_features`

A comma-seperated list of required features used by [`XRInterface.Initialize`](XRInterface.md) when setting up the WebXR session.

If a user's browser or device doesn't support one of the given features, initialization will fail and `SessionFailed` will be emitted.

This doesn't have any effect on the interface when already initialized.

See the MDN documentation on [WebXR's session features](https://developer.mozilla.org/en-US/docs/Web/API/XRSystem/requestSession#session_features) for a list of possible values.

### SessionMode

```csharp
public string SessionMode { get; set; }
```

引擎名：`session_mode`

The session mode used by [`XRInterface.Initialize`](XRInterface.md) when setting up the WebXR session.

This doesn't have any effect on the interface when already initialized.

Possible values come from [WebXR's XRSessionMode](https://developer.mozilla.org/en-US/docs/Web/API/XRSessionMode), including: `"immersive-vr"`, `"immersive-ar"`, and `"inline"`.

### VisibilityState

```csharp
public string VisibilityState { get; }
```

引擎名：`visibility_state`

Indicates if the WebXR session's imagery is visible to the user.

Possible values come from [WebXR's XRVisibilityState](https://developer.mozilla.org/en-US/docs/Web/API/XRVisibilityState), including `"hidden"`, `"visible"`, and `"visible-blurred"`.

## 方法

### GetAvailableDisplayRefreshRates

```csharp
public Array GetAvailableDisplayRefreshRates()
```

引擎名：`get_available_display_refresh_rates`

Returns display refresh rates supported by the current HMD. Only returned if this feature is supported by the web browser and after the interface has been initialized.

### GetDisplayRefreshRate

```csharp
public float GetDisplayRefreshRate()
```

引擎名：`get_display_refresh_rate`

Returns the display refresh rate for the current HMD. Not supported on all HMDs and browsers. It may not report an accurate value until after using `SetDisplayRefreshRate()`.

### GetEnabledFeatures

```csharp
public string GetEnabledFeatures()
```

引擎名：`get_enabled_features`

### GetInputSourceTargetRayMode

```csharp
public WebXRInterface.TargetRayMode GetInputSourceTargetRayMode(int inputSourceId)
```

引擎名：`get_input_source_target_ray_mode`

Returns the target ray mode for the given `inputSourceId`.

This can help interpret the input coming from that input source. See [XRInputSource.targetRayMode](https://developer.mozilla.org/en-US/docs/Web/API/XRInputSource/targetRayMode) for more information.

### GetInputSourceTracker

```csharp
public XRControllerTracker GetInputSourceTracker(int inputSourceId)
```

引擎名：`get_input_source_tracker`

Gets an [`XRControllerTracker`](XRControllerTracker.md) for the given `inputSourceId`.

In the context of WebXR, an input source can be an advanced VR controller like the Oculus Touch or Index controllers, or even a tap on the screen, a spoken voice command or a button press on the device itself. When a non-traditional input source is used, interpret the position and orientation of the [`XRPositionalTracker`](XRPositionalTracker.md) as a ray pointing at the object the user wishes to interact with.

Use this method to get information about the input source that triggered one of these signals:

- `Selectstart`

- `Select`

- `Selectend`

- `Squeezestart`

- `Squeeze`

- `Squeezestart`

### GetOptionalFeatures

```csharp
public string GetOptionalFeatures()
```

引擎名：`get_optional_features`

### GetReferenceSpaceType

```csharp
public string GetReferenceSpaceType()
```

引擎名：`get_reference_space_type`

### GetRequestedReferenceSpaceTypes

```csharp
public string GetRequestedReferenceSpaceTypes()
```

引擎名：`get_requested_reference_space_types`

### GetRequiredFeatures

```csharp
public string GetRequiredFeatures()
```

引擎名：`get_required_features`

### GetSessionMode

```csharp
public string GetSessionMode()
```

引擎名：`get_session_mode`

### GetVisibilityState

```csharp
public string GetVisibilityState()
```

引擎名：`get_visibility_state`

### IsInputSourceActive

```csharp
public bool IsInputSourceActive(int inputSourceId)
```

引擎名：`is_input_source_active`

Returns `true` if there is an active input source with the given `inputSourceId`.

### IsSessionSupported

```csharp
public void IsSessionSupported(string sessionMode)
```

引擎名：`is_session_supported`

Checks if the given `sessionMode` is supported by the user's browser.

Possible values come from [WebXR's XRSessionMode](https://developer.mozilla.org/en-US/docs/Web/API/XRSessionMode), including: `"immersive-vr"`, `"immersive-ar"`, and `"inline"`.

This method returns nothing, instead it emits the `SessionSupported` signal with the result.

### SetDisplayRefreshRate

```csharp
public void SetDisplayRefreshRate(float refreshRate)
```

引擎名：`set_display_refresh_rate`

Sets the display refresh rate for the current HMD. Not supported on all HMDs and browsers. It won't take effect right away until after `DisplayRefreshRateChanged` is emitted.

### SetOptionalFeatures

```csharp
public void SetOptionalFeatures(string optionalFeatures)
```

引擎名：`set_optional_features`

### SetRequestedReferenceSpaceTypes

```csharp
public void SetRequestedReferenceSpaceTypes(string requestedReferenceSpaceTypes)
```

引擎名：`set_requested_reference_space_types`

### SetRequiredFeatures

```csharp
public void SetRequiredFeatures(string requiredFeatures)
```

引擎名：`set_required_features`

### SetSessionMode

```csharp
public void SetSessionMode(string sessionMode)
```

引擎名：`set_session_mode`

## 事件（信号）

### DisplayRefreshRateChanged

```csharp
public event Action DisplayRefreshRateChanged
```

引擎名：`display_refresh_rate_changed`

Emitted after the display's refresh rate has changed.

### ReferenceSpaceReset

```csharp
public event Action ReferenceSpaceReset
```

引擎名：`reference_space_reset`

Emitted to indicate that the reference space has been reset or reconfigured.

When (or whether) this is emitted depends on the user's browser or device, but may include when the user has changed the dimensions of their play space (which you may be able to access via [`XRInterface.GetPlayArea`](XRInterface.md)) or pressed/held a button to recenter their position.

See [WebXR's XRReferenceSpace reset event](https://developer.mozilla.org/en-US/docs/Web/API/XRReferenceSpace/reset_event) for more information.

### Select

```csharp
public event WebXRInterface.SelectEventHandler Select
```

引擎名：`select`

Emitted after one of the input sources has finished its "primary action".

Use `GetInputSourceTracker()` and `GetInputSourceTargetRayMode()` to get more information about the input source.

### Selectend

```csharp
public event WebXRInterface.SelectendEventHandler Selectend
```

引擎名：`selectend`

Emitted when one of the input sources has finished its "primary action".

Use `GetInputSourceTracker()` and `GetInputSourceTargetRayMode()` to get more information about the input source.

### Selectstart

```csharp
public event WebXRInterface.SelectstartEventHandler Selectstart
```

引擎名：`selectstart`

Emitted when one of the input source has started its "primary action".

Use `GetInputSourceTracker()` and `GetInputSourceTargetRayMode()` to get more information about the input source.

### SessionEnded

```csharp
public event Action SessionEnded
```

引擎名：`session_ended`

Emitted when the user ends the WebXR session (which can be done using UI from the browser or device).

At this point, you should do `get_viewport().use_xr = false` to instruct Godot to resume rendering to the screen.

### SessionFailed

```csharp
public event WebXRInterface.SessionFailedEventHandler SessionFailed
```

引擎名：`session_failed`

Emitted by [`XRInterface.Initialize`](XRInterface.md) if the session fails to start.

`message` may optionally contain an error message from WebXR, or an empty string if no message is available.

### SessionStarted

```csharp
public event Action SessionStarted
```

引擎名：`session_started`

Emitted by [`XRInterface.Initialize`](XRInterface.md) if the session is successfully started.

At this point, it's safe to do `get_viewport().use_xr = true` to instruct Godot to start rendering to the XR device.

### SessionSupported

```csharp
public event WebXRInterface.SessionSupportedEventHandler SessionSupported
```

引擎名：`session_supported`

Emitted by `IsSessionSupported()` to indicate if the given `sessionMode` is supported or not.

### Squeeze

```csharp
public event WebXRInterface.SqueezeEventHandler Squeeze
```

引擎名：`squeeze`

Emitted after one of the input sources has finished its "primary squeeze action".

Use `GetInputSourceTracker()` and `GetInputSourceTargetRayMode()` to get more information about the input source.

### Squeezeend

```csharp
public event WebXRInterface.SqueezeendEventHandler Squeezeend
```

引擎名：`squeezeend`

Emitted when one of the input sources has finished its "primary squeeze action".

Use `GetInputSourceTracker()` and `GetInputSourceTargetRayMode()` to get more information about the input source.

### Squeezestart

```csharp
public event WebXRInterface.SqueezestartEventHandler Squeezestart
```

引擎名：`squeezestart`

Emitted when one of the input sources has started its "primary squeeze action".

Use `GetInputSourceTracker()` and `GetInputSourceTargetRayMode()` to get more information about the input source.

### VisibilityStateChanged

```csharp
public event Action VisibilityStateChanged
```

引擎名：`visibility_state_changed`

Emitted when `VisibilityState` has changed.

## SelectEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`WebXRInterface.Select`](WebXRInterface.md) event of a [`WebXRInterface`](WebXRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void SelectEventHandler(long inputSourceId)
```

## SelectendEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`WebXRInterface.Selectend`](WebXRInterface.md) event of a [`WebXRInterface`](WebXRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void SelectendEventHandler(long inputSourceId)
```

## SelectstartEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`WebXRInterface.Selectstart`](WebXRInterface.md) event of a [`WebXRInterface`](WebXRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void SelectstartEventHandler(long inputSourceId)
```

## SessionFailedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`WebXRInterface.SessionFailed`](WebXRInterface.md) event of a [`WebXRInterface`](WebXRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void SessionFailedEventHandler(string message)
```

## SessionSupportedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`WebXRInterface.SessionSupported`](WebXRInterface.md) event of a [`WebXRInterface`](WebXRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void SessionSupportedEventHandler(string sessionMode, bool supported)
```

## SqueezeEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`WebXRInterface.Squeeze`](WebXRInterface.md) event of a [`WebXRInterface`](WebXRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void SqueezeEventHandler(long inputSourceId)
```

## SqueezeendEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`WebXRInterface.Squeezeend`](WebXRInterface.md) event of a [`WebXRInterface`](WebXRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void SqueezeendEventHandler(long inputSourceId)
```

## SqueezestartEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`WebXRInterface.Squeezestart`](WebXRInterface.md) event of a [`WebXRInterface`](WebXRInterface.md) class.

### 签名

#### Invoke

```csharp
delegate void SqueezestartEventHandler(long inputSourceId)
```

## TargetRayMode

命名空间 `Godot`；enum

### 枚举值

- `Unknown = 0`：We don't know the target ray mode.
- `Gaze = 1`：Target ray originates at the viewer's eyes and points in the direction they are looking.
- `TrackedPointer = 2`：Target ray from a handheld pointer, most likely a VR touch controller.
- `Screen = 3`：Target ray from touch screen, mouse or other tactile input device.
