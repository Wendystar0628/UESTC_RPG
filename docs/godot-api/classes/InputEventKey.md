<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventKey

命名空间 `Godot`；class；继承 [`InputEventWithModifiers`](InputEventWithModifiers.md) → [`InputEventFromWindow`](InputEventFromWindow.md) → [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An input event for keys on a keyboard. Supports key presses, key releases and `Echo` events. It can also be received in [`Node._UnhandledKeyInput()`](Node.md).

**Note:** Events received from the keyboard usually have all properties set. Event mappings should have only one of the `Keycode`, `PhysicalKeycode` or `Unicode` set.

When events are compared, properties are checked in the following priority - `Keycode`, `PhysicalKeycode` and `Unicode`. Events with the first matching value will be considered equal.

## 构造函数

### InputEventKey

```csharp
public InputEventKey()
```

## 属性

### Echo

```csharp
public bool Echo { get; set; }
```

引擎名：`echo`

If `true`, the key was already pressed before this event. An echo event is a repeated key event sent when the user is holding down the key.

**Note:** The rate at which echo events are sent is typically around 20 events per second (after holding down the key for roughly half a second). However, the key repeat delay/speed can be changed by the user or disabled entirely in the operating system settings. To ensure your project works correctly on all configurations, do not assume the user has a specific key repeat configuration in your project's behavior.

### KeyLabel

```csharp
public Key KeyLabel { get; set; }
```

引擎名：`key_label`

Represents the localized label printed on the key in the current keyboard layout, which corresponds to one of the [`Key`](Key.md) constants or any valid Unicode character. Key labels are meant for key prompts.

For keyboard layouts with a single label on the key, it is equivalent to `Keycode`.

To get a human-readable representation of the `InputEventKey`, use `OS.get_keycode_string(event.key_label)` where `event` is the `InputEventKey`.

```csharp
+-----+ +-----+
| Q   | | Q   | - "Q" - keycode
|   Й | |  ض | - "Й" and "ض" - key_label
+-----+ +-----+

```

### Keycode

```csharp
public Key Keycode { get; set; }
```

引擎名：`keycode`

Latin label printed on the key in the current keyboard layout, which corresponds to one of the [`Key`](Key.md) constants. Key codes are meant for shortcuts expressed with a standard Latin keyboard, such as Ctrl + S for a "Save" shortcut.

To get a human-readable representation of the `InputEventKey`, use `OS.get_keycode_string(event.keycode)` where `event` is the `InputEventKey`.

```csharp
+-----+ +-----+
| Q   | | Q   | - "Q" - keycode
|   Й | |  ض | - "Й" and "ض" - key_label
+-----+ +-----+

```

### Location

```csharp
public KeyLocation Location { get; set; }
```

引擎名：`location`

Represents the location of a key which has both left and right versions, such as Shift or Alt.

### PhysicalKeycode

```csharp
public Key PhysicalKeycode { get; set; }
```

引擎名：`physical_keycode`

Represents the physical location of a key on the 101/102-key US QWERTY keyboard, which corresponds to one of the [`Key`](Key.md) constants. Physical key codes meant for game input, such as WASD movement, where only the location of the keys is important.

To get a human-readable representation of the `InputEventKey`, use [`OS.GetKeycodeString`](OS.md) in combination with [`DisplayServer.KeyboardGetKeycodeFromPhysical`](DisplayServer.md) or [`DisplayServer.KeyboardGetLabelFromPhysical`](DisplayServer.md):

```csharp
public override void _Input(InputEvent @event)
{
if (@event is InputEventKey inputEventKey)
{
var keycode = DisplayServer.KeyboardGetKeycodeFromPhysical(inputEventKey.PhysicalKeycode);
var label = DisplayServer.KeyboardGetLabelFromPhysical(inputEventKey.PhysicalKeycode);
GD.Print(OS.GetKeycodeString(keycode));
GD.Print(OS.GetKeycodeString(label));
}
}

```

### Pressed

```csharp
public bool Pressed { get; set; }
```

引擎名：`pressed`

If `true`, the key's state is pressed. If `false`, the key's state is released.

### Unicode

```csharp
public long Unicode { get; set; }
```

引擎名：`unicode`

The key Unicode character code (when relevant), shifted by modifier keys. Unicode character codes for composite characters and complex scripts may not be available unless IME input mode is active. See [`Window.SetImeActive`](Window.md) for more information. Unicode character codes are meant for text input.

**Note:** This property is set by the engine only for a pressed event. If the event is sent by an IME or a virtual keyboard, no corresponding key released event is sent.

## 方法

### AsTextKeyLabel

```csharp
public string AsTextKeyLabel()
```

引擎名：`as_text_key_label`

Returns a `System.String` representation of the event's `KeyLabel` and modifiers.

### AsTextKeycode

```csharp
public string AsTextKeycode()
```

引擎名：`as_text_keycode`

Returns a `System.String` representation of the event's `Keycode` and modifiers.

### AsTextLocation

```csharp
public string AsTextLocation()
```

引擎名：`as_text_location`

Returns a `System.String` representation of the event's `Location`. This will be a blank string if the event is not specific to a location.

### AsTextPhysicalKeycode

```csharp
public string AsTextPhysicalKeycode()
```

引擎名：`as_text_physical_keycode`

Returns a `System.String` representation of the event's `PhysicalKeycode` and modifiers.

### GetKeyLabel

```csharp
public Key GetKeyLabel()
```

引擎名：`get_key_label`

### GetKeyLabelWithModifiers

```csharp
public Key GetKeyLabelWithModifiers()
```

引擎名：`get_key_label_with_modifiers`

Returns the localized key label combined with modifier keys such as Shift or Alt. See also [`InputEventWithModifiers`](InputEventWithModifiers.md).

To get a human-readable representation of the `InputEventKey` with modifiers, use `OS.get_keycode_string(event.get_key_label_with_modifiers())` where `event` is the `InputEventKey`.

### GetKeycode

```csharp
public Key GetKeycode()
```

引擎名：`get_keycode`

### GetKeycodeWithModifiers

```csharp
public Key GetKeycodeWithModifiers()
```

引擎名：`get_keycode_with_modifiers`

Returns the Latin keycode combined with modifier keys such as Shift or Alt. See also [`InputEventWithModifiers`](InputEventWithModifiers.md).

To get a human-readable representation of the `InputEventKey` with modifiers, use `OS.get_keycode_string(event.get_keycode_with_modifiers())` where `event` is the `InputEventKey`.

### GetLocation

```csharp
public KeyLocation GetLocation()
```

引擎名：`get_location`

### GetPhysicalKeycode

```csharp
public Key GetPhysicalKeycode()
```

引擎名：`get_physical_keycode`

### GetPhysicalKeycodeWithModifiers

```csharp
public Key GetPhysicalKeycodeWithModifiers()
```

引擎名：`get_physical_keycode_with_modifiers`

Returns the physical keycode combined with modifier keys such as Shift or Alt. See also [`InputEventWithModifiers`](InputEventWithModifiers.md).

To get a human-readable representation of the `InputEventKey` with modifiers, use `OS.get_keycode_string(event.get_physical_keycode_with_modifiers())` where `event` is the `InputEventKey`.

### GetUnicode

```csharp
public long GetUnicode()
```

引擎名：`get_unicode`

### SetEcho

```csharp
public void SetEcho(bool echo)
```

引擎名：`set_echo`

### SetKeyLabel

```csharp
public void SetKeyLabel(Key keyLabel)
```

引擎名：`set_key_label`

### SetKeycode

```csharp
public void SetKeycode(Key keycode)
```

引擎名：`set_keycode`

### SetLocation

```csharp
public void SetLocation(KeyLocation location)
```

引擎名：`set_location`

### SetPhysicalKeycode

```csharp
public void SetPhysicalKeycode(Key physicalKeycode)
```

引擎名：`set_physical_keycode`

### SetPressed

```csharp
public void SetPressed(bool pressed)
```

引擎名：`set_pressed`

### SetUnicode

```csharp
public void SetUnicode(long unicode)
```

引擎名：`set_unicode`
