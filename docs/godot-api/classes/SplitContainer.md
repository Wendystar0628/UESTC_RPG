<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SplitContainer

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A container that arranges child controls horizontally or vertically and creates grabbers between them. The grabbers can be dragged around to change the size relations between the child controls.

## 构造函数

### SplitContainer

```csharp
public SplitContainer()
```

## 属性

### Collapsed

```csharp
public bool Collapsed { get; set; }
```

引擎名：`collapsed`

If `true`, the draggers will be disabled and the children will be sized as if all `SplitOffsets` were `0`.

### DragAreaHighlightInEditor

```csharp
public bool DragAreaHighlightInEditor { get; set; }
```

引擎名：`drag_area_highlight_in_editor`

Highlights the drag area [`Rect2`](Rect2.md) so you can see where it is during development. The drag area is gold if `DraggingEnabled` is `true`, and red if `false`.

### DragAreaMarginBegin

```csharp
public int DragAreaMarginBegin { get; set; }
```

引擎名：`drag_area_margin_begin`

Reduces the size of the drag area and split bar `split_bar_background` at the beginning of the container.

### DragAreaMarginEnd

```csharp
public int DragAreaMarginEnd { get; set; }
```

引擎名：`drag_area_margin_end`

Reduces the size of the drag area and split bar `split_bar_background` at the end of the container.

### DragAreaOffset

```csharp
public int DragAreaOffset { get; set; }
```

引擎名：`drag_area_offset`

Shifts the drag area in the axis of the container to prevent the drag area from overlapping the [`ScrollBar`](ScrollBar.md) or other selectable [`Control`](Control.md) of a child node.

### DragNestedIntersections

```csharp
public bool DragNestedIntersections { get; set; }
```

引擎名：`drag_nested_intersections`

Adds extra draggers at the intersection of the draggers of two SplitContainers to allow dragging both at once. This must be set to `true` for both SplitContainers, and one needs to be a descendant of the other. They also must be orthogonal (their `Vertical` are different) and the descendant must be next to at least one of the ancestor's draggers (within `minimum_grab_thickness`).

### DraggerVisibility

```csharp
public SplitContainer.DraggerVisibilityEnum DraggerVisibility { get; set; }
```

引擎名：`dragger_visibility`

Determines the dragger's visibility. This property does not determine whether dragging is enabled or not. Use `DraggingEnabled` for that.

### DraggingEnabled

```csharp
public bool DraggingEnabled { get; set; }
```

引擎名：`dragging_enabled`

Enables or disables split dragging.

### SplitOffset

```csharp
public int SplitOffset { get; set; }
```

引擎名：`split_offset`

The first element of `SplitOffsets`.

### SplitOffsets

```csharp
public int[] SplitOffsets { get; set; }
```

引擎名：`split_offsets`

Offsets for each dragger in pixels. Each one is the offset of the split between the [`Control`](Control.md) nodes before and after the dragger, with `0` being the default position. The default position is based on the [`Control`](Control.md) nodes expand flags and minimum sizes. See [`Control.SizeFlagsHorizontal`](Control.md), [`Control.SizeFlagsVertical`](Control.md), and [`Control.SizeFlagsStretchRatio`](Control.md).

If none of the [`Control`](Control.md) nodes before the dragger are expanded, the default position will be at the start of the `SplitContainer`. If none of the [`Control`](Control.md) nodes after the dragger are expanded, the default position will be at the end of the `SplitContainer`. If the dragger is in between expanded [`Control`](Control.md) nodes, the default position will be in the middle, based on the [`Control.SizeFlagsStretchRatio`](Control.md)s and minimum sizes.

**Note:** If the split offsets cause [`Control`](Control.md) nodes to overlap, the first split will take priority when resolving the positions.

### TouchDraggerEnabled

```csharp
public bool TouchDraggerEnabled { get; set; }
```

引擎名：`touch_dragger_enabled`

