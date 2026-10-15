<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Parallax2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A `Parallax2D` is used to create a parallax effect. It can move at a different speed relative to the camera movement using `ScrollScale`. This creates an illusion of depth in a 2D game. If manual scrolling is desired, the [`Camera2D`](Camera2D.md) position can be ignored with `IgnoreCameraScroll`.

**Note:** Any changes to this node's position made after it enters the scene tree will be overridden if `IgnoreCameraScroll` is `false` or `ScreenOffset` is modified.

## 构造函数

### Parallax2D

```csharp
public Parallax2D()
```

## 属性

### Autoscroll

```csharp
public Vector2 Autoscroll { get; set; }
```

引擎名：`autoscroll`

Velocity at which the offset scrolls automatically, in pixels per second.

### FollowViewport

```csharp
public bool FollowViewport { get; set; }
```

引擎名：`follow_viewport`

If `true`, this `Parallax2D` is offset by the current camera's position. If the `Parallax2D` is in a [`CanvasLayer`](CanvasLayer.md) separate from the current camera, it may be desired to match the value with [`CanvasLayer.FollowViewportEnabled`](CanvasLayer.md).

### IgnoreCameraScroll

```csharp
public bool IgnoreCameraScroll { get; set; }
```

引擎名：`ignore_camera_scroll`

If `true`, `Parallax2D`'s position is not affected by the position of the camera.

### LimitBegin

```csharp
public Vector2 LimitBegin { get; set; }
```

引擎名：`limit_begin`

Top-left limits for scrolling to begin. If the camera is outside of this limit, the `Parallax2D` stops scrolling. Must be lower than `LimitEnd` minus the viewport size to work.

### LimitEnd

```csharp
public Vector2 LimitEnd { get; set; }
```

引擎名：`limit_end`

Bottom-right limits for scrolling to end. If the camera is outside of this limit, the `Parallax2D` will stop scrolling. Must be higher than `LimitBegin` and the viewport size combined to work.

### RepeatSize

```csharp
public Vector2 RepeatSize { get; set; }
```

引擎名：`repeat_size`

Repeats the [`Texture2D`](Texture2D.md) of each of this node's children and offsets them by this value. When scrolling, the node's position loops, giving the illusion of an infinite scrolling background if the values are larger than the screen size. If an axis is set to `0`, the [`Texture2D`](Texture2D.md) will not be repeated.

### RepeatTimes

```csharp
public int RepeatTimes { get; set; }
```

引擎名：`repeat_times`

Overrides the amount of times the texture repeats. Each texture copy spreads evenly from the original by `RepeatSize`. Useful for when zooming out with a camera.

### ScreenOffset

```csharp
public Vector2 ScreenOffset { get; set; }
```

引擎名：`screen_offset`

Offset used to scroll this `Parallax2D`. This value is updated automatically unless `IgnoreCameraScroll` is `true`.

### ScrollOffset

```csharp
public Vector2 ScrollOffset { get; set; }
```

引擎名：`scroll_offset`

The `Parallax2D`'s offset. Similar to `ScreenOffset` and [`Node2D.Position`](Node2D.md), but will not be overridden.

**Note:** Values will loop if `RepeatSize` is set higher than `0`.

### ScrollScale

```csharp
public Vector2 ScrollScale { get; set; }
```

引擎名：`scroll_scale`

Multiplier to the final `Parallax2D`'s offset. Can be used to simulate distance from the camera.

For example, a value of `1` scrolls at the same speed as the camera. A value greater than `1` scrolls faster, making objects appear closer. Less than `1` scrolls slower, making objects appear further, and a value of `0` stops the objects completely.

## 方法

### GetAutoscroll

```csharp
public Vector2 GetAutoscroll()
```

引擎名：`get_autoscroll`

### GetFollowViewport

```csharp
public bool GetFollowViewport()
```

引擎名：`get_follow_viewport`

### GetLimitBegin

```csharp
public Vector2 GetLimitBegin()
```

引擎名：`get_limit_begin`

### GetLimitEnd

```csharp
public Vector2 GetLimitEnd()
```

引擎名：`get_limit_end`

### GetRepeatSize

```csharp
public Vector2 GetRepeatSize()
```

引擎名：`get_repeat_size`

### GetRepeatTimes

```csharp
public int GetRepeatTimes()
```

引擎名：`get_repeat_times`

### GetScreenOffset

```csharp
public Vector2 GetScreenOffset()
```

引擎名：`get_screen_offset`

### GetScrollOffset

```csharp
public Vector2 GetScrollOffset()
```

引擎名：`get_scroll_offset`

### GetScrollScale

```csharp
public Vector2 GetScrollScale()
```

引擎名：`get_scroll_scale`

### IsIgnoreCameraScroll

```csharp
public bool IsIgnoreCameraScroll()
```

引擎名：`is_ignore_camera_scroll`

### SetAutoscroll

```csharp
public void SetAutoscroll(Vector2 autoscroll)
```

引擎名：`set_autoscroll`

### SetFollowViewport

```csharp
public void SetFollowViewport(bool follow)
```

引擎名：`set_follow_viewport`

### SetIgnoreCameraScroll

```csharp
public void SetIgnoreCameraScroll(bool ignore)
```

引擎名：`set_ignore_camera_scroll`

### SetLimitBegin

```csharp
public void SetLimitBegin(Vector2 offset)
```

引擎名：`set_limit_begin`

### SetLimitEnd

```csharp
public void SetLimitEnd(Vector2 offset)
```

引擎名：`set_limit_end`

### SetRepeatSize

```csharp
public void SetRepeatSize(Vector2 repeatSize)
```

引擎名：`set_repeat_size`

### SetRepeatTimes

```csharp
public void SetRepeatTimes(int repeatTimes)
```

引擎名：`set_repeat_times`

### SetScreenOffset

```csharp
public void SetScreenOffset(Vector2 offset)
```

引擎名：`set_screen_offset`

### SetScrollOffset

```csharp
public void SetScrollOffset(Vector2 offset)
```

引擎名：`set_scroll_offset`

### SetScrollScale

```csharp
public void SetScrollScale(Vector2 scale)
```

引擎名：`set_scroll_scale`
