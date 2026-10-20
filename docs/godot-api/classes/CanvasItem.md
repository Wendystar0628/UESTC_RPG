<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CanvasItem

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Abstract base class for everything in 2D space. Canvas items are laid out in a tree; children inherit and extend their parent's transform. `CanvasItem` is extended by [`Control`](Control.md) for GUI-related nodes, and by [`Node2D`](Node2D.md) for 2D game objects.

Any `CanvasItem` can draw. For this, `QueueRedraw()` is called by the engine, then `NotificationDraw` will be received on idle time to request a redraw. Because of this, canvas items don't need to be redrawn on every frame, improving the performance significantly. Several functions for drawing on the `CanvasItem` are provided (see `draw_*` functions). However, they can only be used inside `_Draw()`, its corresponding [`GodotObject.Notification`](GodotObject.md) or methods connected to the `Draw` signal.

Canvas items are drawn in tree order on their canvas layer. By default, children are on top of their parents, so a root `CanvasItem` will be drawn behind everything. This behavior can be changed on a per-item basis.

A `CanvasItem` can be hidden, which will also hide its children. By adjusting various other properties of a `CanvasItem`, you can also modulate its color (via `Modulate` or `SelfModulate`), change its Z-index, blend mode, and more.

Note that properties like transform, modulation, and visibility are only propagated to *direct* `CanvasItem` child nodes. If there is a non-`CanvasItem` node in between, like [`Node`](Node.md) or [`AnimationPlayer`](AnimationPlayer.md), the `CanvasItem` nodes below will have an independent position and `Modulate` chain. See also `TopLevel`.

## 属性

### ClipChildren

```csharp
public CanvasItem.ClipChildrenMode ClipChildren { get; set; }
```

引擎名：`clip_children`

The mode in which this node clips its children, acting as a mask.

**Note:** Clipping nodes cannot be nested or placed within a [`CanvasGroup`](CanvasGroup.md). If an ancestor of this node clips its children or is a [`CanvasGroup`](CanvasGroup.md), then this node's clip mode should be set to [`CanvasItem.ClipChildrenMode.Disabled`](CanvasItem.md) to avoid unexpected behavior.

### LightMask

```csharp
public int LightMask { get; set; }
```

引擎名：`light_mask`

The rendering layers in which this `CanvasItem` responds to [`Light2D`](Light2D.md) nodes.

### Material

```csharp
public Material Material { get; set; }
```

引擎名：`material`

The material applied to this `CanvasItem`.

### Modulate

```csharp
public Color Modulate { get; set; }
```

引擎名：`modulate`

The color applied to this `CanvasItem`. This property does affect child `CanvasItem`s, unlike `SelfModulate` which only affects the node itself.

### OversamplingWithScale

```csharp
public CanvasItem.OversamplingWithScaleEnum OversamplingWithScale { get; set; }
```

引擎名：`oversampling_with_scale`

If enabled, oversampling for this `CanvasItem` is automatically adjusted with scale.

### SelfModulate

```csharp
public Color SelfModulate { get; set; }
```

引擎名：`self_modulate`

The color applied to this `CanvasItem`. This property does **not** affect child `CanvasItem`s, unlike `Modulate` which affects both the node itself and its children.

**Note:** Internal children are also not affected by this property (see the `include_internal` parameter in [`Node.AddChild`](Node.md)). For built-in nodes this includes sliders in [`ColorPicker`](ColorPicker.md), and the tab bar in [`TabContainer`](TabContainer.md).

### ShowBehindParent

```csharp
public bool ShowBehindParent { get; set; }
```

引擎名：`show_behind_parent`

If `true`, this node draws behind its parent.

### TextureFilter

```csharp
public CanvasItem.TextureFilterEnum TextureFilter { get; set; }
```

引擎名：`texture_filter`

The filtering mode used to render this `CanvasItem`'s texture(s).

### TextureRepeat

```csharp
public CanvasItem.TextureRepeatEnum TextureRepeat { get; set; }
```

引擎名：`texture_repeat`

The repeating mode used to render this `CanvasItem`'s texture(s). It affects what happens when the texture is sampled outside its extents, for example by setting a [`Sprite2D.RegionRect`](Sprite2D.md) that is larger than the texture or assigning [`Polygon2D`](Polygon2D.md) UV points outside the texture.

**Note:** [`TextureRect`](TextureRect.md) is not affected by `TextureRepeat`, as it uses its own texture repeating implementation.

### TopLevel

```csharp
public bool TopLevel { get; set; }
```

引擎名：`top_level`

If `true`, this `CanvasItem` will *not* inherit its transform from parent `CanvasItem`s. Its draw order will also be changed to make it draw on top of other `CanvasItem`s that do not have `TopLevel` set to `true`. The `CanvasItem` will effectively act as if it was placed as a child of a bare [`Node`](Node.md).

### UseParentMaterial

```csharp
public bool UseParentMaterial { get; set; }
```

引擎名：`use_parent_material`

If `true`, the parent `CanvasItem`'s `Material` is used as this node's material.

### VisibilityLayer

```csharp
public uint VisibilityLayer { get; set; }
```

引擎名：`visibility_layer`

The rendering layer in which this `CanvasItem` is rendered by [`Viewport`](Viewport.md) nodes. A [`Viewport`](Viewport.md) will render a `CanvasItem` if it and all its parents share a layer with the [`Viewport`](Viewport.md)'s canvas cull mask.

**Note:** A `CanvasItem` does not inherit its parents' visibility layers. This means that if a parent `CanvasItem` does not have all the same layers as its child, the child may not be visible even if both the parent and child have `Visible` set to `true`. For example, if a parent has layer 1 and a child has layer 2, the child will not be visible in a [`Viewport`](Viewport.md) with the canvas cull mask set to layer 1 or 2 (see [`Viewport.CanvasCullMask`](Viewport.md)). To ensure that both the parent and child are visible, the parent must have both layers 1 and 2, or the child must have `TopLevel` set to `true`.

### Visible

```csharp
public bool Visible { get; set; }
```

引擎名：`visible`

If `true`, this `CanvasItem` may be drawn. Whether this `CanvasItem` is actually drawn depends on the visibility of all of its `CanvasItem` ancestors. In other words: this `CanvasItem` will be drawn when `IsVisibleInTree()` returns `true` and all `CanvasItem` ancestors share at least one `VisibilityLayer` with this `CanvasItem`.

**Note:** For controls that inherit [`Popup`](Popup.md), the correct way to make them visible is to call one of the multiple `popup*()` functions instead.

### YSortEnabled

```csharp
public bool YSortEnabled { get; set; }
```

引擎名：`y_sort_enabled`

If `true`, this and child `CanvasItem` nodes with a higher Y position are rendered in front of nodes with a lower Y position. If `false`, this and child `CanvasItem` nodes are rendered normally in scene tree order.

With Y-sorting enabled on a parent node ('A') but disabled on a child node ('B'), the child node ('B') is sorted but its children ('C1', 'C2', etc.) render together on the same Y position as the child node ('B'). This allows you to organize the render order of a scene without changing the scene tree.

Nodes sort relative to each other only if they are on the same `ZIndex`.

### ZAsRelative

```csharp
public bool ZAsRelative { get; set; }
```

引擎名：`z_as_relative`

If `true`, this node's final Z index is relative to its parent's Z index.

For example, if `ZIndex` is `2` and its parent's final Z index is `3`, then this node's final Z index will be `5` (`2 + 3`).

### ZIndex

```csharp
public int ZIndex { get; set; }
```

引擎名：`z_index`

