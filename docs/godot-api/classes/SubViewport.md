<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SubViewport

命名空间 `Godot`；class；继承 [`Viewport`](Viewport.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`SubViewport` Isolates a rectangular region of a scene to be displayed independently. This can be used, for example, to display UI in 3D space.

**Note:** `SubViewport` is a [`Viewport`](Viewport.md) that isn't a [`Window`](Window.md), i.e. it doesn't draw anything by itself. To display anything, `SubViewport` must have a non-zero size and be either put inside a [`SubViewportContainer`](SubViewportContainer.md) or assigned to a [`ViewportTexture`](ViewportTexture.md).

**Note:** [`InputEvent`](InputEvent.md)s are not passed to a standalone `SubViewport` by default. To ensure [`InputEvent`](InputEvent.md) propagation, a `SubViewport` can be placed inside of a [`SubViewportContainer`](SubViewportContainer.md).

## 构造函数

### SubViewport

```csharp
public SubViewport()
```

## 属性

### RenderTargetClearMode

```csharp
public SubViewport.ClearMode RenderTargetClearMode { get; set; }
```

引擎名：`render_target_clear_mode`

The clear mode when the sub-viewport is used as a render target.

**Note:** This property is intended for 2D usage.

### RenderTargetUpdateMode

```csharp
public SubViewport.UpdateMode RenderTargetUpdateMode { get; set; }
```

引擎名：`render_target_update_mode`

The update mode when the sub-viewport is used as a render target.

### Size

```csharp
public Vector2I Size { get; set; }
```

引擎名：`size`

The width and height of the sub-viewport. Must be set to a value greater than or equal to 2 pixels on both dimensions. Otherwise, nothing will be displayed.

**Note:** If the parent node is a [`SubViewportContainer`](SubViewportContainer.md) and its [`SubViewportContainer.Stretch`](SubViewportContainer.md) is `true`, the viewport size cannot be changed manually.

### Size2DOverride

```csharp
public Vector2I Size2DOverride { get; set; }
```

The 2D size override of the sub-viewport. If either the width or height is `0`, the override is disabled.

### Size2DOverrideStretch

```csharp
public bool Size2DOverrideStretch { get; set; }
```

If `true`, the 2D size override affects stretch as well.

### ViewCount

```csharp
public int ViewCount { get; set; }
```

引擎名：`view_count`

The number of view layers we are rendering to. Set this to `2` to enable stereo rendering.

## 方法

### GetClearMode

```csharp
public SubViewport.ClearMode GetClearMode()
```

引擎名：`get_clear_mode`

### GetSize

```csharp
public Vector2I GetSize()
```

引擎名：`get_size`

### GetSize2DOverride

```csharp
public Vector2I GetSize2DOverride()
```

### GetUpdateMode

```csharp
public SubViewport.UpdateMode GetUpdateMode()
```

引擎名：`get_update_mode`

### GetViewCount

```csharp
public int GetViewCount()
```

引擎名：`get_view_count`

### IsSize2DOverrideStretchEnabled

```csharp
public bool IsSize2DOverrideStretchEnabled()
```

### SetClearMode

```csharp
public void SetClearMode(SubViewport.ClearMode mode)
```

引擎名：`set_clear_mode`

### SetSize

```csharp
public void SetSize(Vector2I size)
```

引擎名：`set_size`

### SetSize2DOverride

```csharp
public void SetSize2DOverride(Vector2I size)
```

### SetSize2DOverrideStretch

```csharp
public void SetSize2DOverrideStretch(bool enable)
```

### SetUpdateMode

```csharp
public void SetUpdateMode(SubViewport.UpdateMode mode)
```

引擎名：`set_update_mode`

### SetViewCount

```csharp
public void SetViewCount(int viewCount)
```

引擎名：`set_view_count`

## ClearMode

命名空间 `Godot`；enum

### 枚举值

- `Always = 0`：Always clear the render target before drawing.
- `Never = 1`：Never clear the render target.
- `Once = 2`：Clear the render target on the next frame, then switch to `Never`.

## UpdateMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Do not update the render target.
- `Once = 1`：Update the render target once, then switch to `Disabled`.
- `WhenVisible = 2`：Update the render target only when it is visible.
- `WhenParentVisible = 3`：Update the render target only when its parent is visible.
- `Always = 4`：Always update the render target.
