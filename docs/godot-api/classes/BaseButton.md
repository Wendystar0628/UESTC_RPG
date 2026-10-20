<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# BaseButton

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`BaseButton` is an abstract base class for GUI buttons. It doesn't display anything by itself.

## 构造函数

### BaseButton

```csharp
public BaseButton()
```

## 属性

### ActionMode

```csharp
public BaseButton.ActionModeEnum ActionMode { get; set; }
```

引擎名：`action_mode`

Determines when the button is considered clicked.

### ButtonGroup

```csharp
public ButtonGroup ButtonGroup { get; set; }
```

引擎名：`button_group`

The [`ButtonGroup`](ButtonGroup.md) associated with the button. Not to be confused with node groups.

**Note:** The button will be configured as a radio button if a [`ButtonGroup`](ButtonGroup.md) is assigned to it.

### ButtonMask

```csharp
public MouseButtonMask ButtonMask { get; set; }
```

引擎名：`button_mask`

Binary mask to choose which mouse buttons this button will respond to.

To allow both left-click and right-click, use `MOUSE_BUTTON_MASK_LEFT | MOUSE_BUTTON_MASK_RIGHT`.

### ButtonPressed

```csharp
public bool ButtonPressed { get; set; }
```

引擎名：`button_pressed`

If `true`, the button's state is pressed. Means the button is pressed down or toggled (if `ToggleMode` is active). Only works if `ToggleMode` is `true`.

**Note:** Changing the value of `ButtonPressed` will result in `Toggled` to be emitted. If you want to change the pressed state without emitting that signal, use `SetPressedNoSignal()`.

### Disabled

```csharp
public bool Disabled { get; set; }
```

引擎名：`disabled`

If `true`, the button is in disabled state and can't be clicked or toggled.

**Note:** If the button is disabled while held down, `ButtonUp` will be emitted.

### KeepPressedOutside

```csharp
public bool KeepPressedOutside { get; set; }
```

引擎名：`keep_pressed_outside`

If `true`, the button stays pressed when moving the cursor outside the button while pressing it.

**Note:** This property only affects the button's visual appearance. Signals will be emitted at the same moment regardless of this property's value.

### Shortcut

```csharp
public Shortcut Shortcut { get; set; }
```

引擎名：`shortcut`

[`Shortcut`](Shortcut.md) associated to the button.

### ShortcutFeedback

```csharp
public bool ShortcutFeedback { get; set; }
```

引擎名：`shortcut_feedback`

If `true`, the button will highlight for a short amount of time when its shortcut is activated. If `false` and `ToggleMode` is `false`, the shortcut will activate without any visual feedback.

### ShortcutInTooltip

```csharp
public bool ShortcutInTooltip { get; set; }
```

引擎名：`shortcut_in_tooltip`

If `true`, the button will add information about its shortcut in the tooltip. This includes the shortcut's events and its [`Resource.ResourceName`](Resource.md). If both events and name are empty, the shortcut will not be included.

**Note:** This property does nothing when the tooltip control is customized using [`Control._MakeCustomTooltip()`](Control.md).

### ToggleMode

```csharp
public bool ToggleMode { get; set; }
```

引擎名：`toggle_mode`

If `true`, the button is in toggle mode. Makes the button flip state between pressed and unpressed each time its area is clicked.

## 方法

### GetActionMode

```csharp
public BaseButton.ActionModeEnum GetActionMode()
```

引擎名：`get_action_mode`

### GetButtonGroup

```csharp
public ButtonGroup GetButtonGroup()
```

引擎名：`get_button_group`

### GetButtonMask

```csharp
public MouseButtonMask GetButtonMask()
```

引擎名：`get_button_mask`

### GetDrawMode

```csharp
public BaseButton.DrawMode GetDrawMode()
```

引擎名：`get_draw_mode`

Returns the visual state used to draw the button. This is useful mainly when implementing your own draw code by either overriding _draw() or connecting to "draw" signal. The visual state of the button is defined by the [`BaseButton.DrawMode`](BaseButton.md) enum.

### GetShortcut

```csharp
public Shortcut GetShortcut()
```

引擎名：`get_shortcut`

### IsDisabled

```csharp
public bool IsDisabled()
```

引擎名：`is_disabled`

### IsHovered

```csharp
public bool IsHovered()
```

引擎名：`is_hovered`

Returns `true` if the mouse has entered the button and has not left it yet.

### IsKeepPressedOutside