If `true`, a touch-friendly drag handle will be enabled for better usability on smaller screens. Unlike the standard grabber, this drag handle overlaps the `SplitContainer`'s children and does not affect their minimum separation. The standard grabber will no longer be drawn when this option is enabled.

### Vertical

```csharp
public bool Vertical { get; set; }
```

引擎名：`vertical`

If `true`, the `SplitContainer` will arrange its children vertically, rather than horizontally.

Can't be changed when using [`HSplitContainer`](HSplitContainer.md) and [`VSplitContainer`](VSplitContainer.md).

## 方法

### ClampSplitOffset

```csharp
public void ClampSplitOffset()
```

引擎名：`clamp_split_offset`

Clamps the `SplitOffsets` values to ensure they are within valid ranges and do not overlap with each other. When overlaps occur, this method prioritizes one split offset (at index `priorityIndex`) by clamping any overlapping split offsets to it.

### ClampSplitOffset

```csharp
public void ClampSplitOffset(int priorityIndex = 0)
```

引擎名：`clamp_split_offset`

Clamps the `SplitOffsets` values to ensure they are within valid ranges and do not overlap with each other. When overlaps occur, this method prioritizes one split offset (at index `priorityIndex`) by clamping any overlapping split offsets to it.

### GetDragAreaControl

```csharp
public Control GetDragAreaControl()
```

引擎名：`get_drag_area_control`

Returns the drag area [`Control`](Control.md). For example, you can move a pre-configured button into the drag area [`Control`](Control.md) so that it rides along with the split bar. Try setting the [`Button`](Button.md) anchors to `center` prior to the `reparent()` call.

```csharp
$BarnacleButton.reparent($SplitContainer.get_drag_area_control())

```

**Note:** The drag area [`Control`](Control.md) is drawn over the `SplitContainer`'s children, so [`CanvasItem`](CanvasItem.md) draw objects called from the [`Control`](Control.md) and children added to the [`Control`](Control.md) will also appear over the `SplitContainer`'s children. Try setting [`Control.MouseFilter`](Control.md) of custom children to [`Control.MouseFilterEnum.Ignore`](Control.md) to prevent blocking the mouse from dragging if desired.

**Warning:** This is a required internal node, removing and freeing it may cause a crash.

### GetDragAreaControls

```csharp
public Array<Control> GetDragAreaControls()
```

引擎名：`get_drag_area_controls`

Returns an [`Collections.Array`](Collections.Array.md) of the drag area [`Control`](Control.md)s. These are the interactable [`Control`](Control.md) nodes between each child. For example, this can be used to add a pre-configured button to a drag area [`Control`](Control.md) so that it rides along with the split bar. Try setting the [`Button`](Button.md) anchors to `center` prior to the [`Node.Reparent`](Node.md) call.

```csharp
$BarnacleButton.reparent($SplitContainer.get_drag_area_controls()[0])

```

**Note:** The drag area [`Control`](Control.md)s are drawn over the `SplitContainer`'s children, so [`CanvasItem`](CanvasItem.md) draw objects called from a drag area and children added to it will also appear over the `SplitContainer`'s children. Try setting [`Control.MouseFilter`](Control.md) of custom children to [`Control.MouseFilterEnum.Ignore`](Control.md) to prevent blocking the mouse from dragging if desired.

**Warning:** These are required internal nodes, removing or freeing them may cause a crash.

### GetDragAreaMarginBegin

```csharp
public int GetDragAreaMarginBegin()
```

引擎名：`get_drag_area_margin_begin`

### GetDragAreaMarginEnd

```csharp
public int GetDragAreaMarginEnd()
```

引擎名：`get_drag_area_margin_end`

### GetDragAreaOffset

```csharp
public int GetDragAreaOffset()
```

引擎名：`get_drag_area_offset`

### GetDraggerVisibility

```csharp
public SplitContainer.DraggerVisibilityEnum GetDraggerVisibility()
```

引擎名：`get_dragger_visibility`

### GetSplitOffset

```csharp
public int GetSplitOffset()
```

引擎名：`get_split_offset`

