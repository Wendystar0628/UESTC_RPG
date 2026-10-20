<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Camera2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Camera node for 2D scenes. It forces the screen (current layer) to scroll following this node. This makes it easier (and faster) to program scrollable scenes than manually changing the position of [`CanvasItem`](CanvasItem.md)-based nodes.

Cameras register themselves in the nearest [`Viewport`](Viewport.md) node (when ascending the tree). Only one camera can be active per viewport. If no viewport is available ascending the tree, the camera will register in the global viewport.

This node is intended to be a simple helper to get things going quickly, but more functionality may be desired to change how the camera works. To make your own custom camera node, inherit it from [`Node2D`](Node2D.md) and change the transform of the canvas by setting [`Viewport.CanvasTransform`](Viewport.md) in [`Viewport`](Viewport.md) (you can obtain the current [`Viewport`](Viewport.md) by using [`Node.GetViewport`](Node.md)).

Note that the `Camera2D` node's [`Node2D.GlobalPosition`](Node2D.md) doesn't represent the actual position of the screen, which may differ due to applied smoothing or limits. You can use `GetScreenCenterPosition()` to get the real position. Same for the node's [`Node2D.GlobalRotation`](Node2D.md) which may be different due to applied rotation smoothing. You can use `GetScreenRotation()` to get the current rotation of the screen.

## 构造函数

### Camera2D

```csharp
public Camera2D()
```

## 属性

### AnchorMode

```csharp
public Camera2D.AnchorModeEnum AnchorMode { get; set; }
```

引擎名：`anchor_mode`

The Camera2D's anchor point.

### CustomViewport

```csharp
public Node CustomViewport { get; set; }
```

引擎名：`custom_viewport`

The custom [`Viewport`](Viewport.md) node attached to the `Camera2D`. If `null` or not a [`Viewport`](Viewport.md), uses the default viewport instead.

### DragBottomMargin

```csharp
public float DragBottomMargin { get; set; }
```

引擎名：`drag_bottom_margin`

Bottom margin needed to drag the camera. A value of `1` makes the camera move only when reaching the bottom edge of the screen.

### DragHorizontalEnabled

```csharp
public bool DragHorizontalEnabled { get; set; }
```

引擎名：`drag_horizontal_enabled`

If `true`, the camera only moves when reaching the horizontal (left and right) drag margins. If `false`, the camera moves horizontally regardless of margins.

### DragHorizontalOffset

```csharp
public float DragHorizontalOffset { get; set; }
```

引擎名：`drag_horizontal_offset`

The relative horizontal drag offset of the camera between the right (`-1`) and left (`1`) drag margins.

**Note:** Used to set the initial horizontal drag offset; determine the current offset; or force the current offset. It's not automatically updated when `DragHorizontalEnabled` is `true` or the drag margins are changed.

### DragLeftMargin

```csharp
public float DragLeftMargin { get; set; }
```

引擎名：`drag_left_margin`

Left margin needed to drag the camera. A value of `1` makes the camera move only when reaching the left edge of the screen.

### DragRightMargin

```csharp
public float DragRightMargin { get; set; }
```

引擎名：`drag_right_margin`

Right margin needed to drag the camera. A value of `1` makes the camera move only when reaching the right edge of the screen.

### DragTopMargin

```csharp
public float DragTopMargin { get; set; }
```

引擎名：`drag_top_margin`

Top margin needed to drag the camera. A value of `1` makes the camera move only when reaching the top edge of the screen.

### DragVerticalEnabled

```csharp
public bool DragVerticalEnabled { get; set; }
```

引擎名：`drag_vertical_enabled`

If `true`, the camera only moves when reaching the vertical (top and bottom) drag margins. If `false`, the camera moves vertically regardless of the drag margins.

### DragVerticalOffset

```csharp
public float DragVerticalOffset { get; set; }
```

引擎名：`drag_vertical_offset`

