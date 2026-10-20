<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventJoypadMotion

命名空间 `Godot`；class；继承 [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Stores information about joystick motions. One `InputEventJoypadMotion` represents one axis at a time. For gamepad buttons, see [`InputEventJoypadButton`](InputEventJoypadButton.md).

## 构造函数

### InputEventJoypadMotion

```csharp
public InputEventJoypadMotion()
```

## 属性

### Axis

```csharp
public JoyAxis Axis { get; set; }
```

引擎名：`axis`

Axis identifier.

### AxisValue

```csharp
public float AxisValue { get; set; }
```

引擎名：`axis_value`

Current position of the joystick on the given axis. The value ranges from `-1.0` to `1.0`. A value of `0` means the axis is in its resting position.

## 方法

### GetAxis

```csharp
public JoyAxis GetAxis()
```

引擎名：`get_axis`

### GetAxisValue

```csharp
public float GetAxisValue()
```

引擎名：`get_axis_value`

### SetAxis

```csharp
public void SetAxis(JoyAxis axis)
```

引擎名：`set_axis`

### SetAxisValue

```csharp
public void SetAxisValue(float axisValue)
```

引擎名：`set_axis_value`
