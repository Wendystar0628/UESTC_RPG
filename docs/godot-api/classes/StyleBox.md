<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StyleBox

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`StyleBox` is an abstract base class for drawing stylized boxes for UI elements. It is used for panels, buttons, [`LineEdit`](LineEdit.md) backgrounds, [`Tree`](Tree.md) backgrounds, etc. and also for testing a transparency mask for pointer signals. If mask test fails on a `StyleBox` assigned as mask to a control, clicks and motion signals will go through it to the one below.

**Note:** For control nodes that have *Theme Properties*, the `focus` `StyleBox` is displayed over the `normal`, `hover` or `pressed` `StyleBox`. This makes the `focus` `StyleBox` more reusable across different nodes.

## 构造函数

### StyleBox

```csharp
public StyleBox()
```

## 属性

### ContentMarginBottom

```csharp
public float ContentMarginBottom { get; set; }
```

引擎名：`content_margin_bottom`

The bottom margin for the contents of this style box. Increasing this value reduces the space available to the contents from the bottom.

If this value is negative, it is ignored and a child-specific margin is used instead. For example, for [`StyleBoxFlat`](StyleBoxFlat.md), the border thickness (if any) is used instead.

It is up to the code using this style box to decide what these contents are: for example, a [`Button`](Button.md) respects this content margin for the textual contents of the button.

`GetMargin()` should be used to fetch this value as consumer instead of reading these properties directly. This is because it correctly respects negative values and the fallback mentioned above.

### ContentMarginLeft

```csharp
public float ContentMarginLeft { get; set; }
```

引擎名：`content_margin_left`

The left margin for the contents of this style box. Increasing this value reduces the space available to the contents from the left.

Refer to `ContentMarginBottom` for extra considerations.

### ContentMarginRight

```csharp
public float ContentMarginRight { get; set; }
```

引擎名：`content_margin_right`

The right margin for the contents of this style box. Increasing this value reduces the space available to the contents from the right.

Refer to `ContentMarginBottom` for extra considerations.

### ContentMarginTop

```csharp
public float ContentMarginTop { get; set; }
```

引擎名：`content_margin_top`

The top margin for the contents of this style box. Increasing this value reduces the space available to the contents from the top.

Refer to `ContentMarginBottom` for extra considerations.

## 方法

### Draw

```csharp
public void Draw(Rid canvasItem, Rect2 rect)
```

引擎名：`draw`

Draws this stylebox using a canvas item identified by the given [`Rid`](Rid.md).

The [`Rid`](Rid.md) value can either be the result of [`CanvasItem.GetCanvasItem`](CanvasItem.md) called on an existing [`CanvasItem`](CanvasItem.md)-derived node, or directly from creating a canvas item in the [`RenderingServer`](RenderingServer.md) with [`RenderingServer.CanvasItemCreate`](RenderingServer.md).

### GetContentMargin

```csharp
public float GetContentMargin(Side margin)
```

引擎名：`get_content_margin`

Returns the default margin of the specified [`Side`](Side.md).

### GetCurrentItemDrawn

```csharp
public CanvasItem GetCurrentItemDrawn()
```

引擎名：`get_current_item_drawn`

Returns the [`CanvasItem`](CanvasItem.md) that handles its [`CanvasItem.NotificationDraw`](CanvasItem.md) or [`CanvasItem.Draw`](CanvasItem.md) callback at this moment.

### GetMargin

```csharp
public float GetMargin(Side margin)
```

引擎名：`get_margin`

Returns the content margin offset for the specified [`Side`](Side.md).

Positive values reduce size inwards, unlike [`Control`](Control.md)'s margin values.

### GetMinimumSize

```csharp
public Vector2 GetMinimumSize()
```

引擎名：`get_minimum_size`

Returns the minimum size that this stylebox can be shrunk to.

### GetOffset

```csharp
public Vector2 GetOffset()
```

引擎名：`get_offset`

Returns the "offset" of a stylebox. This helper function returns a value equivalent to `Vector2(style.get_margin(MARGIN_LEFT), style.get_margin(MARGIN_TOP))`.

### SetContentMargin

```csharp
public void SetContentMargin(Side margin, float offset)
```

引擎名：`set_content_margin`

Sets the default value of the specified [`Side`](Side.md) to `offset` pixels.

### SetContentMarginAll

```csharp
public void SetContentMarginAll(float offset)
```

引擎名：`set_content_margin_all`

Sets the default margin to `offset` pixels for all sides.

### TestMask

```csharp
public bool TestMask(Vector2 point, Rect2 rect)
```

引擎名：`test_mask`

Test a position in a rectangle, return whether it passes the mask test.

### _Draw

```csharp
public virtual void _Draw(Rid toCanvasItem, Rect2 rect)
```

引擎名：`_draw`

### _GetDrawRect

```csharp
public virtual Rect2 _GetDrawRect(Rect2 rect)
```

引擎名：`_get_draw_rect`

### _GetMinimumSize

```csharp
public virtual Vector2 _GetMinimumSize()
```

引擎名：`_get_minimum_size`

Virtual method to be implemented by the user. Returns a custom minimum size that the stylebox must respect when drawing. By default `GetMinimumSize()` only takes content margins into account. This method can be overridden to add another size restriction. A combination of the default behavior and the output of this method will be used, to account for both sizes.

### _TestMask

```csharp
public virtual bool _TestMask(Vector2 point, Rect2 rect)
```

引擎名：`_test_mask`