The relative vertical drag offset of the camera between the bottom (`-1`) and top (`1`) drag margins.

**Note:** Used to set the initial vertical drag offset; determine the current offset; or force the current offset. It's not automatically updated when `DragVerticalEnabled` is `true` or the drag margins are changed.

### EditorDrawDragMargin

```csharp
public bool EditorDrawDragMargin { get; set; }
```

引擎名：`editor_draw_drag_margin`

If `true`, draws the camera's drag margin rectangle in the editor.

### EditorDrawLimits

```csharp
public bool EditorDrawLimits { get; set; }
```

引擎名：`editor_draw_limits`

If `true`, draws the camera's limits rectangle in the editor.

### EditorDrawScreen

```csharp
public bool EditorDrawScreen { get; set; }
```

引擎名：`editor_draw_screen`

If `true`, draws the camera's screen rectangle in the editor.

### Enabled

```csharp
public bool Enabled { get; set; }
```

引擎名：`enabled`

Controls whether the camera can be active or not. If `true`, the `Camera2D` will become the main camera when it enters the scene tree and there is no active camera currently (see [`Viewport.GetCamera2D`](Viewport.md)).

When the camera is currently active and `Enabled` is set to `false`, the next enabled `Camera2D` in the scene tree will become active.

### IgnoreRotation

```csharp
public bool IgnoreRotation { get; set; }
```

引擎名：`ignore_rotation`

If `true`, the camera's rendered view is not affected by its [`Node2D.Rotation`](Node2D.md) and [`Node2D.GlobalRotation`](Node2D.md).

### LimitBottom

```csharp
public int LimitBottom { get; set; }
```

引擎名：`limit_bottom`

Bottom scroll limit in pixels. The camera stops moving when reaching this value, but `Offset` can push the view past the limit.

### LimitEnabled

```csharp
public bool LimitEnabled { get; set; }
```

引擎名：`limit_enabled`

If `true`, the limits will be enabled. Disabling this will allow the camera to focus anywhere, when the four `limit_*` properties will not work.

### LimitLeft

```csharp
public int LimitLeft { get; set; }
```

引擎名：`limit_left`

Left scroll limit in pixels. The camera stops moving when reaching this value, but `Offset` can push the view past the limit.

### LimitRight

```csharp
public int LimitRight { get; set; }
```

引擎名：`limit_right`

Right scroll limit in pixels. The camera stops moving when reaching this value, but `Offset` can push the view past the limit.

### LimitSmoothed

```csharp
public bool LimitSmoothed { get; set; }
```

引擎名：`limit_smoothed`

If `true`, the camera smoothly stops when reaches its limits.

This property has no effect if `PositionSmoothingEnabled` is `false`.

**Note:** To immediately update the camera's position to be within limits without smoothing, even with this setting enabled, invoke `ResetSmoothing()`.

### LimitTop

```csharp
public int LimitTop { get; set; }
```

引擎名：`limit_top`

Top scroll limit in pixels. The camera stops moving when reaching this value, but `Offset` can push the view past the limit.

### Offset

```csharp
public Vector2 Offset { get; set; }
```

引擎名：`offset`

The camera's relative offset. Useful for looking around or camera shake animations. The offsetted camera can go past the limits defined in `LimitTop`, `LimitBottom`, `LimitLeft` and `LimitRight`.

### PositionSmoothingEnabled

```csharp
public bool PositionSmoothingEnabled { get; set; }
```

引擎名：`position_smoothing_enabled`

If `true`, the camera's view smoothly moves towards its target position at `PositionSmoothingSpeed`.

### PositionSmoothingSpeed

```csharp
public float PositionSmoothingSpeed { get; set; }
```

引擎名：`position_smoothing_speed`

Speed in pixels per second of the camera's smoothing effect when `PositionSmoothingEnabled` is `true`.

### ProcessCallback

