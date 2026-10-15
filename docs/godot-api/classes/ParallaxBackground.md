<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ParallaxBackground

命名空间 `Godot`；class；继承 [`CanvasLayer`](CanvasLayer.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A ParallaxBackground uses one or more [`ParallaxLayer`](ParallaxLayer.md) child nodes to create a parallax effect. Each [`ParallaxLayer`](ParallaxLayer.md) can move at a different speed using [`ParallaxLayer.MotionOffset`](ParallaxLayer.md). This creates an illusion of depth in a 2D game. If not used with a [`Camera2D`](Camera2D.md), you must manually calculate the `ScrollOffset`.

**Note:** Each `ParallaxBackground` is drawn on one specific [`Viewport`](Viewport.md) and cannot be shared between multiple [`Viewport`](Viewport.md)s, see [`CanvasLayer.CustomViewport`](CanvasLayer.md). When using multiple [`Viewport`](Viewport.md)s, for example in a split-screen game, you need create an individual `ParallaxBackground` for each [`Viewport`](Viewport.md) you want it to be drawn on.

## 构造函数

### ParallaxBackground

```csharp
public ParallaxBackground()
```

## 属性

### ScrollBaseOffset

```csharp
public Vector2 ScrollBaseOffset { get; set; }
```

引擎名：`scroll_base_offset`

The base position offset for all [`ParallaxLayer`](ParallaxLayer.md) children.

### ScrollBaseScale

```csharp
public Vector2 ScrollBaseScale { get; set; }
```

引擎名：`scroll_base_scale`

The base motion scale for all [`ParallaxLayer`](ParallaxLayer.md) children.

### ScrollIgnoreCameraZoom

```csharp
public bool ScrollIgnoreCameraZoom { get; set; }
```

引擎名：`scroll_ignore_camera_zoom`

If `true`, elements in [`ParallaxLayer`](ParallaxLayer.md) child aren't affected by the zoom level of the camera.

### ScrollLimitBegin

```csharp
public Vector2 ScrollLimitBegin { get; set; }
```

引擎名：`scroll_limit_begin`

Top-left limits for scrolling to begin. If the camera is outside of this limit, the background will stop scrolling. Must be lower than `ScrollLimitEnd` to work.

### ScrollLimitEnd

```csharp
public Vector2 ScrollLimitEnd { get; set; }
```

引擎名：`scroll_limit_end`

Bottom-right limits for scrolling to end. If the camera is outside of this limit, the background will stop scrolling. Must be higher than `ScrollLimitBegin` to work.

### ScrollOffset

```csharp
public Vector2 ScrollOffset { get; set; }
```

引擎名：`scroll_offset`

The ParallaxBackground's scroll value. Calculated automatically when using a [`Camera2D`](Camera2D.md), but can be used to manually manage scrolling when no camera is present.

## 方法

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

### GetScrollBaseOffset

```csharp
public Vector2 GetScrollBaseOffset()
```

引擎名：`get_scroll_base_offset`

### GetScrollBaseScale

```csharp
public Vector2 GetScrollBaseScale()
```

引擎名：`get_scroll_base_scale`

### GetScrollOffset

```csharp
public Vector2 GetScrollOffset()
```

引擎名：`get_scroll_offset`

### IsIgnoreCameraZoom

```csharp
public bool IsIgnoreCameraZoom()
```

引擎名：`is_ignore_camera_zoom`

### SetIgnoreCameraZoom

```csharp
public void SetIgnoreCameraZoom(bool ignore)
```

引擎名：`set_ignore_camera_zoom`

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

### SetScrollBaseOffset

```csharp
public void SetScrollBaseOffset(Vector2 offset)
```

引擎名：`set_scroll_base_offset`

### SetScrollBaseScale

```csharp
public void SetScrollBaseScale(Vector2 scale)
```

引擎名：`set_scroll_base_scale`

### SetScrollOffset

```csharp
public void SetScrollOffset(Vector2 offset)
```

引擎名：`set_scroll_offset`