### GetSplitOffsets

```csharp
public int[] GetSplitOffsets()
```

引擎名：`get_split_offsets`

### IsCollapsed

```csharp
public bool IsCollapsed()
```

引擎名：`is_collapsed`

### IsDragAreaHighlightInEditorEnabled

```csharp
public bool IsDragAreaHighlightInEditorEnabled()
```

引擎名：`is_drag_area_highlight_in_editor_enabled`

### IsDraggingEnabled

```csharp
public bool IsDraggingEnabled()
```

引擎名：`is_dragging_enabled`

### IsDraggingNestedIntersections

```csharp
public bool IsDraggingNestedIntersections()
```

引擎名：`is_dragging_nested_intersections`

### IsTouchDraggerEnabled

```csharp
public bool IsTouchDraggerEnabled()
```

引擎名：`is_touch_dragger_enabled`

### IsVertical

```csharp
public bool IsVertical()
```

引擎名：`is_vertical`

### SetCollapsed

```csharp
public void SetCollapsed(bool collapsed)
```

引擎名：`set_collapsed`

### SetDragAreaHighlightInEditor

```csharp
public void SetDragAreaHighlightInEditor(bool dragAreaHighlightInEditor)
```

引擎名：`set_drag_area_highlight_in_editor`

### SetDragAreaMarginBegin

```csharp
public void SetDragAreaMarginBegin(int margin)
```

引擎名：`set_drag_area_margin_begin`

### SetDragAreaMarginEnd

```csharp
public void SetDragAreaMarginEnd(int margin)
```

引擎名：`set_drag_area_margin_end`

### SetDragAreaOffset

```csharp
public void SetDragAreaOffset(int offset)
```

引擎名：`set_drag_area_offset`

### SetDragNestedIntersections

```csharp
public void SetDragNestedIntersections(bool enabled)
```

引擎名：`set_drag_nested_intersections`

### SetDraggerVisibility

```csharp
public void SetDraggerVisibility(SplitContainer.DraggerVisibilityEnum mode)
```

引擎名：`set_dragger_visibility`

### SetDraggingEnabled

```csharp
public void SetDraggingEnabled(bool draggingEnabled)
```

引擎名：`set_dragging_enabled`

### SetSplitOffset

```csharp
public void SetSplitOffset(int offset)
```

引擎名：`set_split_offset`

### SetSplitOffsets

```csharp
public void SetSplitOffsets(ReadOnlySpan<int> offsets)
```

引擎名：`set_split_offsets`

### SetSplitOffsets

```csharp
public void SetSplitOffsets(int[] offsets)
```

引擎名：`set_split_offsets`

### SetTouchDraggerEnabled

```csharp
public void SetTouchDraggerEnabled(bool enabled)
```

引擎名：`set_touch_dragger_enabled`

### SetVertical

```csharp
public void SetVertical(bool vertical)
```

引擎名：`set_vertical`

## 事件（信号）

### DragEnded

```csharp
public event Action DragEnded
```

引擎名：`drag_ended`

Emitted when the user ends dragging.

### DragStarted

```csharp
public event Action DragStarted
```

引擎名：`drag_started`

Emitted when the user starts dragging.

### Dragged

```csharp
public event SplitContainer.DraggedEventHandler Dragged
```

引擎名：`dragged`

Emitted when any dragger is dragged by user.

## DraggedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`SplitContainer.Dragged`](SplitContainer.md) event of a [`SplitContainer`](SplitContainer.md) class.

### 签名

#### Invoke

```csharp
delegate void DraggedEventHandler(long offset)
```

## DraggerVisibilityEnum

命名空间 `Godot`；enum

### 枚举值

- `Visible = 0`：The split dragger icon is always visible when `autohide` is `false`, otherwise visible only when the cursor hovers it.
- `Hidden = 1`：The split dragger icon is never visible regardless of the value of `autohide`.
- `HiddenCollapsed = 2`：The split dragger icon is not visible, and the split bar is collapsed to zero thickness.