```csharp
public Camera2D.Camera2DProcessCallback ProcessCallback { get; set; }
```

引擎名：`process_callback`

The camera's process callback.

### RotationSmoothingEnabled

```csharp
public bool RotationSmoothingEnabled { get; set; }
```

引擎名：`rotation_smoothing_enabled`

If `true`, the camera's view smoothly rotates, via asymptotic smoothing, to align with its target rotation at `RotationSmoothingSpeed`.

**Note:** This property has no effect if `IgnoreRotation` is `true`.

### RotationSmoothingSpeed

```csharp
public float RotationSmoothingSpeed { get; set; }
```

引擎名：`rotation_smoothing_speed`

The angular, asymptotic speed of the camera's rotation smoothing effect when `RotationSmoothingEnabled` is `true`.

### Zoom

```csharp
public Vector2 Zoom { get; set; }
```

引擎名：`zoom`

The camera's zoom. Higher values are more zoomed in. For example, a zoom of `Vector2(2.0, 2.0)` will be twice as zoomed in on each axis (the view covers an area four times smaller). In contrast, a zoom of `Vector2(0.5, 0.5)` will be twice as zoomed out on each axis (the view covers an area four times larger). The X and Y components should generally always be set to the same value, unless you wish to stretch the camera view.

**Note:** [`FontFile.Oversampling`](FontFile.md) does *not* take `Camera2D` zoom into account. This means that zooming in/out will cause bitmap fonts and rasterized (non-MSDF) dynamic fonts to appear blurry or pixelated unless the font is part of a [`CanvasLayer`](CanvasLayer.md) that makes it ignore camera zoom. To ensure text remains crisp regardless of zoom, you can enable MSDF font rendering by enabling `ProjectSettings.gui/theme/default_font_multichannel_signed_distance_field` (applies to the default project font only), or enabling **Multichannel Signed Distance Field** in the import options of a DynamicFont for custom fonts. On system fonts, [`SystemFont.MultichannelSignedDistanceField`](SystemFont.md) can be enabled in the inspector.

## 方法

### Align

```csharp
public void Align()
```

引擎名：`align`

Aligns the camera to the tracked node.

**Note:** Calling `ForceUpdateScroll()` after this method is not required.

### ForceUpdateScroll

```csharp
public void ForceUpdateScroll()
```

引擎名：`force_update_scroll`

Forces the camera to update scroll immediately.

### GetAnchorMode

```csharp
public Camera2D.AnchorModeEnum GetAnchorMode()
```

引擎名：`get_anchor_mode`

### GetCustomViewport

```csharp
public Node GetCustomViewport()
```

引擎名：`get_custom_viewport`

### GetDragHorizontalOffset

```csharp
public float GetDragHorizontalOffset()
```

引擎名：`get_drag_horizontal_offset`

### GetDragMargin

```csharp
public float GetDragMargin(Side margin)
```

引擎名：`get_drag_margin`

Returns the specified [`Side`](Side.md)'s margin. See also `DragBottomMargin`, `DragTopMargin`, `DragLeftMargin`, and `DragRightMargin`.

### GetDragVerticalOffset

```csharp
public float GetDragVerticalOffset()
```

引擎名：`get_drag_vertical_offset`

### GetLimit

```csharp
public int GetLimit(Side margin)
```

引擎名：`get_limit`

Returns the camera limit for the specified [`Side`](Side.md). See also `LimitBottom`, `LimitTop`, `LimitLeft`, and `LimitRight`.

### GetOffset

```csharp
public Vector2 GetOffset()
```

引擎名：`get_offset`

### GetPositionSmoothingSpeed

```csharp
public float GetPositionSmoothingSpeed()
```

引擎名：`get_position_smoothing_speed`

### GetProcessCallback

```csharp
public Camera2D.Camera2DProcessCallback GetProcessCallback()
```

引擎名：`get_process_callback`

### GetRotationSmoothingSpeed

