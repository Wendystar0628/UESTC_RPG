<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ButtonGroup

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A group of [`BaseButton`](BaseButton.md)-derived buttons. The buttons in a `ButtonGroup` are treated like radio buttons: No more than one button can be pressed at a time. Some types of buttons (such as [`CheckBox`](CheckBox.md)) may have a special appearance in this state.

Every member of a `ButtonGroup` should have [`BaseButton.ToggleMode`](BaseButton.md) set to `true`.

## 构造函数

### ButtonGroup

```csharp
public ButtonGroup()
```

## 属性

### AllowUnpress

```csharp
public bool AllowUnpress { get; set; }
```

引擎名：`allow_unpress`

If `true`, it is possible to unpress all buttons in this `ButtonGroup`.

## 方法

### GetButtons

```csharp
public Array<BaseButton> GetButtons()
```

引擎名：`get_buttons`

Returns an [`Collections.Array`](Collections.Array.md) of [`Button`](Button.md)s who have this as their `ButtonGroup` (see [`BaseButton.ButtonGroup`](BaseButton.md)).

### GetPressedButton

```csharp
public BaseButton GetPressedButton()
```

引擎名：`get_pressed_button`

Returns the current pressed button.

### IsAllowUnpress

```csharp
public bool IsAllowUnpress()
```

引擎名：`is_allow_unpress`

### SetAllowUnpress

```csharp
public void SetAllowUnpress(bool enabled)
```

引擎名：`set_allow_unpress`

## 事件（信号）

### Pressed

```csharp
public event ButtonGroup.PressedEventHandler Pressed
```

引擎名：`pressed`

Emitted when one of the buttons of the group is pressed.

## PressedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`ButtonGroup.Pressed`](ButtonGroup.md) event of a [`ButtonGroup`](ButtonGroup.md) class.

### 签名

#### Invoke

```csharp
delegate void PressedEventHandler(BaseButton button)
```
