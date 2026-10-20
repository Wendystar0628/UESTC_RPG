<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The [`Input`](Input.md) singleton handles key presses, mouse buttons and movement, gamepads, and input actions. Actions and their events can be set in the **Input Map** tab in **Project > Project Settings**, or with the [`InputMap`](InputMap.md) class.

**Note:** [`Input`](Input.md)'s methods reflect the global input state and are not affected by [`Control.AcceptEvent`](Control.md) or [`Viewport.SetInputAsHandled`](Viewport.md), as those methods only deal with the way input is propagated in the [`SceneTree`](SceneTree.md).

## 属性

### EmulateMouseFromTouch

```csharp
public bool EmulateMouseFromTouch { get; set; }
```

If `true`, sends mouse input events when tapping or swiping on the touchscreen. See also `ProjectSettings.input_devices/pointing/emulate_mouse_from_touch`.

### EmulateTouchFromMouse

```csharp
public bool EmulateTouchFromMouse { get; set; }
```

If `true`, sends touch input events when clicking or dragging the mouse. See also `ProjectSettings.input_devices/pointing/emulate_touch_from_mouse`.

### IgnoreJoypadOnUnfocusedApplication

```csharp
public bool IgnoreJoypadOnUnfocusedApplication { get; set; }
```

If `true`, joypad input (including motion sensors) and LED light changes will be ignored and joypad vibration will be stopped when the application is not focused.

### MouseMode

```csharp
public Input.MouseModeEnum MouseMode { get; set; }
```

Controls the mouse mode.

### UseAccumulatedInput

```csharp
public bool UseAccumulatedInput { get; set; }
```

If `true`, similar input events sent by the operating system are accumulated. When input accumulation is enabled, all input events generated during a frame will be merged and emitted when the frame is done rendering. Therefore, this limits the number of input method calls per second to the rendering FPS.

Input accumulation can be disabled to get slightly more precise/reactive input at the cost of increased CPU usage. In applications where drawing freehand lines is required, input accumulation should generally be disabled while the user is drawing the line to get results that closely follow the actual input.

**Note:** Input accumulation is *enabled* by default.

## 方法

### ActionPress

```csharp
public void ActionPress(StringName action, float strength = 1f)
```

This will simulate pressing the specified action.

The strength can be used for non-boolean actions, it's ranged between 0 and 1 representing the intensity of the given action.

**Note:** This method will not cause any [`Node._Input()`](Node.md) calls. It is intended to be used with `IsActionPressed()` and `IsActionJustPressed()`. If you want to simulate `_input`, use `ParseInputEvent()` instead.

### ActionRelease

```csharp
public void ActionRelease(StringName action)
```

If the specified action is already pressed, this will release it.

### AddJoyMapping

```csharp
public void AddJoyMapping(string mapping, bool updateExisting = false)
```

Adds a new mapping entry (in SDL2 format) to the mapping database. Optionally update already connected devices.

### ClearJoyMotionSensorsCalibration

```csharp
public void ClearJoyMotionSensorsCalibration(int device)
```

Clears the calibration information for the specified joypad's motion sensors, if it has any and if they were calibrated.

See `StartJoyMotionSensorsCalibration()` for an example on how to use joypad motion sensors and calibration in your games.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### FlushBufferedEvents

```csharp
public void FlushBufferedEvents()
```

Sends all input events which are in the current buffer to the game loop. These events may have been buffered as a result of accumulated input (`UseAccumulatedInput`) or agile input flushing (`ProjectSettings.input_devices/buffering/agile_event_flushing`).

The engine will already do this itself at key execution points (at least once per frame). However, this can be useful in advanced cases where you want precise control over the timing of event handling.

### GetAccelerometer

```csharp
public Vector3 GetAccelerometer()
```

Returns the acceleration in m/s² of the device's accelerometer sensor, if the device has one. Otherwise, the method returns `Vector3.Zero`.

Note this method returns an empty [`Vector3`](Vector3.md) when running from the editor even when your device has an accelerometer. You must export your project to a supported device to read values from the accelerometer.

**Note:** This method only works on Android and iOS. On other platforms, it always returns `Vector3.Zero`.

**Note:** For Android, `ProjectSettings.input_devices/sensors/enable_accelerometer` must be enabled.

### GetActionRawStrength

```csharp
public float GetActionRawStrength(StringName action, bool exactMatch = false)
```

Returns a value between 0 and 1 representing the raw intensity of the given action, ignoring the action's deadzone. In most cases, you should use `GetActionStrength()` instead.

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

### GetActionStrength

```csharp
public float GetActionStrength(StringName action, bool exactMatch = false)
```

Returns a value between 0 and 1 representing the intensity of the given action. In a joypad, for example, the further away the axis (analog sticks or L2, R2 triggers) is from the dead zone, the closer the value will be to 1. If the action is mapped to a control that has no axis such as the keyboard, the value returned will be 0 or 1.

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

