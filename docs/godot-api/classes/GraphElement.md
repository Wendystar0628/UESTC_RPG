<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GraphElement

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`GraphElement` allows to create custom elements for a [`GraphEdit`](GraphEdit.md) graph. By default such elements can be selected, resized, and repositioned, but they cannot be connected. For a graph element that allows for connections see [`GraphNode`](GraphNode.md).

## 构造函数

### GraphElement

```csharp
public GraphElement()
```

## 属性

### Draggable

```csharp
public bool Draggable { get; set; }
```

引擎名：`draggable`

If `true`, the user can drag the GraphElement.

### PositionOffset

```csharp
public Vector2 PositionOffset { get; set; }
```

引擎名：`position_offset`

The offset of the GraphElement, relative to the scroll offset of the [`GraphEdit`](GraphEdit.md).

### Resizable

```csharp
public bool Resizable { get; set; }
```

引擎名：`resizable`

If `true`, the user can resize the GraphElement.

**Note:** Dragging the handle will only emit the `ResizeRequest` and `ResizeEnd` signals, the GraphElement needs to be resized manually.

### ScalingMenus

```csharp
public bool ScalingMenus { get; set; }
```

引擎名：`scaling_menus`

If `true`, [`PopupMenu`](PopupMenu.md)s that are descendants of the GraphElement are scaled with the [`GraphEdit`](GraphEdit.md) zoom.

### Selectable

```csharp
public bool Selectable { get; set; }
```

引擎名：`selectable`

If `true`, the user can select the GraphElement.

### Selected

```csharp
public bool Selected { get; set; }
```

引擎名：`selected`

If `true`, the GraphElement is selected.

## 方法

### GetPositionOffset

```csharp
public Vector2 GetPositionOffset()
```

引擎名：`get_position_offset`

### IsDraggable

```csharp
public bool IsDraggable()
```

引擎名：`is_draggable`

### IsResizable

```csharp
public bool IsResizable()
```

引擎名：`is_resizable`

### IsScalingMenus

```csharp
public bool IsScalingMenus()
```

引擎名：`is_scaling_menus`

### IsSelectable

```csharp
public bool IsSelectable()
```

引擎名：`is_selectable`

### IsSelected

```csharp
public bool IsSelected()
```

引擎名：`is_selected`

### SetDraggable

```csharp
public void SetDraggable(bool draggable)
```

引擎名：`set_draggable`

### SetPositionOffset

```csharp
public void SetPositionOffset(Vector2 offset)
```

引擎名：`set_position_offset`

### SetResizable

```csharp
public void SetResizable(bool resizable)
```

引擎名：`set_resizable`

### SetScalingMenus

```csharp
public void SetScalingMenus(bool scalingMenus)
```

引擎名：`set_scaling_menus`

### SetSelectable

```csharp
public void SetSelectable(bool selectable)
```

引擎名：`set_selectable`

### SetSelected

```csharp
public void SetSelected(bool selected)
```

引擎名：`set_selected`

## 事件（信号）

### DeleteRequest

```csharp
public event Action DeleteRequest
```

引擎名：`delete_request`

Emitted when removing the GraphElement is requested.

### Dragged

```csharp
public event GraphElement.DraggedEventHandler Dragged
```

引擎名：`dragged`

Emitted when the GraphElement is dragged.

### NodeDeselected

```csharp
public event Action NodeDeselected
```

引擎名：`node_deselected`

Emitted when the GraphElement is deselected.

### NodeSelected

```csharp
public event Action NodeSelected
```

引擎名：`node_selected`

Emitted when the GraphElement is selected.

### PositionOffsetChanged

```csharp
public event Action PositionOffsetChanged
```

引擎名：`position_offset_changed`

Emitted when the GraphElement is moved.

### RaiseRequest

```csharp
public event Action RaiseRequest
```

引擎名：`raise_request`

Emitted when displaying the GraphElement over other ones is requested. Happens on focusing (clicking into) the GraphElement.

### ResizeEnd

```csharp
public event GraphElement.ResizeEndEventHandler ResizeEnd
```

引擎名：`resize_end`

Emitted when releasing the mouse button after dragging the resizer handle (see `Resizable`).

### ResizeRequest

```csharp
public event GraphElement.ResizeRequestEventHandler ResizeRequest
```

引擎名：`resize_request`

Emitted when resizing the GraphElement is requested. Happens on dragging the resizer handle (see `Resizable`).

## DraggedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphElement.Dragged`](GraphElement.md) event of a [`GraphElement`](GraphElement.md) class.

### 签名

#### Invoke

```csharp
delegate void DraggedEventHandler(Vector2 from, Vector2 to)
```

## ResizeEndEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphElement.ResizeEnd`](GraphElement.md) event of a [`GraphElement`](GraphElement.md) class.

### 签名

#### Invoke

```csharp
delegate void ResizeEndEventHandler(Vector2 newSize)
```

## ResizeRequestEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphElement.ResizeRequest`](GraphElement.md) event of a [`GraphElement`](GraphElement.md) class.

### 签名

#### Invoke

```csharp
delegate void ResizeRequestEventHandler(Vector2 newSize)
```
