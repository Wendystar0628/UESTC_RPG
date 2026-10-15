<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEvent

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Abstract base class of all types of input events. See [`Node._Input()`](Node.md).

## 属性

### Device

```csharp
public int Device { get; set; }
```

引擎名：`device`

The event's device ID.

**Note:** `Device` can be negative for special use cases that don't refer to devices physically present on the system. See `DeviceIdEmulation`.

## 方法

### Accumulate

```csharp
public bool Accumulate(InputEvent withEvent)
```

引擎名：`accumulate`

Returns `true` if the given input event and this input event can be added together (only for events of type [`InputEventMouseMotion`](InputEventMouseMotion.md)).

The given input event's position, global position and speed will be copied. The resulting `relative` is a sum of both events. Both events' modifiers have to be identical.

### AsText

```csharp
public string AsText()
```

引擎名：`as_text`

Returns a `System.String` representation of the event.

### GetActionStrength

```csharp
public float GetActionStrength(StringName action, bool exactMatch = false)
```

引擎名：`get_action_strength`

Returns a value between 0.0 and 1.0 depending on the given actions' state. Useful for getting the value of events of type [`InputEventJoypadMotion`](InputEventJoypadMotion.md).

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

### GetDevice

```csharp
public int GetDevice()
```

引擎名：`get_device`

### IsAction

```csharp
public bool IsAction(StringName action, bool exactMatch = false)
```

引擎名：`is_action`

Returns `true` if this input event matches a pre-defined action of any type.

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

### IsActionPressed

```csharp
public bool IsActionPressed(StringName action, bool allowEcho = false, bool exactMatch = false)
```

引擎名：`is_action_pressed`

Returns `true` if the given action matches this event and is being pressed (and is not an echo event for [`InputEventKey`](InputEventKey.md) events, unless `allowEcho` is `true`). Not relevant for events of type [`InputEventMouseMotion`](InputEventMouseMotion.md) or [`InputEventScreenDrag`](InputEventScreenDrag.md).

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

**Note:** Due to keyboard ghosting, `IsActionPressed()` may return `false` even if one of the action's keys is pressed. See [Input examples]($DOCS_URL/tutorials/inputs/input_examples.html#keyboard-events) in the documentation for more information.

### IsActionReleased

```csharp
public bool IsActionReleased(StringName action, bool exactMatch = false)
```

引擎名：`is_action_released`

Returns `true` if the given action matches this event and is released (i.e. not pressed). Not relevant for events of type [`InputEventMouseMotion`](InputEventMouseMotion.md) or [`InputEventScreenDrag`](InputEventScreenDrag.md).

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

### IsActionType

```csharp
public bool IsActionType()
```

引擎名：`is_action_type`

Returns `true` if this input event's type is one that can be assigned to an input action: [`InputEventKey`](InputEventKey.md), [`InputEventMouseButton`](InputEventMouseButton.md), [`InputEventJoypadButton`](InputEventJoypadButton.md), [`InputEventJoypadMotion`](InputEventJoypadMotion.md), [`InputEventAction`](InputEventAction.md). Returns `false` for all other input event types.

### IsCanceled

```csharp
public bool IsCanceled()
```

引擎名：`is_canceled`

Returns `true` if this input event has been canceled.

### IsEcho

```csharp
public bool IsEcho()
```

引擎名：`is_echo`

Returns `true` if this input event is an echo event (only for events of type [`InputEventKey`](InputEventKey.md)). An echo event is a repeated key event sent when the user is holding down the key. Any other event type returns `false`.

**Note:** The rate at which echo events are sent is typically around 20 events per second (after holding down the key for roughly half a second). However, the key repeat delay/speed can be changed by the user or disabled entirely in the operating system settings. To ensure your project works correctly on all configurations, do not assume the user has a specific key repeat configuration in your project's behavior.

### IsMatch

```csharp
public bool IsMatch(InputEvent event, bool exactMatch = true)
```

引擎名：`is_match`

Returns `true` if the specified `event` matches this event. Only valid for action events, which include key ([`InputEventKey`](InputEventKey.md)), button ([`InputEventMouseButton`](InputEventMouseButton.md) or [`InputEventJoypadButton`](InputEventJoypadButton.md)), axis [`InputEventJoypadMotion`](InputEventJoypadMotion.md), and action ([`InputEventAction`](InputEventAction.md)) events.

If `exactMatch` is `false`, the check ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

**Note:** This method only considers the event configuration (such as the keyboard key or the joypad axis), not state information like `IsPressed()`, `IsReleased()`, `IsEcho()`, or `IsCanceled()`.

### IsPressed

```csharp
public bool IsPressed()
```

引擎名：`is_pressed`

Returns `true` if this input event is pressed. Not relevant for events of type [`InputEventMouseMotion`](InputEventMouseMotion.md) or [`InputEventScreenDrag`](InputEventScreenDrag.md).

**Note:** Due to keyboard ghosting, `IsPressed()` may return `false` even if one of the action's keys is pressed. See [Input examples]($DOCS_URL/tutorials/inputs/input_examples.html#keyboard-events) in the documentation for more information.

### IsReleased

```csharp
public bool IsReleased()
```

引擎名：`is_released`

Returns `true` if this input event is released. Not relevant for events of type [`InputEventMouseMotion`](InputEventMouseMotion.md) or [`InputEventScreenDrag`](InputEventScreenDrag.md).

### SetDevice

```csharp
public void SetDevice(int device)
```

引擎名：`set_device`

### XformedBy

```csharp
public InputEvent XformedBy(Transform2D xform, Vector2? localOfs = null)
```

引擎名：`xformed_by`

Returns a copy of the given input event which has been offset by `localOfs` and transformed by `xform`. Relevant for events of type [`InputEventMouseButton`](InputEventMouseButton.md), [`InputEventMouseMotion`](InputEventMouseMotion.md), [`InputEventScreenTouch`](InputEventScreenTouch.md), [`InputEventScreenDrag`](InputEventScreenDrag.md), [`InputEventMagnifyGesture`](InputEventMagnifyGesture.md) and [`InputEventPanGesture`](InputEventPanGesture.md).

参数：
- `localOfs`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

## 常量

### DeviceIdEmulation

```csharp
public const long DeviceIdEmulation = -1
```

Device ID used for emulated mouse input from a touchscreen, or for emulated touch input from a mouse. This can be used to distinguish emulated mouse input from physical mouse input, or emulated touch input from physical touch input.

### DeviceIdKeyboard

```csharp
public const long DeviceIdKeyboard = 16
```

Device ID used for input from a keyboard. This can be used to distinguish keyboard input events from joypad input events.

### DeviceIdMouse

```csharp
public const long DeviceIdMouse = 32
```

Device ID used for input from a mouse. This can be used to distinguish mouse input events from joypad input events.