```csharp
public bool IsKeepPressedOutside()
```

引擎名：`is_keep_pressed_outside`

### IsPressed

```csharp
public bool IsPressed()
```

引擎名：`is_pressed`

### IsShortcutFeedback

```csharp
public bool IsShortcutFeedback()
```

引擎名：`is_shortcut_feedback`

### IsShortcutInTooltipEnabled

```csharp
public bool IsShortcutInTooltipEnabled()
```

引擎名：`is_shortcut_in_tooltip_enabled`

### IsToggleMode

```csharp
public bool IsToggleMode()
```

引擎名：`is_toggle_mode`

### SetActionMode

```csharp
public void SetActionMode(BaseButton.ActionModeEnum mode)
```

引擎名：`set_action_mode`

### SetButtonGroup

```csharp
public void SetButtonGroup(ButtonGroup buttonGroup)
```

引擎名：`set_button_group`

### SetButtonMask

```csharp
public void SetButtonMask(MouseButtonMask mask)
```

引擎名：`set_button_mask`

### SetDisabled

```csharp
public void SetDisabled(bool disabled)
```

引擎名：`set_disabled`

### SetKeepPressedOutside

```csharp
public void SetKeepPressedOutside(bool enabled)
```

引擎名：`set_keep_pressed_outside`

### SetPressed

```csharp
public void SetPressed(bool pressed)
```

引擎名：`set_pressed`

### SetPressedNoSignal

```csharp
public void SetPressedNoSignal(bool pressed)
```

引擎名：`set_pressed_no_signal`

Changes the `ButtonPressed` state of the button, without emitting `Toggled`. Use when you just want to change the state of the button without sending the pressed event (e.g. when initializing scene). Only works if `ToggleMode` is `true`.

**Note:** This method doesn't unpress other buttons in `ButtonGroup`.

### SetShortcut

```csharp
public void SetShortcut(Shortcut shortcut)
```

引擎名：`set_shortcut`

### SetShortcutFeedback

```csharp
public void SetShortcutFeedback(bool enabled)
```

引擎名：`set_shortcut_feedback`

### SetShortcutInTooltip

```csharp
public void SetShortcutInTooltip(bool enabled)
```

引擎名：`set_shortcut_in_tooltip`

### SetToggleMode

```csharp
public void SetToggleMode(bool enabled)
```

引擎名：`set_toggle_mode`

### _Pressed

```csharp
public virtual void _Pressed()
```

引擎名：`_pressed`

Called when the button is pressed. If you need to know the button's pressed state (and `ToggleMode` is active), use `_Toggled()` instead.

### _Toggled

```csharp
public virtual void _Toggled(bool toggledOn)
```

引擎名：`_toggled`

Called when the button is toggled (only if `ToggleMode` is active).

## 事件（信号）

### ButtonDown

```csharp
public event Action ButtonDown
```

引擎名：`button_down`

Emitted when the button starts being held down.

### ButtonUp

```csharp
public event Action ButtonUp
```

引擎名：`button_up`

Emitted when the button stops being held down.

### Pressed

```csharp
public event Action Pressed
```

引擎名：`pressed`

Emitted when the button is toggled or pressed. This is on `ButtonDown` if `ActionMode` is [`BaseButton.ActionModeEnum.Press`](BaseButton.md) and on `ButtonUp` otherwise.

If you need to know the button's pressed state (and `ToggleMode` is active), use `Toggled` instead.

### Toggled

```csharp
public event BaseButton.ToggledEventHandler Toggled
```

引擎名：`toggled`

Emitted when the button was just toggled between pressed and normal states (only if `ToggleMode` is active). The new state is contained in the `toggledOn` argument.

## ActionModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Press = 0`：Require just a press to consider the button clicked.
- `Release = 1`：Require a press and a subsequent release before considering the button clicked.

## DrawMode

命名空间 `Godot`；enum

### 枚举值

- `Normal = 0`：The normal state (i.e.
- `Pressed = 1`：The state of buttons are pressed.
- `Hover = 2`：The state of buttons are hovered.
- `Disabled = 3`：The state of buttons are disabled.
- `HoverPressed = 4`：The state of buttons are both hovered and pressed.

## ToggledEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`BaseButton.Toggled`](BaseButton.md) event of a [`BaseButton`](BaseButton.md) class.

### 签名

#### Invoke

```csharp
delegate void ToggledEventHandler(bool toggledOn)
```
