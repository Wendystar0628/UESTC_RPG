<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ScrollContainer

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A container used to provide a child control with scrollbars when needed. Scrollbars will automatically be drawn at the right (for vertical) or bottom (for horizontal) and will enable dragging to move the viewable Control (and its children) within the ScrollContainer. Scrollbars will also automatically resize the grabber based on the [`Control.CustomMinimumSize`](Control.md) of the Control relative to the ScrollContainer.

## 构造函数

### ScrollContainer

```csharp
public ScrollContainer()
```

## 属性

### DrawFocusBorder

```csharp
public bool DrawFocusBorder { get; set; }
```

引擎名：`draw_focus_border`

If `true`, `focus` is drawn when the ScrollContainer or one of its descendant nodes is focused.

### FollowFocus

```csharp
public bool FollowFocus { get; set; }
```

引擎名：`follow_focus`

If `true`, the ScrollContainer will automatically scroll to focused children (including indirect children) to make sure they are fully visible.

### HorizontalScrollMode

```csharp
public ScrollContainer.ScrollMode HorizontalScrollMode { get; set; }
```

引擎名：`horizontal_scroll_mode`

Controls whether horizontal scrollbar can be used and when it should be visible.

### ScrollDeadzone

```csharp
public int ScrollDeadzone { get; set; }
```

引擎名：`scroll_deadzone`

Deadzone for touch scrolling. Lower deadzone makes the scrolling more sensitive.

### ScrollHintMode

```csharp
public ScrollContainer.ScrollHintModeEnum ScrollHintMode { get; set; }
```

引擎名：`scroll_hint_mode`

The way which scroll hints (indicators that show that the content can still be scrolled in a certain direction) will be shown.

**Note:** Hints won't be shown if the content can be scrolled both vertically and horizontally.

### ScrollHorizontal

```csharp
public int ScrollHorizontal { get; set; }
```

引擎名：`scroll_horizontal`

The current horizontal scroll value.

**Note:** If you are setting this value in the [`Node.Ready`](Node.md) function or earlier, it needs to be wrapped with [`GodotObject.SetDeferred`](GodotObject.md), since scroll bar's [`Range.MaxValue`](Range.md) is not initialized yet.

```csharp
func _ready():
set_deferred("scroll_horizontal", 600)

```

### ScrollHorizontalByDefault

```csharp
public bool ScrollHorizontalByDefault { get; set; }
```

引擎名：`scroll_horizontal_by_default`

If `true`, the mouse wheel scrolls the view horizontally, and holding Shift scrolls vertically.

If `false` (default), the mouse wheel scrolls the view vertically, and holding Shift scrolls horizontally.

### ScrollHorizontalCustomStep

```csharp
public float ScrollHorizontalCustomStep { get; set; }
```

引擎名：`scroll_horizontal_custom_step`

Overrides the [`ScrollBar.CustomStep`](ScrollBar.md) used when clicking the internal scroll bar's horizontal increment and decrement buttons or when using arrow keys when the [`ScrollBar`](ScrollBar.md) is focused.

### ScrollVertical

```csharp
public int ScrollVertical { get; set; }
```

引擎名：`scroll_vertical`

The current vertical scroll value.

**Note:** Setting it early needs to be deferred, just like in `ScrollHorizontal`.

```csharp
func _ready():
set_deferred("scroll_vertical", 600)

```

### ScrollVerticalCustomStep

```csharp
public float ScrollVerticalCustomStep { get; set; }
```

引擎名：`scroll_vertical_custom_step`

Overrides the [`ScrollBar.CustomStep`](ScrollBar.md) used when clicking the internal scroll bar's vertical increment and decrement buttons or when using arrow keys when the [`ScrollBar`](ScrollBar.md) is focused.

### TileScrollHint

```csharp
public bool TileScrollHint { get; set; }
```

引擎名：`tile_scroll_hint`

If `true`, the scroll hint texture will be tiled instead of stretched. See `ScrollHintMode`.

### VerticalScrollMode

```csharp
public ScrollContainer.ScrollMode VerticalScrollMode { get; set; }
```

引擎名：`vertical_scroll_mode`

Controls whether vertical scrollbar can be used and when it should be visible.

## 方法

### EnsureControlVisible

```csharp
public void EnsureControlVisible(Control control)
```

引擎名：`ensure_control_visible`

Ensures the given `control` is visible (must be a direct or indirect child of the ScrollContainer). Used by `FollowFocus`.

**Note:** This will not work on a node that was just added during the same frame. If you want to scroll to a newly added child, you must wait until the next frame using [`SceneTree.ProcessFrame`](SceneTree.md):

```csharp
add_child(child_node)
await get_tree().process_frame
ensure_control_visible(child_node)

```

### GetDeadzone

```csharp
public int GetDeadzone()
```

引擎名：`get_deadzone`

### GetDrawFocusBorder

```csharp
public bool GetDrawFocusBorder()
```

引擎名：`get_draw_focus_border`

### GetHScroll

```csharp
public int GetHScroll()
```

引擎名：`get_h_scroll`

### GetHScrollBar

```csharp
public HScrollBar GetHScrollBar()
```

引擎名：`get_h_scroll_bar`

Returns the horizontal scrollbar [`HScrollBar`](HScrollBar.md) of this `ScrollContainer`.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to disable or hide a scrollbar, you can use `HorizontalScrollMode`.

### GetHorizontalCustomStep