### GetAxis

```csharp
public float GetAxis(StringName negativeAction, StringName positiveAction)
```

Get axis input by specifying two actions, one negative and one positive.

This is a shorthand for writing `Input.get_action_strength("positive_action") - Input.get_action_strength("negative_action")`.

### GetConnectedJoypads

```csharp
public Array<int> GetConnectedJoypads()
```

Returns an [`Collections.Array`](Collections.Array.md) containing the device IDs of all currently connected joypads.

**Note:** The order of connected joypads can not be guaranteed to be the same after a project and/or the editor is restarted, because Godot doesn't save the order of joypad connections. Joypads are registered in the order they are discovered by Godot.

### GetCurrentCursorShape

```csharp
public Input.CursorShape GetCurrentCursorShape()
```

Returns the currently assigned cursor shape.

### GetGravity

```csharp
public Vector3 GetGravity()
```

Returns the gravity in m/s² of the device's accelerometer sensor, if the device has one. Otherwise, the method returns `Vector3.Zero`.

**Note:** This method only works on Android and iOS. On other platforms, it always returns `Vector3.Zero`.

**Note:** For Android, `ProjectSettings.input_devices/sensors/enable_gravity` must be enabled.

### GetGyroscope

```csharp
public Vector3 GetGyroscope()
```

Returns the rotation rate in rad/s around a device's X, Y, and Z axes of the gyroscope sensor, if the device has one. Otherwise, the method returns `Vector3.Zero`.

**Note:** This method only works on Android and iOS. On other platforms, it always returns `Vector3.Zero`.

**Note:** For Android, `ProjectSettings.input_devices/sensors/enable_gyroscope` must be enabled.

### GetJoyAccelerometer

```csharp
public Vector3 GetJoyAccelerometer(int device)
```

Returns the acceleration, including the force of gravity, in m/s² of the joypad's accelerometer sensor, if the joypad has one and it's currently enabled. Otherwise, the method returns `Vector3.Zero`. See also `GetJoyGravity()` and `SetJoyMotionSensorsEnabled()`.

For a joypad held in front of you, the returned axes are defined as follows:

+X ... -X: left ... right;

+Y ... -Y: bottom ... top;

+Z ... -Z: farther ... closer.

The gravity part value is measured as a vector with length of `9.8` away from the center of the Earth, which is a negative Y value.

**Note:** This feature is only supported on Windows, Linux, and macOS. On iOS, joypad accelerometer sensor reading is not supported due to OS limitations.

### GetJoyAxis

```csharp
public float GetJoyAxis(int device, JoyAxis axis)
```

Returns the current value of the joypad axis at index `axis`.

### GetJoyGravity

```csharp
public Vector3 GetJoyGravity(int device)
```

Returns the gravity in m/s² of the joypad's accelerometer sensor, if the joypad has one and it's currently enabled. Otherwise, the method returns `Vector3.Zero`. See also `GetJoyAccelerometer()` and `SetJoyMotionSensorsEnabled()`.

For a joypad held in front of you, the returned axes are defined as follows:

+X ... -X: left ... right;

+Y ... -Y: bottom ... top;

+Z ... -Z: farther ... closer.

The gravity part value is measured as a vector with length of `9.8` away from the center of the Earth, which is a negative Y value.

**Note:** This feature is only supported on Windows, Linux, and macOS. On iOS, joypad accelerometer sensor reading is not supported due to OS limitations.

### GetJoyGuid

```csharp
public string GetJoyGuid(int device)
```

Returns an SDL-compatible device GUID on platforms that use gamepad remapping, e.g. `030000004c050000c405000000010000`. Returns an empty string if it cannot be found. Godot uses SDL's internal mappings, supplemented by community-contributed mappings, to determine gamepad names and mappings based on this GUID.

On Windows, all XInput joypad GUIDs will be overridden by Godot to `__XINPUT_DEVICE__`, because their mappings are the same.

### GetJoyGyroscope

```csharp
public Vector3 GetJoyGyroscope(int device)
```

Returns the rotation rate in rad/s around a joypad's X, Y, and Z axes of the gyroscope sensor, if the joypad has one and it's currently enabled. Otherwise, the method returns `Vector3.Zero`. See also `SetJoyMotionSensorsEnabled()`.

The rotation is positive in the counter-clockwise direction.

For a joypad held in front of you, the returned axes are defined as follows:

X: Angular speed around the X axis (pitch);

Y: Angular speed around the Y axis (yaw);

Z: Angular speed around the Z axis (roll).

See `StartJoyMotionSensorsCalibration()` for an example on how to use joypad gyroscope and gyroscope calibration in your games.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### GetJoyInfo