```csharp
public float GetRotationSmoothingSpeed()
```

引擎名：`get_rotation_smoothing_speed`

### GetScreenCenterPosition

```csharp
public Vector2 GetScreenCenterPosition()
```

引擎名：`get_screen_center_position`

Returns the center of the screen from this camera's point of view, in global coordinates.

**Note:** The exact targeted position of the camera may be different. See `GetTargetPosition()`.

### GetScreenRotation

```csharp
public float GetScreenRotation()
```

引擎名：`get_screen_rotation`

Returns the current screen rotation from this camera's point of view.

**Note:** The screen rotation can be different from [`Node2D.GlobalRotation`](Node2D.md) if the camera is rotating smoothly due to `RotationSmoothingEnabled`.

### GetTargetPosition

```csharp
public Vector2 GetTargetPosition()
```

引擎名：`get_target_position`

Returns this camera's target position, in global coordinates.

**Note:** The returned value is not the same as [`Node2D.GlobalPosition`](Node2D.md), as it is affected by the drag properties. It is also not the same as the current position if `PositionSmoothingEnabled` is `true` (see `GetScreenCenterPosition()`).

### GetZoom

```csharp
public Vector2 GetZoom()
```

引擎名：`get_zoom`

### IsCurrent

```csharp
public bool IsCurrent()
```

引擎名：`is_current`

Returns `true` if this `Camera2D` is the active camera (see [`Viewport.GetCamera2D`](Viewport.md)).

### IsDragHorizontalEnabled

```csharp
public bool IsDragHorizontalEnabled()
```

引擎名：`is_drag_horizontal_enabled`

### IsDragVerticalEnabled

```csharp
public bool IsDragVerticalEnabled()
```

引擎名：`is_drag_vertical_enabled`

### IsEnabled

```csharp
public bool IsEnabled()
```

引擎名：`is_enabled`

### IsIgnoringRotation

```csharp
public bool IsIgnoringRotation()
```

引擎名：`is_ignoring_rotation`

### IsLimitDrawingEnabled

```csharp
public bool IsLimitDrawingEnabled()
```

引擎名：`is_limit_drawing_enabled`

### IsLimitEnabled

```csharp
public bool IsLimitEnabled()
```

引擎名：`is_limit_enabled`

### IsLimitSmoothingEnabled

```csharp
public bool IsLimitSmoothingEnabled()
```

引擎名：`is_limit_smoothing_enabled`

### IsMarginDrawingEnabled

```csharp
public bool IsMarginDrawingEnabled()
```

引擎名：`is_margin_drawing_enabled`

### IsPositionSmoothingEnabled

```csharp
public bool IsPositionSmoothingEnabled()
```

引擎名：`is_position_smoothing_enabled`

### IsRotationSmoothingEnabled

```csharp
public bool IsRotationSmoothingEnabled()
```

引擎名：`is_rotation_smoothing_enabled`

### IsScreenDrawingEnabled

```csharp
public bool IsScreenDrawingEnabled()
```

引擎名：`is_screen_drawing_enabled`

### MakeCurrent

```csharp
public void MakeCurrent()
```

引擎名：`make_current`

Forces this `Camera2D` to become the current active one. `Enabled` must be `true`.

### ResetSmoothing

```csharp
public void ResetSmoothing()
```

引擎名：`reset_smoothing`

Sets the camera's position immediately to its current smoothing destination.

This method has no effect if `PositionSmoothingEnabled` is `false`.

### SetAnchorMode

```csharp
public void SetAnchorMode(Camera2D.AnchorModeEnum anchorMode)
```

引擎名：`set_anchor_mode`

### SetCustomViewport

```csharp
public void SetCustomViewport(Node viewport)
```

引擎名：`set_custom_viewport`

### SetDragHorizontalEnabled

```csharp
public void SetDragHorizontalEnabled(bool enabled)
```