```csharp
public float GetHorizontalCustomStep()
```

引擎名：`get_horizontal_custom_step`

### GetHorizontalScrollMode

```csharp
public ScrollContainer.ScrollMode GetHorizontalScrollMode()
```

引擎名：`get_horizontal_scroll_mode`

### GetScrollHintMode

```csharp
public ScrollContainer.ScrollHintModeEnum GetScrollHintMode()
```

引擎名：`get_scroll_hint_mode`

### GetVScroll

```csharp
public int GetVScroll()
```

引擎名：`get_v_scroll`

### GetVScrollBar

```csharp
public VScrollBar GetVScrollBar()
```

引擎名：`get_v_scroll_bar`

Returns the vertical scrollbar [`VScrollBar`](VScrollBar.md) of this `ScrollContainer`.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to disable or hide a scrollbar, you can use `VerticalScrollMode`.

### GetVerticalCustomStep

```csharp
public float GetVerticalCustomStep()
```

引擎名：`get_vertical_custom_step`

### GetVerticalScrollMode

```csharp
public ScrollContainer.ScrollMode GetVerticalScrollMode()
```

引擎名：`get_vertical_scroll_mode`

### IsFollowingFocus

```csharp
public bool IsFollowingFocus()
```

引擎名：`is_following_focus`

### IsScrollHintTiled

```csharp
public bool IsScrollHintTiled()
```

引擎名：`is_scroll_hint_tiled`

### IsScrollHorizontalByDefault

```csharp
public bool IsScrollHorizontalByDefault()
```

引擎名：`is_scroll_horizontal_by_default`

### SetDeadzone

```csharp
public void SetDeadzone(int deadzone)
```

引擎名：`set_deadzone`

### SetDrawFocusBorder

```csharp
public void SetDrawFocusBorder(bool draw)
```

引擎名：`set_draw_focus_border`

### SetFollowFocus

```csharp
public void SetFollowFocus(bool enabled)
```

引擎名：`set_follow_focus`

### SetHScroll

```csharp
public void SetHScroll(int value)
```

引擎名：`set_h_scroll`

### SetHorizontalCustomStep

```csharp
public void SetHorizontalCustomStep(float value)
```

引擎名：`set_horizontal_custom_step`

### SetHorizontalScrollMode

```csharp
public void SetHorizontalScrollMode(ScrollContainer.ScrollMode enable)
```

引擎名：`set_horizontal_scroll_mode`

### SetScrollHintMode

```csharp
public void SetScrollHintMode(ScrollContainer.ScrollHintModeEnum scrollHintMode)
```

引擎名：`set_scroll_hint_mode`

### SetScrollHorizontalByDefault

```csharp
public void SetScrollHorizontalByDefault(bool enable)
```

引擎名：`set_scroll_horizontal_by_default`

### SetTileScrollHint

```csharp
public void SetTileScrollHint(bool tileScrollHint)
```

引擎名：`set_tile_scroll_hint`

### SetVScroll

```csharp
public void SetVScroll(int value)
```

引擎名：`set_v_scroll`

### SetVerticalCustomStep

```csharp
public void SetVerticalCustomStep(float value)
```

引擎名：`set_vertical_custom_step`

### SetVerticalScrollMode

```csharp
public void SetVerticalScrollMode(ScrollContainer.ScrollMode enable)
```

引擎名：`set_vertical_scroll_mode`

## 事件（信号）

### ScrollEnded

```csharp
public event Action ScrollEnded
```

引擎名：`scroll_ended`

Emitted when scrolling stops when dragging the scrollable area *with a touch event*. This signal is *not* emitted when scrolling by dragging the scrollbar, scrolling with the mouse wheel or scrolling with keyboard/gamepad events.

**Note:** This signal is only emitted on Android or iOS, or on desktop/web platforms when `ProjectSettings.input_devices/pointing/emulate_touch_from_mouse` is enabled.

### ScrollStarted

```csharp
public event Action ScrollStarted
```

引擎名：`scroll_started`

Emitted when scrolling starts when dragging the scrollable area *with a touch event*. This signal is *not* emitted when scrolling by dragging the scrollbar, scrolling with the mouse wheel or scrolling with keyboard/gamepad events.

**Note:** This signal is only emitted on Android or iOS, or on desktop/web platforms when `ProjectSettings.input_devices/pointing/emulate_touch_from_mouse` is enabled.

## ScrollHintModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Scroll hints will never be shown.
- `All = 1`：Scroll hints will be shown at the top and bottom (if vertical), or left and right (if horizontal).
- `TopAndLeft = 2`：Scroll hints will be shown at the top (if vertical), or the left (if horizontal).
- `BottomAndRight = 3`：Scroll hints will be shown at the bottom (if horizontal), or the right (if horizontal).

## ScrollMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Scrolling disabled, scrollbar will be invisible.
- `Auto = 1`：Scrolling enabled, scrollbar will be visible only if necessary, i.e.
- `ShowAlways = 2`：Scrolling enabled, scrollbar will be always visible.
- `ShowNever = 3`：Scrolling enabled, scrollbar will be hidden.
- `Reserve = 4`：Combines `Auto` and `ShowAlways`.
- `MaximizeFirst = 5`：Behaves like `Auto`, but makes the [`ScrollContainer`](ScrollContainer.md) report a minimum size based on its content (limited by [`Control.CustomMaximumSize`](Control.md) when set on the corresponding axis).