```csharp
public Dictionary GetJoyInfo(int device)
```

Returns a dictionary with extra platform-specific information about the device, e.g. the raw gamepad name from the OS or the Steam Input index.

On Windows, Linux, macOS, and iOS, the dictionary contains the following fields:

`raw_name`: The name of the controller as it came from the OS, before getting renamed by the controller database.

`vendor_id`: The USB vendor ID of the device.

`product_id`: The USB product ID of the device.

`serial_number`: The serial number of the device. This key won't be present if the serial number is unavailable.

The dictionary can also include the following fields under selected platforms:

`steam_input_index`: The Steam Input gamepad index (Windows, Linux, and macOS only). If the device is not a Steam Input device this key won't be present.

`xinput_index`: The index of the controller in the XInput system (Windows only). This key won't be present for devices not handled by XInput.

**Note:** The returned dictionary is always empty on Android and Web.

### GetJoyMotionSensorsCalibration

```csharp
public Dictionary GetJoyMotionSensorsCalibration(int device)
```

Returns the calibration information about the specified joypad's motion sensors in the form of a [`Collections.Dictionary`](Collections.Dictionary.md), if it has any and if they have been calibrated, otherwise returns an empty [`Collections.Dictionary`](Collections.Dictionary.md).

The dictionary contains the following fields:

`gyroscope_offset`: average offset in gyroscope values from `Vector2.Zero` in rad/s.

See `StartJoyMotionSensorsCalibration()` for an example on how to use joypad motion sensors and calibration in your games.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### GetJoyMotionSensorsRate

```csharp
public float GetJoyMotionSensorsRate(int device)
```

Returns the joypad's motion sensor rate in Hz, if the joypad has motion sensors and they're currently enabled. See also `SetJoyMotionSensorsEnabled()`.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### GetJoyName

```csharp
public string GetJoyName(int device)
```

