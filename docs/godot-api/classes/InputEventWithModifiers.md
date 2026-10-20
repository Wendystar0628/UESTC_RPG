<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventWithModifiers

命名空间 `Godot`；class；继承 [`InputEventFromWindow`](InputEventFromWindow.md) → [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Stores information about mouse, keyboard, and touch gesture input events. This includes information about which modifier keys are pressed, such as Shift or Alt. See [`Node._Input()`](Node.md).

**Note:** Modifier keys are considered modifiers only when used in combination with another key. As a result, their corresponding member variables, such as `CtrlPressed`, will return `false` if the key is pressed on its own.

## 属性

### AltPressed

```csharp
public bool AltPressed { get; set; }
```

引擎名：`alt_pressed`

State of the Alt modifier.

### CommandOrControlAutoremap

```csharp
public bool CommandOrControlAutoremap { get; set; }
```

引擎名：`command_or_control_autoremap`

Automatically use Meta (Cmd) on macOS and Ctrl on other platforms. If `true`, `CtrlPressed` and `MetaPressed` cannot be set.

### CtrlPressed

```csharp
public bool CtrlPressed { get; set; }
```

引擎名：`ctrl_pressed`

State of the Ctrl modifier.

### MetaPressed

```csharp
public bool MetaPressed { get; set; }
```

引擎名：`meta_pressed`

State of the Meta modifier. On Windows and Linux, this represents the Windows key (sometimes called "meta" or "super" on Linux). On macOS, this represents the Command key.

### ShiftPressed

```csharp
public bool ShiftPressed { get; set; }
```

引擎名：`shift_pressed`

State of the Shift modifier.

## 方法

### GetModifiersMask

```csharp
public KeyModifierMask GetModifiersMask()
```

引擎名：`get_modifiers_mask`

Returns the keycode combination of modifier keys.

### IsAltPressed

```csharp
public bool IsAltPressed()
```

引擎名：`is_alt_pressed`

### IsCommandOrControlAutoremap

```csharp
public bool IsCommandOrControlAutoremap()
```

引擎名：`is_command_or_control_autoremap`

### IsCommandOrControlPressed

```csharp
public bool IsCommandOrControlPressed()
```

引擎名：`is_command_or_control_pressed`

On macOS, returns `true` if Meta (Cmd) is pressed.

On other platforms, returns `true` if Ctrl is pressed.

### IsCtrlPressed

```csharp
public bool IsCtrlPressed()
```

引擎名：`is_ctrl_pressed`

### IsMetaPressed

```csharp
public bool IsMetaPressed()
```

引擎名：`is_meta_pressed`

### IsShiftPressed

```csharp
public bool IsShiftPressed()
```

引擎名：`is_shift_pressed`

### SetAltPressed

```csharp
public void SetAltPressed(bool pressed)
```

引擎名：`set_alt_pressed`

### SetCommandOrControlAutoremap

```csharp
public void SetCommandOrControlAutoremap(bool enable)
```

引擎名：`set_command_or_control_autoremap`

### SetCtrlPressed

```csharp
public void SetCtrlPressed(bool pressed)
```

引擎名：`set_ctrl_pressed`

### SetMetaPressed

```csharp
public void SetMetaPressed(bool pressed)
```

引擎名：`set_meta_pressed`

### SetShiftPressed

```csharp
public void SetShiftPressed(bool pressed)
```

引擎名：`set_shift_pressed`