The order in which this node is drawn. A node with a higher Z index will display in front of others. Must be between [`RenderingServer.CanvasItemZMin`](RenderingServer.md) and [`RenderingServer.CanvasItemZMax`](RenderingServer.md) (inclusive).

**Note:** The Z index does **not** affect the order in which `CanvasItem` nodes are processed or the way input events are handled. This is especially important to keep in mind for [`Control`](Control.md) nodes.

## 方法

### DrawAnimationSlice

```csharp
public void DrawAnimationSlice(double animationLength, double sliceBegin, double sliceEnd, double offset = 0)
```

引擎名：`draw_animation_slice`

Subsequent drawing commands will be ignored unless they fall within the specified animation slice. This is a faster way to implement animations that loop on background rather than redrawing constantly.

### DrawArc

```csharp
public void DrawArc(Vector2 center, float radius, float startAngle, float endAngle, int pointCount, Color color, float width = -1f, bool antialiased = false)
```

引擎名：`draw_arc`

Draws an unfilled arc between the given angles with a uniform `color` and `width` and optional antialiasing (supported only for positive `width`). The larger the value of `pointCount`, the smoother the curve. `center` is defined in local space. For elliptical arcs, see `DrawEllipseArc()`. See also `DrawCircle()`.

If `width` is negative, it will be ignored and the arc will be drawn using [`RenderingServer.PrimitiveType.LineStrip`](RenderingServer.md). This means that when the CanvasItem is scaled, the arc will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

The arc is drawn from `startAngle` towards the value of `endAngle` so in clockwise direction if `start_angle < end_angle` and counter-clockwise otherwise. Passing the same angles but in reversed order will produce the same arc. If absolute difference of `startAngle` and `endAngle` is greater than `@GDScript.TAU` radians, then a full circle arc is drawn (i.e. arc will not overlap itself).

### DrawChar

```csharp
public void DrawChar(Font font, Vector2 pos, string char, int fontSize = 16, Color? modulate = null, float oversampling = 0f)
```

引擎名：`draw_char`

Draws a string first character using a custom font. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used. `pos` is defined in local space.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawChar

```csharp
public void DrawChar(Font font, Vector2 pos, string char, int fontSize, Color? modulate)
```

引擎名：`draw_char`

Draws a string first character using a custom font. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used. `pos` is defined in local space.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawCharOutline

```csharp
public void DrawCharOutline(Font font, Vector2 pos, string char, int fontSize = 16, int size = -1, Color? modulate = null, float oversampling = 0f)
```

引擎名：`draw_char_outline`

Draws a string first character outline using a custom font. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used. `pos` is defined in local space.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawCharOutline

```csharp
public void DrawCharOutline(Font font, Vector2 pos, string char, int fontSize, int size, Color? modulate)
```

引擎名：`draw_char_outline`

Draws a string first character outline using a custom font. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used. `pos` is defined in local space.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawCircle

```csharp
public void DrawCircle(Vector2 position, float radius, Color color)
```

引擎名：`draw_circle`

Draws a circle, with `position` defined in local space. See also `DrawEllipse()`, `DrawArc()`, `DrawPolyline()`, and `DrawPolygon()`.

If `filled` is `true`, the circle will be filled with the `color` specified. If `filled` is `false`, the circle will be drawn as a stroke with the `color` and `width` specified.

If `width` is negative, then two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

If `antialiased` is `true`, half transparent "feathers" will be attached to the boundary, making outlines smooth.

**Note:** `width` is only effective if `filled` is `false`.

### DrawCircle

```csharp
public void DrawCircle(Vector2 position, float radius, Color color, bool filled = true, float width = -1f, bool antialiased = false)
```

引擎名：`draw_circle`

Draws a circle, with `position` defined in local space. See also `DrawEllipse()`, `DrawArc()`, `DrawPolyline()`, and `DrawPolygon()`.

If `filled` is `true`, the circle will be filled with the `color` specified. If `filled` is `false`, the circle will be drawn as a stroke with the `color` and `width` specified.

If `width` is negative, then two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

If `antialiased` is `true`, half transparent "feathers" will be attached to the boundary, making outlines smooth.

**Note:** `width` is only effective if `filled` is `false`.

### DrawColoredPolygon

```csharp
public void DrawColoredPolygon(ReadOnlySpan<Vector2> points, Color color, ReadOnlySpan<Vector2> uvs, Texture2D texture)
```

引擎名：`draw_colored_polygon`

Draws a colored polygon of any number of points, convex or concave. The points in the `points` array are defined in local space. Unlike `DrawPolygon()`, a single color must be specified for the whole polygon.

**Note:** If you frequently redraw the same polygon with a large number of vertices, consider pre-calculating the triangulation with [`Geometry2D.TriangulatePolygon`](Geometry2D.md) and using `DrawMesh()`, `DrawMultimesh()`, or [`RenderingServer.CanvasItemAddTriangleArray`](RenderingServer.md).

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

### DrawColoredPolygon

```csharp
public void DrawColoredPolygon(Vector2[] points, Color color, Vector2[] uvs = null, Texture2D texture = null)
```

引擎名：`draw_colored_polygon`

Draws a colored polygon of any number of points, convex or concave. The points in the `points` array are defined in local space. Unlike `DrawPolygon()`, a single color must be specified for the whole polygon.

**Note:** If you frequently redraw the same polygon with a large number of vertices, consider pre-calculating the triangulation with [`Geometry2D.TriangulatePolygon`](Geometry2D.md) and using `DrawMesh()`, `DrawMultimesh()`, or [`RenderingServer.CanvasItemAddTriangleArray`](RenderingServer.md).

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

参数：
- `uvs`：If the parameter is null, then the default value is `Array.Empty<Vector2>()`.

### DrawDashedLine

```csharp
public void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width = -1f, float dash = 2f, bool aligned = true, bool antialiased = false)
```

引擎名：`draw_dashed_line`

Draws a dashed line from a 2D point to another, with a given color and width. The `from` and `to` positions are defined in local space. See also `DrawLine()`, `DrawMultiline()`, and `DrawPolyline()`.

If `width` is negative, then a two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the line parts will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

`dash` is the length of each dash in pixels, with the gap between each dash being the same length. If `aligned` is `true`, the length of the first and last dashes may be shortened or lengthened to allow the line to begin and end at the precise points defined by `from` and `to`. Both ends are always symmetrical when `aligned` is `true`. If `aligned` is `false`, all dashes will have the same length, but the line may appear incomplete at the end due to the dash length not dividing evenly into the line length. Only full dashes are drawn when `aligned` is `false`.

If `antialiased` is `true`, half transparent "feathers" will be attached to the boundary, making outlines smooth.

**Note:** `antialiased` is only effective if `width` is greater than `0.0`.

### DrawDashedLine

```csharp
public void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width, float dash, bool aligned)
```

引擎名：`draw_dashed_line`

Draws a dashed line from a 2D point to another, with a given color and width. The `from` and `to` positions are defined in local space. See also `DrawLine()`, `DrawMultiline()`, and `DrawPolyline()`.

If `width` is negative, then a two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the line parts will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

`dash` is the length of each dash in pixels, with the gap between each dash being the same length. If `aligned` is `true`, the length of the first and last dashes may be shortened or lengthened to allow the line to begin and end at the precise points defined by `from` and `to`. Both ends are always symmetrical when `aligned` is `true`. If `aligned` is `false`, all dashes will have the same length, but the line may appear incomplete at the end due to the dash length not dividing evenly into the line length. Only full dashes are drawn when `aligned` is `false`.

If `antialiased` is `true`, half transparent "feathers" will be attached to the boundary, making outlines smooth.

