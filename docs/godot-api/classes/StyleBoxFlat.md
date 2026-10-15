<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StyleBoxFlat

命名空间 `Godot`；class；继承 [`StyleBox`](StyleBox.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

By configuring various properties of this style box, you can achieve many common looks without the need of a texture. This includes optionally rounded borders, antialiasing, shadows, and skew.

Setting corner radius to high values is allowed. As soon as corners overlap, the stylebox will switch to a relative system:

```csharp
height = 30
corner_radius_top_left = 50
corner_radius_bottom_left = 100

```

The relative system now would take the 1:2 ratio of the two left corners to calculate the actual corner width. Both corners added will **never** be more than the height. Result:

```csharp
corner_radius_top_left: 10
corner_radius_bottom_left: 20

```

## 构造函数

### StyleBoxFlat

```csharp
public StyleBoxFlat()
```

## 属性

### AntiAliasing

```csharp
public bool AntiAliasing { get; set; }
```

引擎名：`anti_aliasing`

Antialiasing draws a small ring around the edges, which fades to transparency. As a result, edges look much smoother. This is only noticeable when using rounded corners or `Skew`.

**Note:** When using beveled corners with 45-degree angles (`CornerDetail` = 1), it is recommended to set `AntiAliasing` to `false` to ensure crisp visuals and avoid possible visual glitches.

### AntiAliasingSize

```csharp
public float AntiAliasingSize { get; set; }
```

引擎名：`anti_aliasing_size`

This changes the size of the antialiasing effect. `1.0` is recommended for an optimal result at 100% scale, identical to how rounded rectangles are rendered in web browsers and most vector drawing software.

**Note:** Higher values may produce a blur effect but can also create undesired artifacts on small boxes with large-radius corners.

### BgColor

```csharp
public Color BgColor { get; set; }
```

引擎名：`bg_color`

The background color of the stylebox.

### BorderBlend

```csharp
public bool BorderBlend { get; set; }
```

引擎名：`border_blend`

If `true`, the border will fade into the background color.

### BorderColor

```csharp
public Color BorderColor { get; set; }
```

引擎名：`border_color`

Sets the color of the border.

### BorderWidthBottom

```csharp
public int BorderWidthBottom { get; set; }
```

引擎名：`border_width_bottom`

Border width for the bottom border.

### BorderWidthLeft

```csharp
public int BorderWidthLeft { get; set; }
```

引擎名：`border_width_left`

Border width for the left border.

### BorderWidthRight

```csharp
public int BorderWidthRight { get; set; }
```

引擎名：`border_width_right`

Border width for the right border.

### BorderWidthTop

```csharp
public int BorderWidthTop { get; set; }
```

引擎名：`border_width_top`

Border width for the top border.

### CornerDetail

```csharp
public int CornerDetail { get; set; }
```

引擎名：`corner_detail`

This sets the number of vertices used for each corner. Higher values result in rounder corners but take more processing power to compute. When choosing a value, you should take the corner radius (`SetCornerRadiusAll()`) into account.

For corner radii less than 10, `4` or `5` should be enough. For corner radii less than 30, values between `8` and `12` should be enough.

A corner detail of `1` will result in chamfered corners instead of rounded corners, which is useful for some artistic effects.

### CornerRadiusBottomLeft

```csharp
public int CornerRadiusBottomLeft { get; set; }
```

引擎名：`corner_radius_bottom_left`

The bottom-left corner's radius. If `0`, the corner is not rounded.

### CornerRadiusBottomRight

```csharp
public int CornerRadiusBottomRight { get; set; }
```

引擎名：`corner_radius_bottom_right`

The bottom-right corner's radius. If `0`, the corner is not rounded.

### CornerRadiusTopLeft

```csharp
public int CornerRadiusTopLeft { get; set; }
```

引擎名：`corner_radius_top_left`

The top-left corner's radius. If `0`, the corner is not rounded.

### CornerRadiusTopRight

```csharp
public int CornerRadiusTopRight { get; set; }
```

引擎名：`corner_radius_top_right`

The top-right corner's radius. If `0`, the corner is not rounded.

### DrawCenter

```csharp
public bool DrawCenter { get; set; }
```

引擎名：`draw_center`

Toggles drawing of the inner part of the stylebox.

### ExpandMarginBottom

```csharp
public float ExpandMarginBottom { get; set; }
```

引擎名：`expand_margin_bottom`

Expands the stylebox outside of the control rect on the bottom edge. Useful in combination with `BorderWidthBottom` to draw a border outside the control rect.

**Note:** Unlike [`StyleBox.ContentMarginBottom`](StyleBox.md), `ExpandMarginBottom` does *not* affect the size of the clickable area for [`Control`](Control.md)s. This can negatively impact usability if used wrong, as the user may try to click an area of the StyleBox that cannot actually receive clicks.

### ExpandMarginLeft

```csharp
public float ExpandMarginLeft { get; set; }
```

引擎名：`expand_margin_left`

Expands the stylebox outside of the control rect on the left edge. Useful in combination with `BorderWidthLeft` to draw a border outside the control rect.

**Note:** Unlike [`StyleBox.ContentMarginLeft`](StyleBox.md), `ExpandMarginLeft` does *not* affect the size of the clickable area for [`Control`](Control.md)s. This can negatively impact usability if used wrong, as the user may try to click an area of the StyleBox that cannot actually receive clicks.

### ExpandMarginRight

```csharp
public float ExpandMarginRight { get; set; }
```

引擎名：`expand_margin_right`

Expands the stylebox outside of the control rect on the right edge. Useful in combination with `BorderWidthRight` to draw a border outside the control rect.

**Note:** Unlike [`StyleBox.ContentMarginRight`](StyleBox.md), `ExpandMarginRight` does *not* affect the size of the clickable area for [`Control`](Control.md)s. This can negatively impact usability if used wrong, as the user may try to click an area of the StyleBox that cannot actually receive clicks.

### ExpandMarginTop

```csharp
public float ExpandMarginTop { get; set; }
```

引擎名：`expand_margin_top`

Expands the stylebox outside of the control rect on the top edge. Useful in combination with `BorderWidthTop` to draw a border outside the control rect.

**Note:** Unlike [`StyleBox.ContentMarginTop`](StyleBox.md), `ExpandMarginTop` does *not* affect the size of the clickable area for [`Control`](Control.md)s. This can negatively impact usability if used wrong, as the user may try to click an area of the StyleBox that cannot actually receive clicks.

### ShadowColor

```csharp
public Color ShadowColor { get; set; }
```

引擎名：`shadow_color`

The color of the shadow. This has no effect if `ShadowSize` is lower than 1.

### ShadowOffset

```csharp
public Vector2 ShadowOffset { get; set; }
```

引擎名：`shadow_offset`

The shadow offset in pixels. Adjusts the position of the shadow relatively to the stylebox.

### ShadowSize

```csharp
public int ShadowSize { get; set; }
```

引擎名：`shadow_size`

The shadow size in pixels.

### Skew

```csharp
public Vector2 Skew { get; set; }
```

引擎名：`skew`

If set to a non-zero value on either axis, `Skew` distorts the StyleBox horizontally and/or vertically. This can be used for "futuristic"-style UIs. Positive values skew the StyleBox towards the right (X axis) and upwards (Y axis), while negative values skew the StyleBox towards the left (X axis) and downwards (Y axis).

**Note:** To ensure text does not touch the StyleBox's edges, consider increasing the [`StyleBox`](StyleBox.md)'s content margin (see [`StyleBox.ContentMarginBottom`](StyleBox.md)). It is preferable to increase the content margin instead of the expand margin (see `ExpandMarginBottom`), as increasing the expand margin does not increase the size of the clickable area for [`Control`](Control.md)s.

## 方法

### GetAASize

```csharp
public float GetAASize()
```

引擎名：`get_aa_size`

### GetBgColor

```csharp
public Color GetBgColor()
```

引擎名：`get_bg_color`

### GetBorderBlend

```csharp
public bool GetBorderBlend()
```

引擎名：`get_border_blend`

### GetBorderColor

```csharp
public Color GetBorderColor()
```

引擎名：`get_border_color`

### GetBorderWidth

```csharp
public int GetBorderWidth(Side margin)
```

引擎名：`get_border_width`

Returns the specified [`Side`](Side.md)'s border width.

### GetBorderWidthMin

```csharp
public int GetBorderWidthMin()
```

引擎名：`get_border_width_min`

Returns the smallest border width out of all four borders.

### GetCornerDetail

```csharp
public int GetCornerDetail()
```

引擎名：`get_corner_detail`

### GetCornerRadius

```csharp
public int GetCornerRadius(Corner corner)
```

引擎名：`get_corner_radius`

Returns the given `corner`'s radius.

### GetExpandMargin

```csharp
public float GetExpandMargin(Side margin)
```

引擎名：`get_expand_margin`

Returns the size of the specified [`Side`](Side.md)'s expand margin.

### GetShadowColor

```csharp
public Color GetShadowColor()
```

引擎名：`get_shadow_color`

### GetShadowOffset

```csharp
public Vector2 GetShadowOffset()
```

引擎名：`get_shadow_offset`

### GetShadowSize

```csharp
public int GetShadowSize()
```

引擎名：`get_shadow_size`

### GetSkew

```csharp
public Vector2 GetSkew()
```

引擎名：`get_skew`

### IsAntiAliased

```csharp
public bool IsAntiAliased()
```

引擎名：`is_anti_aliased`

### IsDrawCenterEnabled

```csharp
public bool IsDrawCenterEnabled()
```

引擎名：`is_draw_center_enabled`

### SetAASize

```csharp
public void SetAASize(float size)
```

引擎名：`set_aa_size`

### SetAntiAliased

```csharp
public void SetAntiAliased(bool antiAliased)
```

引擎名：`set_anti_aliased`

### SetBgColor

```csharp
public void SetBgColor(Color color)
```

引擎名：`set_bg_color`

### SetBorderBlend

```csharp
public void SetBorderBlend(bool blend)
```

引擎名：`set_border_blend`

### SetBorderColor

```csharp
public void SetBorderColor(Color color)
```

引擎名：`set_border_color`

### SetBorderWidth

```csharp
public void SetBorderWidth(Side margin, int width)
```

引擎名：`set_border_width`

Sets the specified [`Side`](Side.md)'s border width to `width` pixels.

### SetBorderWidthAll

```csharp
public void SetBorderWidthAll(int width)
```

引擎名：`set_border_width_all`

Sets the border width to `width` pixels for all sides.

### SetCornerDetail

```csharp
public void SetCornerDetail(int detail)
```

引擎名：`set_corner_detail`

### SetCornerRadius

```csharp
public void SetCornerRadius(Corner corner, int radius)
```

引擎名：`set_corner_radius`

Sets the corner radius to `radius` pixels for the given `corner`.

### SetCornerRadiusAll

```csharp
public void SetCornerRadiusAll(int radius)
```

引擎名：`set_corner_radius_all`

Sets the corner radius to `radius` pixels for all corners.

### SetDrawCenter

```csharp
public void SetDrawCenter(bool drawCenter)
```

引擎名：`set_draw_center`

### SetExpandMargin

```csharp
public void SetExpandMargin(Side margin, float size)
```

引擎名：`set_expand_margin`

Sets the expand margin to `size` pixels for the specified [`Side`](Side.md).

### SetExpandMarginAll

```csharp
public void SetExpandMarginAll(float size)
```

引擎名：`set_expand_margin_all`

Sets the expand margin to `size` pixels for all sides.

### SetShadowColor

```csharp
public void SetShadowColor(Color color)
```

引擎名：`set_shadow_color`

### SetShadowOffset

```csharp
public void SetShadowOffset(Vector2 offset)
```

引擎名：`set_shadow_offset`

### SetShadowSize

```csharp
public void SetShadowSize(int size)
```

引擎名：`set_shadow_size`

### SetSkew

```csharp
public void SetSkew(Vector2 skew)
```

引擎名：`set_skew`
