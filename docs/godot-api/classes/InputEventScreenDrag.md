<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventScreenDrag

命名空间 `Godot`；class；继承 [`InputEventFromWindow`](InputEventFromWindow.md) → [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Stores information about screen drag events. See [`Node._Input()`](Node.md).

## 构造函数

### InputEventScreenDrag

```csharp
public InputEventScreenDrag()
```

## 属性

### Index

```csharp
public int Index { get; set; }
```

引擎名：`index`

The drag event index in the case of a multi-drag event.

### PenInverted

```csharp
public bool PenInverted { get; set; }
```

引擎名：`pen_inverted`

Returns `true` when using the eraser end of a stylus pen.

### Position

```csharp
public Vector2 Position { get; set; }
```

引擎名：`position`

The drag position in the viewport the node is in, using the coordinate system of this viewport.

### Pressure

```csharp
public float Pressure { get; set; }
```

引擎名：`pressure`

Represents the pressure the user puts on the pen. Ranges from `0.0` to `1.0`.

### Relative

```csharp
public Vector2 Relative { get; set; }
```

引擎名：`relative`

The drag position relative to the previous position (position at the last frame).

**Note:** `Relative` is automatically scaled according to the content scale factor, which is defined by the project's stretch mode settings. This means touch sensitivity will appear different depending on resolution when using `Relative` in a script that handles touch aiming. To avoid this, use `ScreenRelative` instead.

### ScreenRelative

```csharp
public Vector2 ScreenRelative { get; set; }
```

引擎名：`screen_relative`

The unscaled drag position relative to the previous position in screen coordinates (position at the last frame). This position is *not* scaled according to the content scale factor or calls to [`InputEvent.XformedBy`](InputEvent.md). This should be preferred over `Relative` for touch aiming regardless of the project's stretch mode.

### ScreenVelocity

```csharp
public Vector2 ScreenVelocity { get; set; }
```

引擎名：`screen_velocity`

The unscaled drag velocity in pixels per second in screen coordinates. This velocity is *not* scaled according to the content scale factor or calls to [`InputEvent.XformedBy`](InputEvent.md). This should be preferred over `Velocity` for touch aiming regardless of the project's stretch mode.

### Tilt

```csharp
public Vector2 Tilt { get; set; }
```

引擎名：`tilt`

Represents the angles of tilt of the pen. Positive X-coordinate value indicates a tilt to the right. Positive Y-coordinate value indicates a tilt toward the user. Ranges from `-1.0` to `1.0` for both axes.

### Velocity

```csharp
public Vector2 Velocity { get; set; }
```

引擎名：`velocity`

The drag velocity.

**Note:** `Velocity` is automatically scaled according to the content scale factor, which is defined by the project's stretch mode settings. This means touch sensitivity will appear different depending on resolution when using `Velocity` in a script that handles touch aiming. To avoid this, use `ScreenVelocity` instead.

## 方法

### GetIndex

```csharp
public int GetIndex()
```

引擎名：`get_index`

### GetPenInverted

```csharp
public bool GetPenInverted()
```

引擎名：`get_pen_inverted`

### GetPosition

```csharp
public Vector2 GetPosition()
```

引擎名：`get_position`

### GetPressure

```csharp
public float GetPressure()
```

引擎名：`get_pressure`

### GetRelative

```csharp
public Vector2 GetRelative()
```

引擎名：`get_relative`

### GetScreenRelative

```csharp
public Vector2 GetScreenRelative()
```

引擎名：`get_screen_relative`

### GetScreenVelocity

```csharp
public Vector2 GetScreenVelocity()
```

引擎名：`get_screen_velocity`

### GetTilt

```csharp
public Vector2 GetTilt()
```

引擎名：`get_tilt`

### GetVelocity

```csharp
public Vector2 GetVelocity()
```

引擎名：`get_velocity`

### SetIndex

```csharp
public void SetIndex(int index)
```

引擎名：`set_index`

### SetPenInverted

```csharp
public void SetPenInverted(bool penInverted)
```

引擎名：`set_pen_inverted`

### SetPosition

```csharp
public void SetPosition(Vector2 position)
```

引擎名：`set_position`

### SetPressure

```csharp
public void SetPressure(float pressure)
```

引擎名：`set_pressure`

### SetRelative

```csharp
public void SetRelative(Vector2 relative)
```

引擎名：`set_relative`

### SetScreenRelative

```csharp
public void SetScreenRelative(Vector2 relative)
```

引擎名：`set_screen_relative`

### SetScreenVelocity

```csharp
public void SetScreenVelocity(Vector2 velocity)
```

引擎名：`set_screen_velocity`

### SetTilt

```csharp
public void SetTilt(Vector2 tilt)
```

引擎名：`set_tilt`

### SetVelocity

```csharp
public void SetVelocity(Vector2 velocity)
```

引擎名：`set_velocity`
