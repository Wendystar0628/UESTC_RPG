<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VirtualJoystick

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A customizable on-screen joystick control designed for touchscreen devices. It allows users to provide directional input by dragging a virtual tip within a defined circular area.

This control can simulate directional actions (see `ActionUp`, `ActionDown`, `ActionLeft`, and `ActionRight`), which are triggered when the joystick is moved in the corresponding directions.

## 构造函数

### VirtualJoystick

```csharp
public VirtualJoystick()
```

## 属性

### ActionDown

```csharp
public StringName ActionDown { get; set; }
```

引擎名：`action_down`

The action to trigger when the joystick is moved down.

### ActionLeft

```csharp
public StringName ActionLeft { get; set; }
```

引擎名：`action_left`

The action to trigger when the joystick is moved left.

### ActionRight

```csharp
public StringName ActionRight { get; set; }
```

引擎名：`action_right`

The action to trigger when the joystick is moved right.

### ActionUp

```csharp
public StringName ActionUp { get; set; }
```

引擎名：`action_up`

The action to trigger when the joystick is moved up.

### ClampzoneRatio

```csharp
public float ClampzoneRatio { get; set; }
```

引擎名：`clampzone_ratio`

The multiplier applied to the joystick's radius that defines the clamp zone.

This zone limits how far the joystick tip can move from its center before being clamped.

A value of `1.0` means the tip can move up to the edge of the joystick's visual size.

In [`VirtualJoystick.JoystickModeEnum.Following`](VirtualJoystick.md) mode, this radius also determines how far the finger can move before the joystick base starts following the touch input.

### DeadzoneRatio

```csharp
public float DeadzoneRatio { get; set; }
```

引擎名：`deadzone_ratio`

The ratio of the joystick size that defines the joystick deadzone. The joystick tip must move beyond this ratio before being considered active.

This deadzone is applied before triggering input actions and affects the joystick's input vector and all related signals.

Note that input actions may also define their own deadzones in the InputMap. If both are set, the joystick deadzone is applied first, followed by the action's deadzone.

By default, this value is `0.0`, meaning the joystick does not apply its own deadzone and relies entirely on the InputMap action deadzones.

### InitialOffsetRatio

```csharp
public Vector2 InitialOffsetRatio { get; set; }
```

引擎名：`initial_offset_ratio`

The initial position of the joystick as a ratio of the control's size. `(0, 0)` is top-left and `(1, 1)` is bottom-right.

### JoystickMode

```csharp
public VirtualJoystick.JoystickModeEnum JoystickMode { get; set; }
```

引擎名：`joystick_mode`

The joystick mode to use.

### JoystickSize

```csharp
public float JoystickSize { get; set; }
```

引擎名：`joystick_size`

The size of the joystick in pixels.

### TipSize

```csharp
public float TipSize { get; set; }
```

引擎名：`tip_size`

The size of the joystick tip in pixels.

### VisibilityMode

```csharp
public VirtualJoystick.VisibilityModeEnum VisibilityMode { get; set; }
```

引擎名：`visibility_mode`

The visibility mode to use.

## 方法

### GetActionDown

```csharp
public StringName GetActionDown()
```

引擎名：`get_action_down`

### GetActionLeft

```csharp
public StringName GetActionLeft()
```

引擎名：`get_action_left`

### GetActionRight

```csharp
public StringName GetActionRight()
```

引擎名：`get_action_right`

### GetActionUp

```csharp
public StringName GetActionUp()
```

引擎名：`get_action_up`

### GetClampzoneRatio

```csharp
public float GetClampzoneRatio()
```

引擎名：`get_clampzone_ratio`

### GetDeadzoneRatio

```csharp
public float GetDeadzoneRatio()
```

引擎名：`get_deadzone_ratio`

### GetInitialOffsetRatio

```csharp
public Vector2 GetInitialOffsetRatio()
```

引擎名：`get_initial_offset_ratio`

