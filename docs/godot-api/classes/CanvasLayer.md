<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CanvasLayer

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

[`CanvasItem`](CanvasItem.md)-derived nodes that are direct or indirect children of a `CanvasLayer` will be drawn in that layer. The layer is a numeric index that defines the draw order. The default 2D scene renders with index `0`, so a `CanvasLayer` with index `-1` will be drawn below, and a `CanvasLayer` with index `1` will be drawn above. This order will hold regardless of the [`CanvasItem.ZIndex`](CanvasItem.md) of the nodes within each layer.

`CanvasLayer`s can be hidden and they can also optionally follow the viewport. This makes them useful for HUDs like health bar overlays (on layers `1` and higher) or backgrounds (on layers `-1` and lower).

**Note:** Embedded [`Window`](Window.md)s are placed on layer `1024`. [`CanvasItem`](CanvasItem.md)s on layers `1025` and higher appear in front of embedded windows.

**Note:** Each `CanvasLayer` is drawn on one specific [`Viewport`](Viewport.md) and cannot be shared between multiple [`Viewport`](Viewport.md)s, see `CustomViewport`. When using multiple [`Viewport`](Viewport.md)s, for example in a split-screen game, you need to create an individual `CanvasLayer` for each [`Viewport`](Viewport.md) you want it to be drawn on.

## 构造函数

### CanvasLayer

```csharp
public CanvasLayer()
```

## 属性

### CustomViewport

```csharp
public Node CustomViewport { get; set; }
```

引擎名：`custom_viewport`

The custom [`Viewport`](Viewport.md) node assigned to the `CanvasLayer`. If `null`, uses the default viewport instead.

### FollowViewportEnabled

```csharp
public bool FollowViewportEnabled { get; set; }
```

引擎名：`follow_viewport_enabled`

If enabled, the `CanvasLayer` maintains its position in world space. If disabled, the `CanvasLayer` stays in a fixed position on the screen.

Together with `FollowViewportScale`, this can be used for a pseudo-3D effect.

### FollowViewportScale

```csharp
public float FollowViewportScale { get; set; }
```

引擎名：`follow_viewport_scale`

Scales the layer when using `FollowViewportEnabled`. Layers moving into the foreground should have increasing scales, while layers moving into the background should have decreasing scales.

### Layer

```csharp
public int Layer { get; set; }
```

引擎名：`layer`

Layer index for draw order. Lower values are drawn behind higher values.

**Note:** If multiple CanvasLayers have the same layer index, [`CanvasItem`](CanvasItem.md) children of one CanvasLayer are drawn behind the [`CanvasItem`](CanvasItem.md) children of the other CanvasLayer. Which CanvasLayer is drawn in front is non-deterministic.

**Note:** The layer index should be between [`RenderingServer.CanvasLayerMin`](RenderingServer.md) and [`RenderingServer.CanvasLayerMax`](RenderingServer.md) (inclusive). Any other value will wrap around.

### Offset

```csharp
public Vector2 Offset { get; set; }
```

引擎名：`offset`

The layer's base offset.

### Rotation

```csharp
public float Rotation { get; set; }
```

引擎名：`rotation`

The layer's rotation in radians.

### Scale

```csharp
public Vector2 Scale { get; set; }
```

引擎名：`scale`

The layer's scale.

### Transform

```csharp
public Transform2D Transform { get; set; }
```

引擎名：`transform`

The layer's transform.

### Visible

```csharp
public bool Visible { get; set; }
```

引擎名：`visible`

If `false`, any [`CanvasItem`](CanvasItem.md) under this `CanvasLayer` will be hidden.

Unlike [`CanvasItem.Visible`](CanvasItem.md), visibility of a `CanvasLayer` isn't propagated to underlying layers.

## 方法

### GetCanvas

```csharp
public Rid GetCanvas()
```

引擎名：`get_canvas`

Returns the RID of the canvas used by this layer.

### GetCustomViewport

```csharp
public Node GetCustomViewport()
```

引擎名：`get_custom_viewport`

### GetFinalTransform

```csharp
public Transform2D GetFinalTransform()
```

引擎名：`get_final_transform`

Returns the transform from the `CanvasLayer`s coordinate system to the [`Viewport`](Viewport.md)s coordinate system.

### GetFollowViewportScale

```csharp
public float GetFollowViewportScale()
```

引擎名：`get_follow_viewport_scale`

### GetLayer

```csharp
public int GetLayer()
```

引擎名：`get_layer`

### GetOffset

```csharp
public Vector2 GetOffset()
```

引擎名：`get_offset`

### GetRotation

```csharp
public float GetRotation()
```

引擎名：`get_rotation`

### GetScale

```csharp
public Vector2 GetScale()
```

引擎名：`get_scale`

### GetTransform

```csharp
public Transform2D GetTransform()
```

引擎名：`get_transform`

### Hide

```csharp
public void Hide()
```

引擎名：`hide`

Hides any [`CanvasItem`](CanvasItem.md) under this `CanvasLayer`. This is equivalent to setting `Visible` to `false`.

### IsFollowingViewport

```csharp
public bool IsFollowingViewport()
```

引擎名：`is_following_viewport`

### IsVisible

```csharp
public bool IsVisible()
```

引擎名：`is_visible`

### SetCustomViewport

```csharp
public void SetCustomViewport(Node viewport)
```

引擎名：`set_custom_viewport`

### SetFollowViewport

```csharp
public void SetFollowViewport(bool enable)
```

引擎名：`set_follow_viewport`

### SetFollowViewportScale

```csharp
public void SetFollowViewportScale(float scale)
```

引擎名：`set_follow_viewport_scale`

### SetLayer

```csharp
public void SetLayer(int layer)
```

引擎名：`set_layer`

### SetOffset

```csharp
public void SetOffset(Vector2 offset)
```

引擎名：`set_offset`

### SetRotation

```csharp
public void SetRotation(float radians)
```

引擎名：`set_rotation`

### SetScale

```csharp
public void SetScale(Vector2 scale)
```

引擎名：`set_scale`

### SetTransform

```csharp
public void SetTransform(Transform2D transform)
```

引擎名：`set_transform`

### SetVisible

```csharp
public void SetVisible(bool visible)
```

引擎名：`set_visible`

### Show

```csharp
public void Show()
```

引擎名：`show`

Shows any [`CanvasItem`](CanvasItem.md) under this `CanvasLayer`. This is equivalent to setting `Visible` to `true`.

## 事件（信号）

### VisibilityChanged

```csharp
public event Action VisibilityChanged
```

引擎名：`visibility_changed`

Emitted when visibility of the layer is changed. See `Visible`.