**Note:** `antialiased` is only effective if `width` is greater than `0.0`.

### DrawEllipse

```csharp
public void DrawEllipse(Vector2 position, float major, float minor, Color color, bool filled = true, float width = -1f, bool antialiased = false)
```

引擎名：`draw_ellipse`

Draws an ellipse with semi-major axis `major` and semi-minor axis `minor`. See also `DrawCircle()`, `DrawEllipseArc()`, `DrawPolyline()`, and `DrawPolygon()`.

If `filled` is `true`, the ellipse will be filled with the `color` specified. If `filled` is `false`, the ellipse will be drawn as a stroke with the `color` and `width` specified.

If `width` is negative, then two-point primitives will be drawn instead of four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

If `antialiased` is `true`, half transparent "feathers" will be attached to the boundary, making outlines smooth.

**Note:** `width` is only effective if `filled` is `false`.

### DrawEllipseArc

```csharp
public void DrawEllipseArc(Vector2 center, float major, float minor, float startAngle, float endAngle, int pointCount, Color color, float width = -1f, bool antialiased = false)
```

引擎名：`draw_ellipse_arc`

Draws an unfilled elliptical arc between the given angles with a uniform `color` and `width` and optional antialiasing (supported only for positive `width`). The larger the value of `pointCount`, the smoother the curve. For circular arcs, see `DrawArc()`. See also `DrawEllipse()`.

If `width` is negative, it will be ignored and the arc will be drawn using [`RenderingServer.PrimitiveType.LineStrip`](RenderingServer.md). This means that when the CanvasItem is scaled, the arc will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

The arc is drawn from `startAngle` towards the value of `endAngle` so in clockwise direction if `start_angle < end_angle` and counter-clockwise otherwise. Passing the same angles but in reversed order will produce the same arc. If absolute difference of `startAngle` and `endAngle` is greater than `@GDScript.TAU` radians, then a full ellipse is drawn (i.e. arc will not overlap itself).

### DrawEndAnimation

```csharp
public void DrawEndAnimation()
```

引擎名：`draw_end_animation`

After submitting all animations slices via `DrawAnimationSlice()`, this function can be used to revert drawing to its default state (all subsequent drawing commands will be visible). If you don't care about this particular use case, usage of this function after submitting the slices is not required.

### DrawLcdTextureRectRegion

```csharp
public void DrawLcdTextureRectRegion(Texture2D texture, Rect2 rect, Rect2 srcRect, Color? modulate = null)
```

引擎名：`draw_lcd_texture_rect_region`

Draws a textured rectangle region of the font texture with LCD subpixel anti-aliasing at a given position, optionally modulated by a color. The `rect` is defined in local space.

Texture is drawn using the following blend operation, blend mode of the [`CanvasItemMaterial`](CanvasItemMaterial.md) is ignored:

```csharp
dst.r = texture.r * modulate.r * modulate.a + dst.r * (1.0 - texture.r * modulate.a);
dst.g = texture.g * modulate.g * modulate.a + dst.g * (1.0 - texture.g * modulate.a);
dst.b = texture.b * modulate.b * modulate.a + dst.b * (1.0 - texture.b * modulate.a);
dst.a = modulate.a + dst.a * (1.0 - modulate.a);

```

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawLine

```csharp
public void DrawLine(Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false)
```

引擎名：`draw_line`

Draws a line from a 2D point to another, with a given color and width. It can be optionally antialiased. The `from` and `to` positions are defined in local space. See also `DrawDashedLine()`, `DrawMultiline()`, and `DrawPolyline()`.

If `width` is negative, then a two-point primitive will be drawn instead of a four-point one. This means that when the CanvasItem is scaled, the line will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

### DrawMesh

```csharp
public void DrawMesh(Mesh mesh, Texture2D texture, Transform2D? transform = null, Color? modulate = null)
```

引擎名：`draw_mesh`

Draws a [`Mesh`](Mesh.md) in 2D, using the provided texture. See [`MeshInstance2D`](MeshInstance2D.md) for related documentation. The `transform` is defined in local space.

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

参数：
- `transform`：If the parameter is null, then the default value is `Transform2D.Identity`.
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawMsdfTextureRectRegion

```csharp
public void DrawMsdfTextureRectRegion(Texture2D texture, Rect2 rect, Rect2 srcRect, Color? modulate = null, double outline = 0, double pixelRange = 4, double scale = 1)
```

引擎名：`draw_msdf_texture_rect_region`

Draws a textured rectangle region of the multichannel signed distance field texture at a given position, optionally modulated by a color. The `rect` is defined in local space. See [`FontFile.MultichannelSignedDistanceField`](FontFile.md) for more information and caveats about MSDF font rendering.

If `outline` is positive, each alpha channel value of pixel in region is set to maximum value of true distance in the `outline` radius.

Value of the `pixelRange` should the same that was used during distance field texture generation.

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawMultiline

```csharp
public void DrawMultiline(ReadOnlySpan<Vector2> points, Color color, float width)
```

引擎名：`draw_multiline`

Draws multiple disconnected lines with a uniform `width` and `color`. Each line is defined by two consecutive points from `points` array in local space, i.e. i-th segment consists of `points[2 * i]`, `points[2 * i + 1]` endpoints. When drawing large amounts of lines, this is faster than using individual `DrawLine()` calls. To draw interconnected lines, use `DrawPolyline()` instead.

If `width` is negative, then two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

**Note:** `antialiased` is only effective if `width` is greater than `0.0`.

### DrawMultiline

```csharp
public void DrawMultiline(ReadOnlySpan<Vector2> points, Color color, float width, bool antialiased)
```

引擎名：`draw_multiline`

Draws multiple disconnected lines with a uniform `width` and `color`. Each line is defined by two consecutive points from `points` array in local space, i.e. i-th segment consists of `points[2 * i]`, `points[2 * i + 1]` endpoints. When drawing large amounts of lines, this is faster than using individual `DrawLine()` calls. To draw interconnected lines, use `DrawPolyline()` instead.

If `width` is negative, then two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

**Note:** `antialiased` is only effective if `width` is greater than `0.0`.

### DrawMultiline

```csharp
public void DrawMultiline(Vector2[] points, Color color, float width = -1f, bool antialiased = false)
```

引擎名：`draw_multiline`

Draws multiple disconnected lines with a uniform `width` and `color`. Each line is defined by two consecutive points from `points` array in local space, i.e. i-th segment consists of `points[2 * i]`, `points[2 * i + 1]` endpoints. When drawing large amounts of lines, this is faster than using individual `DrawLine()` calls. To draw interconnected lines, use `DrawPolyline()` instead.

If `width` is negative, then two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

**Note:** `antialiased` is only effective if `width` is greater than `0.0`.

### DrawMultiline

```csharp
public void DrawMultiline(Vector2[] points, Color color, float width)
```

引擎名：`draw_multiline`

Draws multiple disconnected lines with a uniform `width` and `color`. Each line is defined by two consecutive points from `points` array in local space, i.e. i-th segment consists of `points[2 * i]`, `points[2 * i + 1]` endpoints. When drawing large amounts of lines, this is faster than using individual `DrawLine()` calls. To draw interconnected lines, use `DrawPolyline()` instead.

If `width` is negative, then two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

**Note:** `antialiased` is only effective if `width` is greater than `0.0`.

### DrawMultilineColors

```csharp
public void DrawMultilineColors(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width)
```

引擎名：`draw_multiline_colors`

Draws multiple disconnected lines with a uniform `width` and segment-by-segment coloring. Each segment is defined by two consecutive points from `points` array in local space and a corresponding color from `colors` array, i.e. i-th segment consists of `points[2 * i]`, `points[2 * i + 1]` endpoints and has `colors[i]` color. When drawing large amounts of lines, this is faster than using individual `DrawLine()` calls. To draw interconnected lines, use `DrawPolylineColors()` instead.

