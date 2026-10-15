<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventMouseMotion

命名空间 `Godot`；class；继承 [`InputEventMouse`](InputEventMouse.md) → [`InputEventWithModifiers`](InputEventWithModifiers.md) → [`InputEventFromWindow`](InputEventFromWindow.md) → [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Stores information about a mouse or a pen motion. This includes relative position, absolute position, and velocity. See [`Node._Input()`](Node.md).

**Note:** By default, this event is only emitted once per frame rendered at most. If you need more precise input reporting, set [`Input.UseAccumulatedInput`](Input.md) to `false` to make events emitted as often as possible. If you use InputEventMouseMotion to draw lines, consider using [`Geometry2D.BresenhamLine`](Geometry2D.md) as well to avoid visible gaps in lines if the user is moving the mouse quickly.

**Note:** This event may be emitted even when the mouse hasn't moved, either by the operating system or by Godot itself. If you really need to know if the mouse has moved (e.g. to suppress displaying a tooltip), you should check that `relative.is_zero_approx()` is `false`.

## 构造函数

### InputEventMouseMotion

```csharp
public InputEventMouseMotion()
```

## 属性

### PenInverted

```csharp
public bool PenInverted { get; set; }
```

引擎名：`pen_inverted`

Returns `true` when using the eraser end of a stylus pen.

**Note:** This property is implemented on Linux, macOS and Windows.

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

The mouse position relative to the previous position (position at the last frame).

**Note:** Since `InputEventMouseMotion` may only be emitted when the mouse moves, it is not possible to reliably detect when the mouse has stopped moving by checking this property. A separate, short timer may be necessary.

**Note:** `Relative` is automatically scaled according to the content scale factor, which is defined by the project's stretch mode settings. This means mouse sensitivity will appear different depending on resolution when using `Relative` in a script that handles mouse aiming with the [`Input.MouseModeEnum.Captured`](Input.md) mouse mode. To avoid this, use `ScreenRelative` instead.

### ScreenRelative

```csharp
public Vector2 ScreenRelative { get; set; }
```

引擎名：`screen_relative`

The unscaled mouse position relative to the previous position in the coordinate system of the screen (position at the last frame).

**Note:** Since `InputEventMouseMotion` may only be emitted when the mouse moves, it is not possible to reliably detect when the mouse has stopped moving by checking this property. A separate, short timer may be necessary.

**Note:** This coordinate is *not* scaled according to the content scale factor or calls to [`InputEvent.XformedBy`](InputEvent.md). This should be preferred over `Relative` for mouse aiming when using the [`Input.MouseModeEnum.Captured`](Input.md) mouse mode, regardless of the project's stretch mode.

### ScreenVelocity

```csharp
public Vector2 ScreenVelocity { get; set; }
```

引擎名：`screen_velocity`

The unscaled mouse velocity in pixels per second in screen coordinates. This velocity is *not* scaled according to the content scale factor or calls to [`InputEvent.XformedBy`](InputEvent.md).

**Note:** In [`Input.MouseModeEnum.Captured`](Input.md) mode, `ScreenVelocity` returns `(0, 0)` because the mouse cursor is hidden and locked. Use `ScreenRelative` for mouse aiming using the [`Input.MouseModeEnum.Captured`](Input.md) mouse mode.

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

The mouse velocity in pixels per second.

**Note:** `Velocity` is automatically scaled according to the content scale factor, which is defined by the project's stretch mode settings. That means mouse sensitivity may appear different depending on resolution.

**Note:** In [`Input.MouseModeEnum.Captured`](Input.md) mode, `Velocity` returns `(0, 0)` because the mouse cursor is hidden and locked. Use `ScreenRelative` for mouse aiming using the [`Input.MouseModeEnum.Captured`](Input.md) mouse mode.

## 方法

### GetPenInverted

```csharp
public bool GetPenInverted()
```

引擎名：`get_pen_inverted`

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

### SetPenInverted

```csharp
public void SetPenInverted(bool penInverted)
```

引擎名：`set_pen_inverted`

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