### GetJoystickMode

```csharp
public VirtualJoystick.JoystickModeEnum GetJoystickMode()
```

引擎名：`get_joystick_mode`

### GetJoystickSize

```csharp
public float GetJoystickSize()
```

引擎名：`get_joystick_size`

### GetTipSize

```csharp
public float GetTipSize()
```

引擎名：`get_tip_size`

### GetVisibilityMode

```csharp
public VirtualJoystick.VisibilityModeEnum GetVisibilityMode()
```

引擎名：`get_visibility_mode`

### SetActionDown

```csharp
public void SetActionDown(StringName action)
```

引擎名：`set_action_down`

### SetActionLeft

```csharp
public void SetActionLeft(StringName action)
```

引擎名：`set_action_left`

### SetActionRight

```csharp
public void SetActionRight(StringName action)
```

引擎名：`set_action_right`

### SetActionUp

```csharp
public void SetActionUp(StringName action)
```

引擎名：`set_action_up`

### SetClampzoneRatio

```csharp
public void SetClampzoneRatio(float ratio)
```

引擎名：`set_clampzone_ratio`

### SetDeadzoneRatio

```csharp
public void SetDeadzoneRatio(float ratio)
```

引擎名：`set_deadzone_ratio`

### SetInitialOffsetRatio

```csharp
public void SetInitialOffsetRatio(Vector2 ratio)
```

引擎名：`set_initial_offset_ratio`

### SetJoystickMode

```csharp
public void SetJoystickMode(VirtualJoystick.JoystickModeEnum mode)
```

引擎名：`set_joystick_mode`

### SetJoystickSize

```csharp
public void SetJoystickSize(float size)
```

引擎名：`set_joystick_size`

### SetTipSize

```csharp
public void SetTipSize(float size)
```

引擎名：`set_tip_size`

### SetVisibilityMode

```csharp
public void SetVisibilityMode(VirtualJoystick.VisibilityModeEnum mode)
```

引擎名：`set_visibility_mode`

## 事件（信号）

### FlickCanceled

```csharp
public event Action FlickCanceled
```

引擎名：`flick_canceled`

Emitted when the tip enters the deadzone after being outside of it.

### Flicked

```csharp
public event VirtualJoystick.FlickedEventHandler Flicked
```

引擎名：`flicked`

Emitted when the tip moved outside the deadzone and the joystick is released. The `inputVector` contains the last input direction and strength before release. Its length is between `0.0` and `1.0`.

### Pressed

```csharp
public event Action Pressed
```

引擎名：`pressed`

Emitted when the joystick is pressed.

### Released

```csharp
public event VirtualJoystick.ReleasedEventHandler Released
```

引擎名：`released`

Emitted when the joystick is released. The `inputVector` is the final input direction and strength, with a length between `0.0` and `1.0`.

### Tapped

```csharp
public event Action Tapped
```

引擎名：`tapped`

Emitted when the joystick is released without moving the tip.

## FlickedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`VirtualJoystick.Flicked`](VirtualJoystick.md) event of a [`VirtualJoystick`](VirtualJoystick.md) class.

### 签名

#### Invoke

```csharp
delegate void FlickedEventHandler(Vector2 inputVector)
```

## JoystickModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Fixed = 0`：The joystick doesn't move.
- `Dynamic = 1`：The joystick is moved to the initial touch position as long as it's within the joystick's bounds.
- `Following = 2`：The joystick is moved to the initial touch position as long as it's within the joystick's bounds.

## ReleasedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`VirtualJoystick.Released`](VirtualJoystick.md) event of a [`VirtualJoystick`](VirtualJoystick.md) class.

### 签名

#### Invoke

```csharp
delegate void ReleasedEventHandler(Vector2 inputVector)
```

## VisibilityModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Always = 0`：The joystick is always visible.
- `WhenTouched = 1`：The joystick is only visible when being touched.
