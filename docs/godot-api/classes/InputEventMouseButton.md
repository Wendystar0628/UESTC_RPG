<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventMouseButton

命名空间 `Godot`；class；继承 [`InputEventMouse`](InputEventMouse.md) → [`InputEventWithModifiers`](InputEventWithModifiers.md) → [`InputEventFromWindow`](InputEventFromWindow.md) → [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Stores information about mouse click events. See [`Node._Input()`](Node.md).

**Note:** On Wear OS devices, rotary input is mapped to [`MouseButton.WheelUp`](MouseButton.md) and [`MouseButton.WheelDown`](MouseButton.md). This can be changed to [`MouseButton.WheelLeft`](MouseButton.md) and [`MouseButton.WheelRight`](MouseButton.md) with the `ProjectSettings.input_devices/pointing/android/rotary_input_scroll_axis` setting.

## 构造函数

### InputEventMouseButton

```csharp
public InputEventMouseButton()
```

## 属性

### ButtonIndex

```csharp
public MouseButton ButtonIndex { get; set; }
```

引擎名：`button_index`

The mouse button identifier, one of the [`MouseButton`](MouseButton.md) button or button wheel constants.

### Canceled

```csharp
public bool Canceled { get; set; }
```

引擎名：`canceled`

If `true`, the mouse button event has been canceled.

### DoubleClick

```csharp
public bool DoubleClick { get; set; }
```

引擎名：`double_click`

If `true`, the mouse button's state is a double-click.

### Factor

```csharp
public float Factor { get; set; }
```

引擎名：`factor`

The amount (or delta) of the event. When used for high-precision scroll events, this indicates the scroll amount (vertical or horizontal). This is only supported on some platforms; the reported sensitivity varies depending on the platform. May be `0` if not supported.

### Pressed

```csharp
public bool Pressed { get; set; }
```

引擎名：`pressed`

If `true`, the mouse button's state is pressed. If `false`, the mouse button's state is released.

## 方法

### GetButtonIndex

```csharp
public MouseButton GetButtonIndex()
```

引擎名：`get_button_index`

### GetFactor

```csharp
public float GetFactor()
```

引擎名：`get_factor`

### IsDoubleClick

```csharp
public bool IsDoubleClick()
```

引擎名：`is_double_click`

### SetButtonIndex

```csharp
public void SetButtonIndex(MouseButton buttonIndex)
```

引擎名：`set_button_index`

### SetCanceled

```csharp
public void SetCanceled(bool canceled)
```

引擎名：`set_canceled`

### SetDoubleClick

```csharp
public void SetDoubleClick(bool doubleClick)
```

引擎名：`set_double_click`

### SetFactor

```csharp
public void SetFactor(float factor)
```

引擎名：`set_factor`

### SetPressed

```csharp
public void SetPressed(bool pressed)
```

引擎名：`set_pressed`
