<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SubViewportContainer

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A container that displays the contents of underlying [`SubViewport`](SubViewport.md) child nodes. It uses the combined size of the [`SubViewport`](SubViewport.md)s as minimum size, unless `Stretch` is enabled.

**Note:** Changing a `SubViewportContainer`'s [`Control.Scale`](Control.md) will cause its contents to appear distorted. To change its visual size without causing distortion, adjust the node's margins instead (if it's not already in a container).

**Note:** The `SubViewportContainer` forwards mouse-enter and mouse-exit notifications to its sub-viewports.

## 构造函数

### SubViewportContainer

```csharp
public SubViewportContainer()
```

## 属性

### MouseTarget

```csharp
public bool MouseTarget { get; set; }
```

引擎名：`mouse_target`

Configure, if either the `SubViewportContainer` or alternatively the [`Control`](Control.md) nodes of its [`SubViewport`](SubViewport.md) children should be available as targets of mouse-related functionalities, like identifying the drop target in drag-and-drop operations or cursor shape of hovered [`Control`](Control.md) node.

If `false`, the [`Control`](Control.md) nodes inside its [`SubViewport`](SubViewport.md) children are considered as targets.

If `true`, the `SubViewportContainer` itself will be considered as a target.

### Stretch

```csharp
public bool Stretch { get; set; }
```

引擎名：`stretch`

If `true`, the sub-viewport will be automatically resized to the control's size.

**Note:** If `true`, this will prohibit changing [`SubViewport.Size`](SubViewport.md) of its children manually.

### StretchShrink

```csharp
public int StretchShrink { get; set; }
```

引擎名：`stretch_shrink`

Divides the sub-viewport's effective resolution by this value while preserving its scale. This can be used to speed up rendering.

For example, a 1280×720 sub-viewport with `StretchShrink` set to `2` will be rendered at 640×360 while occupying the same size in the container.

**Note:** `Stretch` must be `true` for this property to work.

## 方法

### GetStretchShrink

```csharp
public int GetStretchShrink()
```

引擎名：`get_stretch_shrink`

### IsMouseTargetEnabled

```csharp
public bool IsMouseTargetEnabled()
```

引擎名：`is_mouse_target_enabled`

### IsStretchEnabled

```csharp
public bool IsStretchEnabled()
```

引擎名：`is_stretch_enabled`

### SetMouseTarget

```csharp
public void SetMouseTarget(bool amount)
```

引擎名：`set_mouse_target`

### SetStretch

```csharp
public void SetStretch(bool enable)
```

引擎名：`set_stretch`

### SetStretchShrink

```csharp
public void SetStretchShrink(int amount)
```

引擎名：`set_stretch_shrink`

### _PropagateInputEvent

```csharp
public virtual bool _PropagateInputEvent(InputEvent event)
```

引擎名：`_propagate_input_event`

Virtual method to be implemented by the user. If it returns `true`, the `event` is propagated to [`SubViewport`](SubViewport.md) children. Propagation doesn't happen if it returns `false`. If the function is not implemented, all events are propagated to SubViewports.