Returns the name of the joypad at the specified device index, e.g. `PS4 Controller`. Godot uses the [SDL2 game controller database](https://github.com/gabomdq/SDL_GameControllerDB) to determine gamepad names.

### GetJoyVibrationDuration

```csharp
public float GetJoyVibrationDuration(int device)
```

Returns the duration of the current vibration effect in seconds.

**Note:** This method returns the same value that was passed to `StartJoyVibration()`, and this value does **not** change when the joypad's vibration runs out, it only gets reset after a call to `StopJoyVibration()`.

If you want to check if a joypad is still vibrating, use `IsJoyVibrating()` instead.

### GetJoyVibrationRemainingDuration

```csharp
public float GetJoyVibrationRemainingDuration(int device)
```

Returns the remaining duration of the current vibration effect in seconds.

### GetJoyVibrationStrength

```csharp
public Vector2 GetJoyVibrationStrength(int device)
```

Returns the strength of the joypad vibration: x is the strength of the weak motor, and y is the strength of the strong motor.

**Note:** This method returns the same values that were passed to `StartJoyVibration()`, and these values do **not** change when the joypad's vibration runs out, they only get reset after a call to `StopJoyVibration()`.

If you want to check if a joypad is still vibrating, use `IsJoyVibrating()` instead.

### GetLastMouseScreenVelocity

```csharp
public Vector2 GetLastMouseScreenVelocity()
```

Returns the last mouse velocity in screen coordinates. To provide a precise and jitter-free velocity, mouse velocity is only calculated every 0.1s. Therefore, mouse velocity will lag mouse movements.

### GetLastMouseVelocity

```csharp
public Vector2 GetLastMouseVelocity()
```

Returns the last mouse velocity. To provide a precise and jitter-free velocity, mouse velocity is only calculated every 0.1s. Therefore, mouse velocity will lag mouse movements.

### GetMagnetometer

```csharp
public Vector3 GetMagnetometer()
```

Returns the magnetic field strength in micro-Tesla for all axes of the device's magnetometer sensor, if the device has one. Otherwise, the method returns `Vector3.Zero`.

**Note:** This method only works on Android and iOS. On other platforms, it always returns `Vector3.Zero`.

**Note:** For Android, `ProjectSettings.input_devices/sensors/enable_magnetometer` must be enabled.

### GetMouseButtonMask

```csharp
public MouseButtonMask GetMouseButtonMask()
```

Returns mouse buttons as a bitmask. If multiple mouse buttons are pressed at the same time, the bits are added together. Equivalent to [`DisplayServer.MouseGetButtonState`](DisplayServer.md).

### GetMouseMode

```csharp
public Input.MouseModeEnum GetMouseMode()
```

### GetVector

```csharp
public Vector2 GetVector(StringName negativeX, StringName positiveX, StringName negativeY, StringName positiveY, float deadzone = -1f)
```

Gets an input vector by specifying four actions for the positive and negative X and Y axes.

This method is useful when getting vector input, such as from a joystick, directional pad, arrows, or WASD. The vector has its length limited to 1 and has a circular deadzone, which is useful for using vector input as movement.

By default, the deadzone is automatically calculated from the average of the action deadzones. However, you can override the deadzone to be whatever you want (on the range of 0 to 1).

### HasJoyLight

```csharp
public bool HasJoyLight(int device)
```

Returns `true` if the joypad has an LED light that can change colors and/or brightness. See also `SetJoyLight()`.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### HasJoyMotionSensors

```csharp
public bool HasJoyMotionSensors(int device)
```

Returns `true` if the joypad has motion sensors (accelerometer and gyroscope).

**Note:** On iOS, joypad accelerometer sensor reading is not supported due to OS limitations.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### HasJoyVibration

```csharp
public bool HasJoyVibration(int device)
```

Returns `true` if the joypad supports vibration. See also `StartJoyVibration()`.

**Note:** For macOS, vibration is only supported in macOS 11 and later. When connected via USB, vibration is only supported for major brand controllers (except Xbox One and Xbox Series X/S controllers) due to macOS limitations.

### IsActionJustPressed

```csharp
public bool IsActionJustPressed(StringName action, bool exactMatch = false)
```

Returns `true` when the user has *started* pressing the action event in the current frame or physics tick. It will only return `true` on the frame or tick that the user pressed down the button.

This is useful for code that needs to run only once when an action is pressed, instead of every frame while it's pressed.

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

**Note:** Returning `true` does not imply that the action is *still* pressed. An action can be pressed and released again rapidly, and `true` will still be returned so as not to miss input.

**Note:** Due to keyboard ghosting, `IsActionJustPressed()` may return `false` even if one of the action's keys is pressed. See [Input examples]($DOCS_URL/tutorials/inputs/input_examples.html#keyboard-events) in the documentation for more information.

**Note:** During input handling (e.g. [`Node._Input()`](Node.md)), use [`InputEvent.IsActionPressed`](InputEvent.md) instead to query the action state of the current event. See also `IsActionJustPressedByEvent()`.

### IsActionJustPressedByEvent

```csharp
public bool IsActionJustPressedByEvent(StringName action, InputEvent event, bool exactMatch = false)
```

Returns `true` when the user has *started* pressing the action event in the current frame or physics tick, and the first event that triggered action press in the current frame/physics tick was `event`. It will only return `true` on the frame or tick that the user pressed down the button.

This is useful for code that needs to run only once when an action is pressed, and the action is processed during input handling (e.g. [`Node._Input()`](Node.md)).

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

**Note:** Returning `true` does not imply that the action is *still* pressed. An action can be pressed and released again rapidly, and `true` will still be returned so as not to miss input.

**Note:** Due to keyboard ghosting, `IsActionJustPressed()` may return `false` even if one of the action's keys is pressed. See [Input examples]($DOCS_URL/tutorials/inputs/input_examples.html#keyboard-events) in the documentation for more information.

### IsActionJustReleased

```csharp
public bool IsActionJustReleased(StringName action, bool exactMatch = false)
```

Returns `true` when the user *stops* pressing the action event in the current frame or physics tick. It will only return `true` on the frame or tick that the user releases the button.

**Note:** Returning `true` does not imply that the action is *still* not pressed. An action can be released and pressed again rapidly, and `true` will still be returned so as not to miss input.

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

**Note:** During input handling (e.g. [`Node._Input()`](Node.md)), use [`InputEvent.IsActionReleased`](InputEvent.md) instead to query the action state of the current event. See also `IsActionJustReleasedByEvent()`.

### IsActionJustReleasedByEvent

```csharp
public bool IsActionJustReleasedByEvent(StringName action, InputEvent event, bool exactMatch = false)
```

Returns `true` when the user *stops* pressing the action event in the current frame or physics tick, and the first event that triggered action release in the current frame/physics tick was `event`. It will only return `true` on the frame or tick that the user releases the button.

This is useful when an action is processed during input handling (e.g. [`Node._Input()`](Node.md)).

**Note:** Returning `true` does not imply that the action is *still* not pressed. An action can be released and pressed again rapidly, and `true` will still be returned so as not to miss input.

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

### IsActionPressed

```csharp
public bool IsActionPressed(StringName action, bool exactMatch = false)
```

Returns `true` if you are pressing the action event.

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

**Note:** Due to keyboard ghosting, `IsActionPressed()` may return `false` even if one of the action's keys is pressed. See [Input examples]($DOCS_URL/tutorials/inputs/input_examples.html#keyboard-events) in the documentation for more information.

### IsAnythingPressed

```csharp
public bool IsAnythingPressed()
```

Returns `true` if any action, key, joypad button, or mouse button is being pressed. This will also return `true` if any action is simulated via code by calling `ActionPress()`.

### IsEmulatingMouseFromTouch

```csharp
public bool IsEmulatingMouseFromTouch()
```

### IsEmulatingTouchFromMouse

```csharp
public bool IsEmulatingTouchFromMouse()
```

### IsIgnoringJoypadOnUnfocusedApplication

```csharp
public bool IsIgnoringJoypadOnUnfocusedApplication()
```

### IsJoyButtonPressed

```csharp
public bool IsJoyButtonPressed(int device, JoyButton button)
```

Returns `true` if you are pressing the joypad button at index `button`.

**Note:** If you want to check if a joypad button was just pressed, use Godot's input action system with `IsActionJustPressed()` or use the [`Node._Input()`](Node.md) method like this instead:

```csharp
public override void _Input(InputEvent @event)
{
if (@event is InputEventJoypadButton eventButton && eventButton.Pressed && eventButton.ButtonIndex == JoyButton.A)
{
// Your code here.
}
}

```

### IsJoyKnown

```csharp
public bool IsJoyKnown(int device)
```

Returns `true` if the system knows the specified device. This means that it sets all button and axis indices. Unknown joypads are not expected to match these constants, but you can still retrieve events from them.

### IsJoyMotionSensorsCalibrated

```csharp
public bool IsJoyMotionSensorsCalibrated(int device)
```

Returns `true` if the joypad's motion sensors have been calibrated.

See `StartJoyMotionSensorsCalibration()` for an example on how to use joypad motion sensors and calibration in your games.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### IsJoyMotionSensorsCalibrating

```csharp
public bool IsJoyMotionSensorsCalibrating(int device)
```

Returns `true` if the joypad's motion sensors are currently being calibrated.

See `StartJoyMotionSensorsCalibration()` for an example on how to use joypad motion sensors and calibration in your games.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### IsJoyMotionSensorsEnabled

```csharp
public bool IsJoyMotionSensorsEnabled(int device)
```

Returns `true` if the requested joypad has motion sensors (accelerometer and gyroscope) and they are currently enabled. See also `SetJoyMotionSensorsEnabled()` and `HasJoyMotionSensors()`.

See `StartJoyMotionSensorsCalibration()` for an example on how to use joypad motion sensors and calibration in your games.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### IsJoyVibrating

```csharp
public bool IsJoyVibrating(int device)
```

Returns `true` if the joypad is still vibrating after a call to `StartJoyVibration()`.

Unlike `GetJoyVibrationStrength()` and `GetJoyVibrationDuration()`, this method returns `false` after the joypad's vibration runs out.

### IsKeyLabelPressed

```csharp
public bool IsKeyLabelPressed(Key keycode)
```

Returns `true` if you are pressing the key with the `keycode` printed on it. You can pass a [`Key`](Key.md) constant or any Unicode character code.

**Note:** If you want to check if a key was just pressed by using its label, use Godot's input action system with `IsActionJustPressed()` or use the [`Node._Input()`](Node.md) method like this instead:

```csharp
public override void _Input(InputEvent @event)
{
if (@event is InputEventKey eventKey && !eventKey.IsEcho() && eventKey.Pressed && eventKey.KeyLabel == Key.Space)
{
// Your code here.
}
}

```

### IsKeyPressed

```csharp
public bool IsKeyPressed(Key keycode)
```

Returns `true` if you are pressing the Latin key in the current keyboard layout. You can pass a [`Key`](Key.md) constant.

`IsKeyPressed()` is only recommended over `IsPhysicalKeyPressed()` in non-game applications. This ensures that shortcut keys behave as expected depending on the user's keyboard layout, as keyboard shortcuts are generally dependent on the keyboard layout in non-game applications. If in doubt, use `IsPhysicalKeyPressed()`.

**Note:** Due to keyboard ghosting, `IsKeyPressed()` may return `false` even if one of the action's keys is pressed. See [Input examples]($DOCS_URL/tutorials/inputs/input_examples.html#keyboard-events) in the documentation for more information.

**Note:** If you want to check if a key was just pressed by using its keycode, use Godot's input action system with `IsActionJustPressed()` or use the [`Node._Input()`](Node.md) method like this instead:

```csharp
public override void _Input(InputEvent @event)
{
if (@event is InputEventKey eventKey && !eventKey.IsEcho() && eventKey.Pressed && eventKey.Keycode == Key.Space)
{
// Your code here.
}
}

```

### IsMouseButtonPressed

```csharp
public bool IsMouseButtonPressed(MouseButton button)
```

Returns `true` if you are pressing the mouse button specified with [`MouseButton`](MouseButton.md).

**Note:** If you want to check if a mouse button was just pressed, use Godot's input action system with `IsActionJustPressed()` or use the [`Node._Input()`](Node.md) method like this instead:

```csharp
public override void _Input(InputEvent @event)
{
if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed && eventMouseButton.ButtonIndex == MouseButton.Left)
{
// Your code here.
}
}

```

### IsPhysicalKeyPressed

```csharp
public bool IsPhysicalKeyPressed(Key keycode)
```

Returns `true` if you are pressing the key in the physical location on the 101/102-key US QWERTY keyboard. You can pass a [`Key`](Key.md) constant.

`IsPhysicalKeyPressed()` is recommended over `IsKeyPressed()` for in-game actions, as it will make W/A/S/D layouts work regardless of the user's keyboard layout. `IsPhysicalKeyPressed()` will also ensure that the top row number keys work on any keyboard layout. If in doubt, use `IsPhysicalKeyPressed()`.

**Note:** Due to keyboard ghosting, `IsPhysicalKeyPressed()` may return `false` even if one of the action's keys is pressed. See [Input examples]($DOCS_URL/tutorials/inputs/input_examples.html#keyboard-events) in the documentation for more information.

**Note:** If you want to check if a key was just pressed by using its physical keycode, use Godot's input action system with `IsActionJustPressed()` or use the [`Node._Input()`](Node.md) method like this instead:

```csharp
public override void _Input(InputEvent @event)
{
if (@event is InputEventKey eventKey && !eventKey.IsEcho() && eventKey.Pressed && eventKey.PhysicalKeycode == Key.Space)
{
// Your code here.
}
}

```

### IsUsingAccumulatedInput

```csharp
public bool IsUsingAccumulatedInput()
```

### ParseInputEvent

```csharp
public void ParseInputEvent(InputEvent event)
```

Feeds an [`InputEvent`](InputEvent.md) to the game. Can be used to artificially trigger input events from code. Also generates [`Node._Input()`](Node.md) calls.

```csharp
var cancelEvent = new InputEventAction();
cancelEvent.Action = "ui_cancel";
cancelEvent.Pressed = true;
Input.ParseInputEvent(cancelEvent);

```

**Note:** Calling this function has no influence on the operating system. So for example sending an [`InputEventMouseMotion`](InputEventMouseMotion.md) will not move the OS mouse cursor to the specified position (use `WarpMouse()` instead) and sending Alt/Cmd + Tab as [`InputEventKey`](InputEventKey.md) won't toggle between active windows.

### RemoveJoyMapping

```csharp
public void RemoveJoyMapping(string guid)
```

Removes all mappings from the internal database that match the given GUID. All currently connected joypads that use this GUID will become unmapped.

On Android, Godot will map to an internal fallback mapping.

### SetAccelerometer

```csharp
public void SetAccelerometer(Vector3 value)
```

Sets the acceleration value of the accelerometer sensor. Can be used for debugging on devices without a hardware sensor, for example in an editor on a PC.

**Note:** This value can be immediately overwritten by the hardware sensor value on Android and iOS.

### SetCustomMouseCursor

```csharp
public void SetCustomMouseCursor(Resource image, Input.CursorShape shape = Input.CursorShape.Arrow, Vector2? hotspot = null)
```

Sets a custom mouse cursor image, which is only visible inside the game window, for the given mouse `shape`. The hotspot can also be specified. Passing `null` to the image parameter resets to the system cursor.

`image` can be either [`Texture2D`](Texture2D.md) or [`Image`](Image.md) and its size must be lower than or equal to 256×256. To avoid rendering issues, sizes lower than or equal to 128×128 are recommended.

`hotspot` must be within `image`'s size.

**Note:** [`AnimatedTexture`](AnimatedTexture.md)s aren't supported as custom mouse cursors. If using an [`AnimatedTexture`](AnimatedTexture.md), only the first frame will be displayed.

**Note:** The **Lossless**, **Lossy** or **Uncompressed** compression modes are recommended. The **Video RAM** compression mode can be used, but it will be decompressed on the CPU, which means loading times are slowed down and no memory is saved compared to lossless modes.

**Note:** On the web platform, the maximum allowed cursor image size is 128×128. Cursor images larger than 32×32 will also only be displayed if the mouse cursor image is entirely located within the page for [security reasons](https://chromestatus.com/feature/5825971391299584).

参数：
- `hotspot`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### SetDefaultCursorShape

```csharp
public void SetDefaultCursorShape(Input.CursorShape shape = Input.CursorShape.Arrow)
```

Sets the default cursor shape to be used in the viewport instead of [`Input.CursorShape.Arrow`](Input.md).

**Note:** If you want to change the default cursor shape for [`Control`](Control.md)'s nodes, use [`Control.MouseDefaultCursorShape`](Control.md) instead.

**Note:** This method generates an [`InputEventMouseMotion`](InputEventMouseMotion.md) to update cursor immediately.

### SetEmulateMouseFromTouch

```csharp
public void SetEmulateMouseFromTouch(bool enable)
```

### SetEmulateTouchFromMouse

```csharp
public void SetEmulateTouchFromMouse(bool enable)
```

### SetGravity

```csharp
public void SetGravity(Vector3 value)
```

Sets the gravity value of the accelerometer sensor. Can be used for debugging on devices without a hardware sensor, for example in an editor on a PC.

**Note:** This value can be immediately overwritten by the hardware sensor value on Android and iOS.

### SetGyroscope

```csharp
public void SetGyroscope(Vector3 value)
```

Sets the value of the rotation rate of the gyroscope sensor. Can be used for debugging on devices without a hardware sensor, for example in an editor on a PC.

**Note:** This value can be immediately overwritten by the hardware sensor value on Android and iOS.

### SetIgnoreJoypadOnUnfocusedApplication

```csharp
public void SetIgnoreJoypadOnUnfocusedApplication(bool enable)
```

### SetJoyLight

```csharp
public void SetJoyLight(int device, Color color)
```

Sets the joypad's LED light, if available, to the specified color. See also `HasJoyLight()`.

**Note:** There is no way to get the color of the light from a joypad. If you need to know the assigned color, store it separately.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### SetJoyMotionSensorsCalibration

```csharp
public void SetJoyMotionSensorsCalibration(int device, Dictionary calibrationInfo)
```

Sets the specified joypad's calibration information. See also `GetJoyMotionSensorsCalibration()`.

See `StartJoyMotionSensorsCalibration()` for an example on how to use joypad motion sensors and calibration in your games.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### SetJoyMotionSensorsEnabled

```csharp
public void SetJoyMotionSensorsEnabled(int device, bool enable)
```

Enables or disables the motion sensors (accelerometer and gyroscope), if available, on the specified joypad.

See `StartJoyMotionSensorsCalibration()` for an example on how to use joypad motion sensors and calibration in your games.

It's recommended to disable the motion sensors when they're no longer being used, because otherwise it might drain the controller battery faster.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### SetMagnetometer

```csharp
public void SetMagnetometer(Vector3 value)
```

Sets the value of the magnetic field of the magnetometer sensor. Can be used for debugging on devices without a hardware sensor, for example in an editor on a PC.

**Note:** This value can be immediately overwritten by the hardware sensor value on Android and iOS.

### SetMouseMode

```csharp
public void SetMouseMode(Input.MouseModeEnum mode)
```

### SetUseAccumulatedInput

```csharp
public void SetUseAccumulatedInput(bool enable)
```

### ShouldIgnoreDevice

```csharp
public bool ShouldIgnoreDevice(int vendorId, int productId)
```

Queries whether an input device should be ignored or not. Devices can be ignored by setting the environment variable `SDL_GAMECONTROLLER_IGNORE_DEVICES`. Read the [SDL documentation](https://wiki.libsdl.org/SDL2) for more information.

**Note:** Some 3rd party tools can contribute to the list of ignored devices. For example, *SteamInput* creates virtual devices from physical devices for remapping purposes. To avoid handling the same input device twice, the original device is added to the ignore list.

### StartJoyMotionSensorsCalibration

```csharp
public void StartJoyMotionSensorsCalibration(int device)
```

Starts the process of calibrating the specified joypad's gyroscope, if it has one.

Once a joypad's gyroscope has been calibrated correctly (e.g. laying still on a table without being rotated), `GetJoyGyroscope()` will return values close or equal to `Vector3.Zero` when the joypad is not being rotated.

Here's an example of how to use joypad gyroscope and gyroscope calibration in your games:

```csharp
private const float GyroSensitivity = 10.0;

public override void _Ready()
{
// In this example we only use the first connected joypad (id 0).
if (!Input.GetConnectedJoypads().Contains(0))
{
return;
}

if (!Input.HasJoyMotionSensors(0))
{
return;
}

// We must enable the accelerometer and the gyroscope before using them.
Input.SetJoyMotionSensorsEnabled(0, true);

// (Tell the users here that they need to put their joypads on a flat surface and wait for confirmation.)

// Start the calibration process.
CalibrateMotion();
}

public override void _Process(double delta)
{
// Only move the object if the joypad motion sensors are calibrated.
if (Input.IsJoyMotionSensorsCalibrated(0))
{
MoveObject(delta);
}
}

private async Task CalibrateMotion()
{
Input.StartJoyMotionSensorsCalibration(0);

// Wait for some time.
await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);

Input.StopJoyMotionSensorsCalibration(0);
// The joypad is now calibrated.
}

private void MoveObject(double delta)
{
Node3D node = ... ; // Put your object here.
Vector3 gyro = Input.GetJoyGyroscope(0);
Vector3 rotation = node.Rotation;
rotation.X -= -gyro.Y * GyroSensitivity * (float)delta; // Use rotation around the Y axis (yaw) here.
rotation.Y += -gyro.X * GyroSensitivity * (float)delta; // Use rotation around the X axis (pitch) here.
node.Rotation = rotation;
}

```

**Note:** Accelerometer sensor doesn't usually require calibration.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### StartJoyVibration

```csharp
public void StartJoyVibration(int device, float weakMagnitude, float strongMagnitude, float duration = 0f)
```

Starts to vibrate the joypad. See also `HasJoyVibration()` and `IsJoyVibrating()`.

Joypads usually come with two rumble motors, a strong and a weak one.

`weakMagnitude` is the strength of the weak motor (between `0.0` and `1.0`).

`strongMagnitude` is the strength of the strong motor (between `0.0` and `1.0`).

`duration` is the duration of the effect in seconds (a duration of `0.0` will try to play the vibration as long as possible, which is about 65 seconds).

The vibration can be stopped early by calling `StopJoyVibration()`.

See also `GetJoyVibrationStrength()` and `GetJoyVibrationDuration()`.

**Note:** For macOS, vibration is only supported in macOS 11 and later. When connected via USB, vibration is only supported for major brand controllers (except Xbox One and Xbox Series X/S controllers) due to macOS limitations.

### StopJoyMotionSensorsCalibration

```csharp
public void StopJoyMotionSensorsCalibration(int device)
```

Stops the calibration process of the specified joypad's motion sensors.

See `StartJoyMotionSensorsCalibration()` for an example on how to use joypad motion sensors and calibration in your games.

**Note:** This feature is only supported on Windows, Linux, macOS, and iOS.

### StopJoyVibration

```csharp
public void StopJoyVibration(int device)
```

Stops the vibration of the joypad started with `StartJoyVibration()`.

### VibrateHandheld

```csharp
public void VibrateHandheld(int durationMs = 500, float amplitude = -1f)
```

Vibrate the handheld device for the specified duration in milliseconds.

`amplitude` is the strength of the vibration, as a value between `0.0` and `1.0`. If set to `-1.0`, the default vibration strength of the device is used.

**Note:** This method is implemented on Android, iOS, and Web. It has no effect on other platforms.

**Note:** For Android, `VibrateHandheld()` requires enabling the `VIBRATE` permission in the export preset. Otherwise, `VibrateHandheld()` will have no effect.

**Note:** For iOS, specifying the duration is only supported in iOS 13 and later.

**Note:** For Web, the amplitude cannot be changed.

**Note:** Some web browsers such as Safari and Firefox for Android do not support `VibrateHandheld()`.

**Note:** Device settings such as vibration on/off, "do not disturb" mode or specific haptic feedback on/off may prevent `VibrateHandheld()` effects.

### VibrateHandheld

```csharp
public void VibrateHandheld(int durationMs)
```

Vibrate the handheld device for the specified duration in milliseconds.

`amplitude` is the strength of the vibration, as a value between `0.0` and `1.0`. If set to `-1.0`, the default vibration strength of the device is used.

**Note:** This method is implemented on Android, iOS, and Web. It has no effect on other platforms.

**Note:** For Android, `VibrateHandheld()` requires enabling the `VIBRATE` permission in the export preset. Otherwise, `VibrateHandheld()` will have no effect.

**Note:** For iOS, specifying the duration is only supported in iOS 13 and later.

**Note:** For Web, the amplitude cannot be changed.

**Note:** Some web browsers such as Safari and Firefox for Android do not support `VibrateHandheld()`.

**Note:** Device settings such as vibration on/off, "do not disturb" mode or specific haptic feedback on/off may prevent `VibrateHandheld()` effects.

### WarpMouse

```csharp
public void WarpMouse(Vector2 position)
```

Sets the mouse position to the specified vector, provided in pixels and relative to an origin at the upper left corner of the currently focused Window Manager game window.

Mouse position is clipped to the limits of the screen resolution, or to the limits of the game window if [`Input.MouseModeEnum`](Input.md) is set to [`Input.MouseModeEnum.Confined`](Input.md) or [`Input.MouseModeEnum.ConfinedHidden`](Input.md).

**Note:** `WarpMouse()` is only supported on Windows, macOS and Linux. It has no effect on Android, iOS and Web.

## 事件（信号）

### JoyConnectionChanged

```csharp
public event InputInstance.JoyConnectionChangedEventHandler JoyConnectionChanged
```

Emitted when a joypad device has been connected or disconnected.

## JoyConnectionChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`InputInstance.JoyConnectionChanged`](InputInstance.md) event of a [`InputInstance`](InputInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void JoyConnectionChangedEventHandler(long device, bool connected)
```