If `width` is negative, then two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

**Note:** `antialiased` is only effective if `width` is greater than `0.0`.

### DrawMultilineColors

```csharp
public void DrawMultilineColors(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width, bool antialiased)
```

引擎名：`draw_multiline_colors`

Draws multiple disconnected lines with a uniform `width` and segment-by-segment coloring. Each segment is defined by two consecutive points from `points` array in local space and a corresponding color from `colors` array, i.e. i-th segment consists of `points[2 * i]`, `points[2 * i + 1]` endpoints and has `colors[i]` color. When drawing large amounts of lines, this is faster than using individual `DrawLine()` calls. To draw interconnected lines, use `DrawPolylineColors()` instead.

If `width` is negative, then two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

**Note:** `antialiased` is only effective if `width` is greater than `0.0`.

### DrawMultilineColors

```csharp
public void DrawMultilineColors(Vector2[] points, Color[] colors, float width = -1f, bool antialiased = false)
```

引擎名：`draw_multiline_colors`

Draws multiple disconnected lines with a uniform `width` and segment-by-segment coloring. Each segment is defined by two consecutive points from `points` array in local space and a corresponding color from `colors` array, i.e. i-th segment consists of `points[2 * i]`, `points[2 * i + 1]` endpoints and has `colors[i]` color. When drawing large amounts of lines, this is faster than using individual `DrawLine()` calls. To draw interconnected lines, use `DrawPolylineColors()` instead.

If `width` is negative, then two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

**Note:** `antialiased` is only effective if `width` is greater than `0.0`.

### DrawMultilineColors

```csharp
public void DrawMultilineColors(Vector2[] points, Color[] colors, float width)
```

引擎名：`draw_multiline_colors`

Draws multiple disconnected lines with a uniform `width` and segment-by-segment coloring. Each segment is defined by two consecutive points from `points` array in local space and a corresponding color from `colors` array, i.e. i-th segment consists of `points[2 * i]`, `points[2 * i + 1]` endpoints and has `colors[i]` color. When drawing large amounts of lines, this is faster than using individual `DrawLine()` calls. To draw interconnected lines, use `DrawPolylineColors()` instead.

If `width` is negative, then two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

**Note:** `antialiased` is only effective if `width` is greater than `0.0`.

### DrawMultilineString

```csharp
public void DrawMultilineString(Font font, Vector2 pos, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1f, int fontSize = 16, int maxLines = -1, Color? modulate = null, TextServer.LineBreakFlag brkFlags = TextServer.LineBreakFlag., TextServer.JustificationFlag justificationFlags = TextServer.JustificationFlag., TextServer.Direction direction = TextServer.Direction.Auto, TextServer.Orientation orientation = TextServer.Orientation.Horizontal, float oversampling = 0f)
```

引擎名：`draw_multiline_string`

Breaks `text` into lines and draws it using the specified `font` at the `pos` in local space (top-left corner). The text will have its color multiplied by `modulate`. If `width` is greater than or equal to 0, the text will be clipped if it exceeds the specified width. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawMultilineString

```csharp
public void DrawMultilineString(Font font, Vector2 pos, string text, HorizontalAlignment alignment, float width, int fontSize, int maxLines, Color? modulate, TextServer.LineBreakFlag brkFlags, TextServer.JustificationFlag justificationFlags, TextServer.Direction direction, TextServer.Orientation orientation)
```

引擎名：`draw_multiline_string`

Breaks `text` into lines and draws it using the specified `font` at the `pos` in local space (top-left corner). The text will have its color multiplied by `modulate`. If `width` is greater than or equal to 0, the text will be clipped if it exceeds the specified width. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawMultilineStringOutline

```csharp
public void DrawMultilineStringOutline(Font font, Vector2 pos, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1f, int fontSize = 16, int maxLines = -1, int size = 1, Color? modulate = null, TextServer.LineBreakFlag brkFlags = TextServer.LineBreakFlag., TextServer.JustificationFlag justificationFlags = TextServer.JustificationFlag., TextServer.Direction direction = TextServer.Direction.Auto, TextServer.Orientation orientation = TextServer.Orientation.Horizontal, float oversampling = 0f)
```

引擎名：`draw_multiline_string_outline`

Breaks `text` to the lines and draws text outline using the specified `font` at the `pos` in local space (top-left corner). The text will have its color multiplied by `modulate`. If `width` is greater than or equal to 0, the text will be clipped if it exceeds the specified width. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawMultilineStringOutline

```csharp
public void DrawMultilineStringOutline(Font font, Vector2 pos, string text, HorizontalAlignment alignment, float width, int fontSize, int maxLines, int size, Color? modulate, TextServer.LineBreakFlag brkFlags, TextServer.JustificationFlag justificationFlags, TextServer.Direction direction, TextServer.Orientation orientation)
```

引擎名：`draw_multiline_string_outline`

Breaks `text` to the lines and draws text outline using the specified `font` at the `pos` in local space (top-left corner). The text will have its color multiplied by `modulate`. If `width` is greater than or equal to 0, the text will be clipped if it exceeds the specified width. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawMultimesh

```csharp
public void DrawMultimesh(MultiMesh multimesh, Texture2D texture)
```

引擎名：`draw_multimesh`

Draws a [`MultiMesh`](MultiMesh.md) in 2D with the provided texture. See [`MultiMeshInstance2D`](MultiMeshInstance2D.md) for related documentation.

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

### DrawPolygon

```csharp
public void DrawPolygon(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, Texture2D texture)
```

引擎名：`draw_polygon`

Draws a solid polygon of any number of points, convex or concave. Unlike `DrawColoredPolygon()`, each point's color can be changed individually. The `points` array is defined in local space. See also `DrawPolyline()` and `DrawPolylineColors()`. If you need more flexibility (such as being able to use bones), use [`RenderingServer.CanvasItemAddTriangleArray`](RenderingServer.md) instead.

**Note:** If you frequently redraw the same polygon with a large number of vertices, consider pre-calculating the triangulation with [`Geometry2D.TriangulatePolygon`](Geometry2D.md) and using `DrawMesh()`, `DrawMultimesh()`, or [`RenderingServer.CanvasItemAddTriangleArray`](RenderingServer.md).

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

### DrawPolygon

```csharp
public void DrawPolygon(Vector2[] points, Color[] colors, Vector2[] uvs = null, Texture2D texture = null)
```

引擎名：`draw_polygon`

Draws a solid polygon of any number of points, convex or concave. Unlike `DrawColoredPolygon()`, each point's color can be changed individually. The `points` array is defined in local space. See also `DrawPolyline()` and `DrawPolylineColors()`. If you need more flexibility (such as being able to use bones), use [`RenderingServer.CanvasItemAddTriangleArray`](RenderingServer.md) instead.

**Note:** If you frequently redraw the same polygon with a large number of vertices, consider pre-calculating the triangulation with [`Geometry2D.TriangulatePolygon`](Geometry2D.md) and using `DrawMesh()`, `DrawMultimesh()`, or [`RenderingServer.CanvasItemAddTriangleArray`](RenderingServer.md).

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

参数：
- `uvs`：If the parameter is null, then the default value is `Array.Empty<Vector2>()`.

### DrawPolyline

```csharp
public void DrawPolyline(ReadOnlySpan<Vector2> points, Color color, float width, bool antialiased)
```

引擎名：`draw_polyline`