引擎名：`set_drag_horizontal_enabled`

### SetDragHorizontalOffset

```csharp
public void SetDragHorizontalOffset(float offset)
```

引擎名：`set_drag_horizontal_offset`

### SetDragMargin

```csharp
public void SetDragMargin(Side margin, float dragMargin)
```

引擎名：`set_drag_margin`

Sets the specified [`Side`](Side.md)'s margin. See also `DragBottomMargin`, `DragTopMargin`, `DragLeftMargin`, and `DragRightMargin`.

### SetDragVerticalEnabled

```csharp
public void SetDragVerticalEnabled(bool enabled)
```

引擎名：`set_drag_vertical_enabled`

### SetDragVerticalOffset

```csharp
public void SetDragVerticalOffset(float offset)
```

引擎名：`set_drag_vertical_offset`

### SetEnabled

```csharp
public void SetEnabled(bool enabled)
```

引擎名：`set_enabled`

### SetIgnoreRotation

```csharp
public void SetIgnoreRotation(bool ignore)
```

引擎名：`set_ignore_rotation`

### SetLimit

```csharp
public void SetLimit(Side margin, int limit)
```

引擎名：`set_limit`

Sets the camera limit for the specified [`Side`](Side.md). See also `LimitBottom`, `LimitTop`, `LimitLeft`, and `LimitRight`.

### SetLimitDrawingEnabled

```csharp
public void SetLimitDrawingEnabled(bool limitDrawingEnabled)
```

引擎名：`set_limit_drawing_enabled`

### SetLimitEnabled

```csharp
public void SetLimitEnabled(bool limitEnabled)
```

引擎名：`set_limit_enabled`

### SetLimitSmoothingEnabled

```csharp
public void SetLimitSmoothingEnabled(bool limitSmoothingEnabled)
```

引擎名：`set_limit_smoothing_enabled`

### SetMarginDrawingEnabled

```csharp
public void SetMarginDrawingEnabled(bool marginDrawingEnabled)
```

引擎名：`set_margin_drawing_enabled`

### SetOffset

```csharp
public void SetOffset(Vector2 offset)
```

引擎名：`set_offset`

### SetPositionSmoothingEnabled

```csharp
public void SetPositionSmoothingEnabled(bool enabled)
```

引擎名：`set_position_smoothing_enabled`

### SetPositionSmoothingSpeed

```csharp
public void SetPositionSmoothingSpeed(float positionSmoothingSpeed)
```

引擎名：`set_position_smoothing_speed`

### SetProcessCallback

```csharp
public void SetProcessCallback(Camera2D.Camera2DProcessCallback mode)
```

引擎名：`set_process_callback`

### SetRotationSmoothingEnabled

```csharp
public void SetRotationSmoothingEnabled(bool enabled)
```

引擎名：`set_rotation_smoothing_enabled`

### SetRotationSmoothingSpeed

```csharp
public void SetRotationSmoothingSpeed(float speed)
```

引擎名：`set_rotation_smoothing_speed`

### SetScreenDrawingEnabled

```csharp
public void SetScreenDrawingEnabled(bool screenDrawingEnabled)
```

引擎名：`set_screen_drawing_enabled`

### SetZoom

```csharp
public void SetZoom(Vector2 zoom)
```

引擎名：`set_zoom`

## AnchorModeEnum

命名空间 `Godot`；enum

### 枚举值

- `FixedTopLeft = 0`：The camera's position is fixed so that the top-left corner is always at the origin.
- `DragCenter = 1`：The camera's position takes into account vertical/horizontal offsets and the screen size.

## Camera2DProcessCallback

命名空间 `Godot`；enum

### 枚举值

- `Physics = 0`：The camera updates during physics frames (see [`Node.NotificationInternalPhysicsProcess`](Node.md)).
- `Idle = 1`：The camera updates during process frames (see [`Node.NotificationInternalProcess`](Node.md)).
