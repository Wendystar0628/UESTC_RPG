<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventJoypadButton

命名空间 `Godot`；class；继承 [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Input event type for gamepad buttons. For gamepad analog sticks and joysticks, see [`InputEventJoypadMotion`](InputEventJoypadMotion.md).

## 构造函数

### InputEventJoypadButton

```csharp
public InputEventJoypadButton()
```

## 属性

### ButtonIndex

```csharp
public JoyButton ButtonIndex { get; set; }
```

引擎名：`button_index`

Button identifier. One of the [`JoyButton`](JoyButton.md) button constants.

### Pressed

```csharp
public bool Pressed { get; set; }
```

引擎名：`pressed`

If `true`, the button's state is pressed. If `false`, the button's state is released.

### Pressure

```csharp
public float Pressure { get; set; }
```

引擎名：`pressure`

## 方法

### GetButtonIndex

```csharp
public JoyButton GetButtonIndex()
```

引擎名：`get_button_index`

### GetPressure

```csharp
public float GetPressure()
```

引擎名：`get_pressure`

### SetButtonIndex

```csharp
public void SetButtonIndex(JoyButton buttonIndex)
```

引擎名：`set_button_index`

### SetPressed

```csharp
public void SetPressed(bool pressed)
```

引擎名：`set_pressed`

### SetPressure

```csharp
public void SetPressure(float pressure)
```

引擎名：`set_pressure`