Draws interconnected line segments with a uniform `color` and `width` and optional antialiasing (supported only for positive `width`). The `points` array is defined in local space. When drawing large amounts of lines, this is faster than using individual `DrawLine()` calls. To draw disconnected lines, use `DrawMultiline()` instead. See also `DrawPolygon()`.

If `width` is negative, it will be ignored and the polyline will be drawn using [`RenderingServer.PrimitiveType.LineStrip`](RenderingServer.md). This means that when the CanvasItem is scaled, the polyline will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

### DrawPolyline

```csharp
public void DrawPolyline(Vector2[] points, Color color, float width = -1f, bool antialiased = false)
```

引擎名：`draw_polyline`

Draws interconnected line segments with a uniform `color` and `width` and optional antialiasing (supported only for positive `width`). The `points` array is defined in local space. When drawing large amounts of lines, this is faster than using individual `DrawLine()` calls. To draw disconnected lines, use `DrawMultiline()` instead. See also `DrawPolygon()`.

If `width` is negative, it will be ignored and the polyline will be drawn using [`RenderingServer.PrimitiveType.LineStrip`](RenderingServer.md). This means that when the CanvasItem is scaled, the polyline will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

### DrawPolylineColors

```csharp
public void DrawPolylineColors(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width, bool antialiased)
```

引擎名：`draw_polyline_colors`

Draws interconnected line segments with a uniform `width`, point-by-point coloring, and optional antialiasing (supported only for positive `width`). Colors assigned to line points match by index between `points` and `colors`, i.e. each line segment is filled with a gradient between the colors of the endpoints. The `points` array is defined in local space. When drawing large amounts of lines, this is faster than using individual `DrawLine()` calls. To draw disconnected lines, use `DrawMultilineColors()` instead. See also `DrawPolygon()`.

If `width` is negative, it will be ignored and the polyline will be drawn using [`RenderingServer.PrimitiveType.LineStrip`](RenderingServer.md). This means that when the CanvasItem is scaled, the polyline will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

### DrawPolylineColors

```csharp
public void DrawPolylineColors(Vector2[] points, Color[] colors, float width = -1f, bool antialiased = false)
```

引擎名：`draw_polyline_colors`

Draws interconnected line segments with a uniform `width`, point-by-point coloring, and optional antialiasing (supported only for positive `width`). Colors assigned to line points match by index between `points` and `colors`, i.e. each line segment is filled with a gradient between the colors of the endpoints. The `points` array is defined in local space. When drawing large amounts of lines, this is faster than using individual `DrawLine()` calls. To draw disconnected lines, use `DrawMultilineColors()` instead. See also `DrawPolygon()`.

If `width` is negative, it will be ignored and the polyline will be drawn using [`RenderingServer.PrimitiveType.LineStrip`](RenderingServer.md). This means that when the CanvasItem is scaled, the polyline will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

### DrawPrimitive

```csharp
public void DrawPrimitive(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, Texture2D texture)
```

引擎名：`draw_primitive`

Draws a custom primitive. 1 point for a point, 2 points for a line, 3 points for a triangle, and 4 points for a quad. If 0 points or more than 4 points are specified, nothing will be drawn and an error message will be printed. The `points` array is defined in local space. See also `DrawLine()`, `DrawPolyline()`, `DrawPolygon()`, and `DrawRect()`.

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

### DrawPrimitive

```csharp
public void DrawPrimitive(Vector2[] points, Color[] colors, Vector2[] uvs, Texture2D texture = null)
```

引擎名：`draw_primitive`

Draws a custom primitive. 1 point for a point, 2 points for a line, 3 points for a triangle, and 4 points for a quad. If 0 points or more than 4 points are specified, nothing will be drawn and an error message will be printed. The `points` array is defined in local space. See also `DrawLine()`, `DrawPolyline()`, `DrawPolygon()`, and `DrawRect()`.

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

### DrawRect

```csharp
public void DrawRect(Rect2 rect, Color color, bool filled = true, float width = -1f, bool antialiased = false)
```

引擎名：`draw_rect`

Draws a rectangle. If `filled` is `true`, the rectangle will be filled with the `color` specified. If `filled` is `false`, the rectangle will be drawn as a stroke with the `color` and `width` specified. The `rect` is specified in local space. See also `DrawTextureRect()`.

If `width` is negative, then two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

If `antialiased` is `true`, half transparent "feathers" will be attached to the boundary, making outlines smooth.

**Note:** `width` is only effective if `filled` is `false`.

**Note:** Unfilled rectangles drawn with a negative `width` may not display perfectly. For example, corners may be missing or brighter due to overlapping lines (for a translucent `color`).

### DrawRect

```csharp
public void DrawRect(Rect2 rect, Color color, bool filled, float width)
```

引擎名：`draw_rect`

Draws a rectangle. If `filled` is `true`, the rectangle will be filled with the `color` specified. If `filled` is `false`, the rectangle will be drawn as a stroke with the `color` and `width` specified. The `rect` is specified in local space. See also `DrawTextureRect()`.

If `width` is negative, then two-point primitives will be drawn instead of a four-point ones. This means that when the CanvasItem is scaled, the lines will remain thin. If this behavior is not desired, then pass a positive `width` like `1.0`.

If `antialiased` is `true`, half transparent "feathers" will be attached to the boundary, making outlines smooth.

**Note:** `width` is only effective if `filled` is `false`.

**Note:** Unfilled rectangles drawn with a negative `width` may not display perfectly. For example, corners may be missing or brighter due to overlapping lines (for a translucent `color`).

### DrawSetTransform

```csharp
public void DrawSetTransform(Vector2 position, float rotation = 0f, Vector2? scale = null)
```

引擎名：`draw_set_transform`

Sets a custom local transform for drawing via components. Anything drawn afterwards will be transformed by this.

**Note:** [`FontFile.Oversampling`](FontFile.md) does *not* take `scale` into account. This means that scaling up/down will cause bitmap fonts and rasterized (non-MSDF) dynamic fonts to appear blurry or pixelated. To ensure text remains crisp regardless of scale, you can enable MSDF font rendering by enabling `ProjectSettings.gui/theme/default_font_multichannel_signed_distance_field` (applies to the default project font only), or enabling **Multichannel Signed Distance Field** in the import options of a DynamicFont for custom fonts. On system fonts, [`SystemFont.MultichannelSignedDistanceField`](SystemFont.md) can be enabled in the inspector.

参数：
- `scale`：If the parameter is null, then the default value is `new Vector2(1.0f, 1.0f)`.

### DrawSetTransformMatrix

```csharp
public void DrawSetTransformMatrix(Transform2D xform)
```

引擎名：`draw_set_transform_matrix`

Sets a custom local transform for drawing via matrix. Anything drawn afterwards will be transformed by this.

### DrawString

```csharp
public void DrawString(Font font, Vector2 pos, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1f, int fontSize = 16, Color? modulate = null, TextServer.JustificationFlag justificationFlags = TextServer.JustificationFlag., TextServer.Direction direction = TextServer.Direction.Auto, TextServer.Orientation orientation = TextServer.Orientation.Horizontal, float oversampling = 0f)
```

引擎名：`draw_string`

Draws `text` using the specified `font` at the `pos` in local space (bottom-left corner using the baseline of the font). The text will have its color multiplied by `modulate`. If `width` is greater than or equal to 0, the text will be clipped if it exceeds the specified width. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

**Example:** Draw "Hello world", using the project's default font:

```csharp
DrawString(ThemeDB.FallbackFont, new Vector2(64, 64), "Hello world", HorizontalAlignment.Left, -1, ThemeDB.FallbackFontSize);

```

See also [`Font.DrawString`](Font.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawString

```csharp
public void DrawString(Font font, Vector2 pos, string text, HorizontalAlignment alignment, float width, int fontSize, Color? modulate, TextServer.JustificationFlag justificationFlags, TextServer.Direction direction, TextServer.Orientation orientation)
```

引擎名：`draw_string`

Draws `text` using the specified `font` at the `pos` in local space (bottom-left corner using the baseline of the font). The text will have its color multiplied by `modulate`. If `width` is greater than or equal to 0, the text will be clipped if it exceeds the specified width. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

**Example:** Draw "Hello world", using the project's default font:

```csharp
DrawString(ThemeDB.FallbackFont, new Vector2(64, 64), "Hello world", HorizontalAlignment.Left, -1, ThemeDB.FallbackFontSize);

```

See also [`Font.DrawString`](Font.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawStringOutline

```csharp
public void DrawStringOutline(Font font, Vector2 pos, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1f, int fontSize = 16, int size = 1, Color? modulate = null, TextServer.JustificationFlag justificationFlags = TextServer.JustificationFlag., TextServer.Direction direction = TextServer.Direction.Auto, TextServer.Orientation orientation = TextServer.Orientation.Horizontal, float oversampling = 0f)
```

引擎名：`draw_string_outline`

Draws `text` outline using the specified `font` at the `pos` in local space (bottom-left corner using the baseline of the font). The text will have its color multiplied by `modulate`. If `width` is greater than or equal to 0, the text will be clipped if it exceeds the specified width. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawStringOutline

```csharp
public void DrawStringOutline(Font font, Vector2 pos, string text, HorizontalAlignment alignment, float width, int fontSize, int size, Color? modulate, TextServer.JustificationFlag justificationFlags, TextServer.Direction direction, TextServer.Orientation orientation)
```

引擎名：`draw_string_outline`

Draws `text` outline using the specified `font` at the `pos` in local space (bottom-left corner using the baseline of the font). The text will have its color multiplied by `modulate`. If `width` is greater than or equal to 0, the text will be clipped if it exceeds the specified width. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawStyleBox

```csharp
public void DrawStyleBox(StyleBox styleBox, Rect2 rect)
```

引擎名：`draw_style_box`

Draws a styled rectangle. The `rect` is defined in local space.

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

### DrawTexture

```csharp
public void DrawTexture(Texture2D texture, Vector2 position, Color? modulate = null)
```

引擎名：`draw_texture`

Draws a texture at a given position. The `position` is defined in local space.

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawTextureRect

```csharp
public void DrawTextureRect(Texture2D texture, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)
```

引擎名：`draw_texture_rect`

Draws a textured rectangle at a given position, optionally modulated by a color. The `rect` is defined in local space. If `transpose` is `true`, the texture will have its X and Y coordinates swapped. See also `DrawRect()` and `DrawTextureRectRegion()`.

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawTextureRectRegion

```csharp
public void DrawTextureRectRegion(Texture2D texture, Rect2 rect, Rect2 srcRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
```

引擎名：`draw_texture_rect_region`

Draws a textured rectangle from a texture's region (specified by `srcRect`) at a given position in local space, optionally modulated by a color. If `transpose` is `true`, the texture will have its X and Y coordinates swapped. See also `DrawTextureRect()`.

**Note:** Styleboxes, textures, and meshes stored only inside local variables should **not** be used with this method in GDScript, because the drawing operation doesn't begin immediately once this method is called. In GDScript, when the function with the local variables ends, the local variables get destroyed before the rendering takes place.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### ForceUpdateTransform

```csharp
public void ForceUpdateTransform()
```

引擎名：`force_update_transform`

Forces the node's transform to update. Fails if the node is not inside the tree. See also `GetTransform()`.

**Note:** For performance reasons, transform changes are usually accumulated and applied *once* at the end of the frame. The update propagates through `CanvasItem` children, as well. Therefore, use this method only when you need an up-to-date transform (such as during physics operations).

### GetCanvas

```csharp
public Rid GetCanvas()
```

引擎名：`get_canvas`

Returns the [`Rid`](Rid.md) of the [`World2D`](World2D.md) canvas where this node is registered to, used by the [`RenderingServer`](RenderingServer.md).

### GetCanvasItem

```csharp
public Rid GetCanvasItem()
```

引擎名：`get_canvas_item`

Returns the internal canvas item [`Rid`](Rid.md) used by the [`RenderingServer`](RenderingServer.md) for this node.

### GetCanvasLayerNode

```csharp
public CanvasLayer GetCanvasLayerNode()
```

引擎名：`get_canvas_layer_node`

Returns the [`CanvasLayer`](CanvasLayer.md) that contains this node, or `null` if the node is not in any [`CanvasLayer`](CanvasLayer.md).

### GetCanvasTransform

```csharp
public Transform2D GetCanvasTransform()
```

引擎名：`get_canvas_transform`

Returns the transform of this node, converted from its registered canvas's coordinate system to its viewport's coordinate system. See also [`Node.GetViewport`](Node.md).

### GetClipChildrenMode

```csharp
public CanvasItem.ClipChildrenMode GetClipChildrenMode()
```

引擎名：`get_clip_children_mode`

### GetGlobalMousePosition

```csharp
public Vector2 GetGlobalMousePosition()
```

引擎名：`get_global_mouse_position`

Returns mouse cursor's global position relative to the [`CanvasLayer`](CanvasLayer.md) that contains this node.

**Note:** For screen-space coordinates (e.g. when using a non-embedded [`Popup`](Popup.md)), you can use [`DisplayServer.MouseGetPosition`](DisplayServer.md).

### GetGlobalTransform

```csharp
public Transform2D GetGlobalTransform()
```

引擎名：`get_global_transform`

Returns the global transform matrix of this item, i.e. the combined transform up to the topmost `CanvasItem` node. The topmost item is a `CanvasItem` that either has no parent, has non-`CanvasItem` parent or it has `TopLevel` enabled.

### GetGlobalTransformWithCanvas

```csharp
public Transform2D GetGlobalTransformWithCanvas()
```

引擎名：`get_global_transform_with_canvas`

Returns the transform from the local coordinate system of this `CanvasItem` to the [`Viewport`](Viewport.md)s coordinate system.

### GetInstanceShaderParameter

```csharp
public Variant GetInstanceShaderParameter(StringName name)
```

引擎名：`get_instance_shader_parameter`

Get the value of a shader parameter as set on this instance.

### GetLightMask

```csharp
public int GetLightMask()
```

引擎名：`get_light_mask`

### GetLocalMousePosition

```csharp
public Vector2 GetLocalMousePosition()
```

引擎名：`get_local_mouse_position`

Returns the mouse's position in this `CanvasItem` using the local coordinate system of this `CanvasItem`.

### GetMaterial

```csharp
public Material GetMaterial()
```

引擎名：`get_material`

### GetModulate

```csharp
public Color GetModulate()
```

引擎名：`get_modulate`

### GetOversamplingWithScale

```csharp
public CanvasItem.OversamplingWithScaleEnum GetOversamplingWithScale()
```

引擎名：`get_oversampling_with_scale`

### GetScreenTransform

```csharp
public Transform2D GetScreenTransform()
```

引擎名：`get_screen_transform`

Returns the transform of this `CanvasItem` in global screen coordinates (i.e. taking window position into account). Mostly useful for editor plugins.

Equivalent to `GetGlobalTransformWithCanvas()` if the window is embedded (see [`Viewport.GuiEmbedSubwindows`](Viewport.md)).

### GetSelfModulate

```csharp
public Color GetSelfModulate()
```

引擎名：`get_self_modulate`

### GetTextureFilter

```csharp
public CanvasItem.TextureFilterEnum GetTextureFilter()
```

引擎名：`get_texture_filter`

### GetTextureRepeat

```csharp
public CanvasItem.TextureRepeatEnum GetTextureRepeat()
```

引擎名：`get_texture_repeat`

### GetTransform

```csharp
public Transform2D GetTransform()
```

引擎名：`get_transform`

Returns the transform matrix of this `CanvasItem`.

### GetUseParentMaterial

```csharp
public bool GetUseParentMaterial()
```

引擎名：`get_use_parent_material`

### GetViewportRect

```csharp
public Rect2 GetViewportRect()
```

引擎名：`get_viewport_rect`

Returns this node's viewport boundaries as a [`Rect2`](Rect2.md). See also [`Node.GetViewport`](Node.md).

### GetViewportTransform

```csharp
public Transform2D GetViewportTransform()
```

引擎名：`get_viewport_transform`

Returns the transform of this node, converted from its registered canvas's coordinate system to its viewport embedder's coordinate system. See also [`Viewport.GetFinalTransform`](Viewport.md) and [`Node.GetViewport`](Node.md).

### GetVisibilityLayer

```csharp
public uint GetVisibilityLayer()
```

引擎名：`get_visibility_layer`

### GetVisibilityLayerBit

```csharp
public bool GetVisibilityLayerBit(uint layer)
```

引擎名：`get_visibility_layer_bit`

Returns `true` if the layer at the given index is set in `VisibilityLayer`.

### GetWorld2D

```csharp
public World2D GetWorld2D()
```

Returns the [`World2D`](World2D.md) this node is registered to.

Usually, this is the same as this node's viewport (see [`Node.GetViewport`](Node.md) and [`Viewport.FindWorld2D`](Viewport.md)).

### GetZIndex

```csharp
public int GetZIndex()
```

引擎名：`get_z_index`

### Hide

```csharp
public void Hide()
```

引擎名：`hide`

Hide the `CanvasItem` if it's currently visible. This is equivalent to setting `Visible` to `false`.

### IsDrawBehindParentEnabled

```csharp
public bool IsDrawBehindParentEnabled()
```

引擎名：`is_draw_behind_parent_enabled`

### IsLocalTransformNotificationEnabled

```csharp
public bool IsLocalTransformNotificationEnabled()
```

引擎名：`is_local_transform_notification_enabled`

Returns `true` if the node receives `NotificationLocalTransformChanged` whenever its local transform changes. This is enabled with `SetNotifyLocalTransform()`.

### IsSetAsTopLevel

```csharp
public bool IsSetAsTopLevel()
```

引擎名：`is_set_as_top_level`

### IsTransformNotificationEnabled

```csharp
public bool IsTransformNotificationEnabled()
```

引擎名：`is_transform_notification_enabled`

Returns `true` if the node receives `NotificationTransformChanged` whenever its global transform changes. This is enabled with `SetNotifyTransform()`.

### IsVisible

```csharp
public bool IsVisible()
```

引擎名：`is_visible`

### IsVisibleInTree

```csharp
public bool IsVisibleInTree()
```

引擎名：`is_visible_in_tree`

Returns `true` if the node is present in the [`SceneTree`](SceneTree.md), its `Visible` property is `true` and all its ancestors are also visible. If any ancestor is hidden, this node will not be visible in the scene tree, and is therefore not drawn (see `_Draw()`).

Visibility is checked only in parent nodes that inherit from `CanvasItem`, [`CanvasLayer`](CanvasLayer.md), and [`Window`](Window.md). If the parent is of any other type (such as [`Node`](Node.md), [`AnimationPlayer`](AnimationPlayer.md), or [`Node3D`](Node3D.md)), it is assumed to be visible.

**Note:** This method does not take `VisibilityLayer` into account, so even if this method returns `true`, the node might end up not being rendered.

### IsYSortEnabled

```csharp
public bool IsYSortEnabled()
```

引擎名：`is_y_sort_enabled`

### IsZRelative

```csharp
public bool IsZRelative()
```

引擎名：`is_z_relative`

### MakeCanvasPositionLocal

```csharp
public Vector2 MakeCanvasPositionLocal(Vector2 viewportPoint)
```

引擎名：`make_canvas_position_local`

Transforms `viewportPoint` from the viewport's coordinates to this node's local coordinates.

For the opposite operation, use `GetGlobalTransformWithCanvas()`.

```csharp
var viewport_point = get_global_transform_with_canvas() * local_point

```

### MakeInputLocal

```csharp
public InputEvent MakeInputLocal(InputEvent event)
```

引擎名：`make_input_local`

Returns a copy of the given `event` with its coordinates converted from global space to this `CanvasItem`'s local space. If not possible, returns the same [`InputEvent`](InputEvent.md) unchanged.

### MoveToFront

```csharp
public void MoveToFront()
```

引擎名：`move_to_front`

Moves this node below its siblings, usually causing the node to draw on top of its siblings. Does nothing if this node does not have a parent. See also [`Node.MoveChild`](Node.md).

### QueueRedraw

```csharp
public void QueueRedraw()
```

引擎名：`queue_redraw`

Queues the `CanvasItem` to redraw. During idle time, if `CanvasItem` is visible, `NotificationDraw` is sent and `_Draw()` is called. This only occurs **once** per frame, even if this method has been called multiple times.

### SetAsTopLevel

```csharp
public void SetAsTopLevel(bool enable)
```

引擎名：`set_as_top_level`

### SetClipChildrenMode

```csharp
public void SetClipChildrenMode(CanvasItem.ClipChildrenMode mode)
```

引擎名：`set_clip_children_mode`

### SetDrawBehindParent

```csharp
public void SetDrawBehindParent(bool enable)
```

引擎名：`set_draw_behind_parent`

### SetInstanceShaderParameter

```csharp
public void SetInstanceShaderParameter(StringName name, Variant value)
```

引擎名：`set_instance_shader_parameter`

Set the value of a shader uniform for this instance only ([per-instance uniform]($DOCS_URL/tutorials/shaders/shader_reference/shading_language.html#per-instance-uniforms)). See also [`ShaderMaterial.SetShaderParameter`](ShaderMaterial.md) to assign a uniform on all instances using the same [`ShaderMaterial`](ShaderMaterial.md).

**Note:** For a shader uniform to be assignable on a per-instance basis, it *must* be defined with `instance uniform ...` rather than `uniform ...` in the shader code.

**Note:** `name` is case-sensitive and must match the name of the uniform in the code exactly (not the capitalized name in the inspector).

### SetLightMask

```csharp
public void SetLightMask(int lightMask)
```

引擎名：`set_light_mask`

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

引擎名：`set_material`

### SetModulate

```csharp
public void SetModulate(Color modulate)
```

引擎名：`set_modulate`

### SetNotifyLocalTransform

```csharp
public void SetNotifyLocalTransform(bool enable)
```

引擎名：`set_notify_local_transform`

If `true`, the node will receive `NotificationLocalTransformChanged` whenever its local transform changes.

**Note:** Many canvas items such as [`Bone2D`](Bone2D.md) or [`CollisionShape2D`](CollisionShape2D.md) automatically enable this in order to function correctly.

### SetNotifyTransform

```csharp
public void SetNotifyTransform(bool enable)
```

引擎名：`set_notify_transform`

If `true`, the node will receive `NotificationTransformChanged` whenever its global transform changes.

**Note:** Many canvas items such as [`Camera2D`](Camera2D.md) or [`Light2D`](Light2D.md) automatically enable this in order to function correctly.

### SetOversamplingWithScale

```csharp
public void SetOversamplingWithScale(CanvasItem.OversamplingWithScaleEnum enabled)
```

引擎名：`set_oversampling_with_scale`

### SetSelfModulate

```csharp
public void SetSelfModulate(Color selfModulate)
```

引擎名：`set_self_modulate`

### SetTextureFilter

```csharp
public void SetTextureFilter(CanvasItem.TextureFilterEnum mode)
```

引擎名：`set_texture_filter`

### SetTextureRepeat

```csharp
public void SetTextureRepeat(CanvasItem.TextureRepeatEnum mode)
```

引擎名：`set_texture_repeat`

### SetUseParentMaterial

```csharp
public void SetUseParentMaterial(bool enable)
```

引擎名：`set_use_parent_material`

### SetVisibilityLayer

```csharp
public void SetVisibilityLayer(uint layer)
```

引擎名：`set_visibility_layer`

### SetVisibilityLayerBit

```csharp
public void SetVisibilityLayerBit(uint layer, bool enabled)
```

引擎名：`set_visibility_layer_bit`

Set/clear individual bits on the rendering visibility layer. This simplifies editing this `CanvasItem`'s visibility layer.

### SetVisible

```csharp
public void SetVisible(bool visible)
```

引擎名：`set_visible`

### SetYSortEnabled

```csharp
public void SetYSortEnabled(bool enabled)
```

引擎名：`set_y_sort_enabled`

### SetZAsRelative

```csharp
public void SetZAsRelative(bool enable)
```

引擎名：`set_z_as_relative`

### SetZIndex

```csharp
public void SetZIndex(int zIndex)
```

引擎名：`set_z_index`

### Show

```csharp
public void Show()
```

引擎名：`show`

Show the `CanvasItem` if it's currently hidden. This is equivalent to setting `Visible` to `true`.

**Note:** For controls that inherit [`Popup`](Popup.md), the correct way to make them visible is to call one of the multiple `popup*()` functions instead.

### _Draw

```csharp
public virtual void _Draw()
```

引擎名：`_draw`

Called when `CanvasItem` has been requested to redraw (after `QueueRedraw()` is called, either manually or by the engine).

Corresponds to the `NotificationDraw` notification in [`GodotObject.Notification`](GodotObject.md).

## 事件（信号）

### Draw

```csharp
public event Action Draw
```

引擎名：`draw`

Emitted when the `CanvasItem` must redraw, *after* the related `NotificationDraw` notification, and *before* `_Draw()` is called.

**Note:** Deferred connections do not allow drawing through the `draw_*` methods.

### Hidden

```csharp
public event Action Hidden
```

引擎名：`hidden`

Emitted when this node becomes hidden, i.e. it's no longer visible in the tree (see `IsVisibleInTree()`).

### ItemRectChanged

```csharp
public event Action ItemRectChanged
```

引擎名：`item_rect_changed`

Emitted when the `CanvasItem`'s boundaries (position or size) change, or when an action took place that may have affected these boundaries (e.g. changing [`Sprite2D.Texture`](Sprite2D.md)).

### VisibilityChanged

```csharp
public event Action VisibilityChanged
```

引擎名：`visibility_changed`

Emitted when the `CanvasItem`'s visibility changes, either because its own `Visible` property changed or because its visibility in the tree changed (see `IsVisibleInTree()`).

This signal is emitted *after* the related `NotificationVisibilityChanged` notification.

## 常量

### NotificationDraw

```csharp
public const long NotificationDraw = 30
```

The `CanvasItem` is requested to draw (see `_Draw()`).

### NotificationEnterCanvas

```csharp
public const long NotificationEnterCanvas = 32
```

The `CanvasItem` has entered the canvas.

### NotificationExitCanvas

```csharp
public const long NotificationExitCanvas = 33
```

The `CanvasItem` has exited the canvas.

This notification is sent in reversed order.

### NotificationLocalTransformChanged

```csharp
public const long NotificationLocalTransformChanged = 35
```

Notification received when this node's transform changes, if `IsLocalTransformNotificationEnabled()` is `true`. This is not received when a parent [`Node2D`](Node2D.md)'s transform changes. See also `SetNotifyLocalTransform()`.

**Note:** Many canvas items such as [`Camera2D`](Camera2D.md) or [`CollisionShape2D`](CollisionShape2D.md) automatically enable this in order to function correctly.

### NotificationTransformChanged

```csharp
public const long NotificationTransformChanged = 2000
```

Notification received when this node's global transform changes, if `IsTransformNotificationEnabled()` is `true`. See also `SetNotifyTransform()` and `GetTransform()`.

**Note:** Many canvas items such as [`Camera2D`](Camera2D.md) or [`CollisionObject2D`](CollisionObject2D.md) automatically enable this in order to function correctly.

### NotificationVisibilityChanged

```csharp
public const long NotificationVisibilityChanged = 31
```

Notification received when this node's visibility changes (see `Visible` and `IsVisibleInTree()`).

This notification is received *before* the related `VisibilityChanged` signal.

### NotificationWorld2DChanged

```csharp
public const long NotificationWorld2DChanged = 36
```

Notification received when this `CanvasItem` is registered to a new [`World2D`](World2D.md) (see `GetWorld2D()`).

## ClipChildrenMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Children are drawn over this node and are not clipped.
- `Only = 1`：This node is used as a mask and is **not** drawn.
- `AndDraw = 2`：This node is used as a mask and is also drawn.
- `Max = 3`：Represents the size of the `CanvasItem.ClipChildrenMode` enum.

## OversamplingWithScaleEnum

命名空间 `Godot`；enum

### 枚举值

- `ParentNode = 0`：The [`CanvasItem`](CanvasItem.md) will inherit the oversampling mode from its parent.
- `Disabled = 1`：The oversampling is not affected by [`CanvasItem`](CanvasItem.md) scale, and is equal to the [`Viewport`](Viewport.md) oversampling.
- `Enabled = 2`：The oversampling is a product of [`CanvasItem`](CanvasItem.md) scale and [`Viewport`](Viewport.md) oversampling.
- `Max = 3`：Represents the size of the `CanvasItem.OversamplingWithScaleEnum` enum.

## TextureFilterEnum

命名空间 `Godot`；enum

### 枚举值

- `ParentNode = 0`：The [`CanvasItem`](CanvasItem.md) will inherit the filter from its parent.
- `Nearest = 1`：The texture filter reads from the nearest pixel only.
- `Linear = 2`：The texture filter blends between the nearest 4 pixels.
- `NearestWithMipmaps = 3`：The texture filter reads from the nearest pixel and blends between the nearest 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`).
- `LinearWithMipmaps = 4`：The texture filter blends between the nearest 4 pixels and between the nearest 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`).
- `NearestWithMipmapsAnisotropic = 5`：The texture filter reads from the nearest pixel and blends between 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`) based on the angle bet…
- `LinearWithMipmapsAnisotropic = 6`：The texture filter blends between the nearest 4 pixels and blends between 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`) based on the an…
- `Max = 7`：Represents the size of the `CanvasItem.TextureFilterEnum` enum.

## TextureRepeatEnum

命名空间 `Godot`；enum

### 枚举值

- `ParentNode = 0`：The [`CanvasItem`](CanvasItem.md) will inherit the repeat mode from its parent.
- `Disabled = 1`：The texture does not repeat.
- `Enabled = 2`：The texture repeats when exceeding the texture's size.
- `Mirror = 3`：The texture repeats when the exceeding the texture's size in a "2×2 tiled mode".
- `Max = 4`：Represents the size of the `CanvasItem.TextureRepeatEnum` enum.
