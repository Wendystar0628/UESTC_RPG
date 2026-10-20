<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StyleBoxTexture

命名空间 `Godot`；class；继承 [`StyleBox`](StyleBox.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A texture-based nine-patch [`StyleBox`](StyleBox.md), in a way similar to [`NinePatchRect`](NinePatchRect.md). This stylebox performs a 3×3 scaling of a texture, where only the center cell is fully stretched. This makes it possible to design bordered styles regardless of the stylebox's size.

## 构造函数

### StyleBoxTexture

```csharp
public StyleBoxTexture()
```

## 属性

### AxisStretchHorizontal

```csharp
public StyleBoxTexture.AxisStretchMode AxisStretchHorizontal { get; set; }
```

引擎名：`axis_stretch_horizontal`

Controls how the stylebox's texture will be stretched or tiled horizontally.

### AxisStretchVertical

```csharp
public StyleBoxTexture.AxisStretchMode AxisStretchVertical { get; set; }
```

引擎名：`axis_stretch_vertical`

Controls how the stylebox's texture will be stretched or tiled vertically.

### DrawCenter

```csharp
public bool DrawCenter { get; set; }
```

引擎名：`draw_center`

If `true`, the nine-patch texture's center tile will be drawn.

### ExpandMarginBottom

```csharp
public float ExpandMarginBottom { get; set; }
```

引擎名：`expand_margin_bottom`

Expands the bottom margin of this style box when drawing, causing it to be drawn larger than requested.

### ExpandMarginLeft

```csharp
public float ExpandMarginLeft { get; set; }
```

引擎名：`expand_margin_left`

Expands the left margin of this style box when drawing, causing it to be drawn larger than requested.

### ExpandMarginRight

```csharp
public float ExpandMarginRight { get; set; }
```

引擎名：`expand_margin_right`

Expands the right margin of this style box when drawing, causing it to be drawn larger than requested.

### ExpandMarginTop

```csharp
public float ExpandMarginTop { get; set; }
```

引擎名：`expand_margin_top`

Expands the top margin of this style box when drawing, causing it to be drawn larger than requested.

### ModulateColor

```csharp
public Color ModulateColor { get; set; }
```

引擎名：`modulate_color`

Modulates the color of the texture when this style box is drawn.

### RegionRect

```csharp
public Rect2 RegionRect { get; set; }
```

引擎名：`region_rect`

The region to use from the `Texture`.

This is equivalent to first wrapping the `Texture` in an [`AtlasTexture`](AtlasTexture.md) with the same region.

If empty (`Rect2(0, 0, 0, 0)`), the whole `Texture` is used.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

引擎名：`texture`

The texture to use when drawing this style box.

### TextureMarginBottom

```csharp
public float TextureMarginBottom { get; set; }
```

引擎名：`texture_margin_bottom`

Increases the bottom margin of the 3×3 texture box.

A higher value means more of the source texture is considered to be part of the bottom border of the 3×3 box.

This is also the value used as fallback for [`StyleBox.ContentMarginBottom`](StyleBox.md) if it is negative.

### TextureMarginLeft

```csharp
public float TextureMarginLeft { get; set; }
```

引擎名：`texture_margin_left`

Increases the left margin of the 3×3 texture box.

A higher value means more of the source texture is considered to be part of the left border of the 3×3 box.

This is also the value used as fallback for [`StyleBox.ContentMarginLeft`](StyleBox.md) if it is negative.

### TextureMarginRight

```csharp
public float TextureMarginRight { get; set; }
```

引擎名：`texture_margin_right`

Increases the right margin of the 3×3 texture box.

A higher value means more of the source texture is considered to be part of the right border of the 3×3 box.

This is also the value used as fallback for [`StyleBox.ContentMarginRight`](StyleBox.md) if it is negative.

### TextureMarginTop

```csharp
public float TextureMarginTop { get; set; }
```

引擎名：`texture_margin_top`

Increases the top margin of the 3×3 texture box.

A higher value means more of the source texture is considered to be part of the top border of the 3×3 box.

This is also the value used as fallback for [`StyleBox.ContentMarginTop`](StyleBox.md) if it is negative.

## 方法

### GetExpandMargin

```csharp
public float GetExpandMargin(Side margin)
```

引擎名：`get_expand_margin`

Returns the expand margin size of the specified [`Side`](Side.md).

### GetHAxisStretchMode

```csharp
public StyleBoxTexture.AxisStretchMode GetHAxisStretchMode()
```

引擎名：`get_h_axis_stretch_mode`

### GetModulate

```csharp
public Color GetModulate()
```

引擎名：`get_modulate`

### GetRegionRect

```csharp
public Rect2 GetRegionRect()
```

引擎名：`get_region_rect`

### GetTexture

```csharp
public Texture2D GetTexture()
```

引擎名：`get_texture`

### GetTextureMargin

```csharp
public float GetTextureMargin(Side margin)
```

引擎名：`get_texture_margin`

Returns the margin size of the specified [`Side`](Side.md).

### GetVAxisStretchMode

```csharp
public StyleBoxTexture.AxisStretchMode GetVAxisStretchMode()
```

引擎名：`get_v_axis_stretch_mode`

### IsDrawCenterEnabled

```csharp
public bool IsDrawCenterEnabled()
```

引擎名：`is_draw_center_enabled`

### SetDrawCenter

```csharp
public void SetDrawCenter(bool enable)
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

### SetHAxisStretchMode

```csharp
public void SetHAxisStretchMode(StyleBoxTexture.AxisStretchMode mode)
```

引擎名：`set_h_axis_stretch_mode`

### SetModulate

```csharp
public void SetModulate(Color color)
```

引擎名：`set_modulate`

### SetRegionRect

```csharp
public void SetRegionRect(Rect2 region)
```

引擎名：`set_region_rect`

### SetTexture

```csharp
public void SetTexture(Texture2D texture)
```

引擎名：`set_texture`

### SetTextureMargin

```csharp
public void SetTextureMargin(Side margin, float size)
```

引擎名：`set_texture_margin`

Sets the margin to `size` pixels for the specified [`Side`](Side.md).

### SetTextureMarginAll

```csharp
public void SetTextureMarginAll(float size)
```

引擎名：`set_texture_margin_all`

Sets the margin to `size` pixels for all sides.

### SetVAxisStretchMode

```csharp
public void SetVAxisStretchMode(StyleBoxTexture.AxisStretchMode mode)
```

引擎名：`set_v_axis_stretch_mode`

## AxisStretchMode

命名空间 `Godot`；enum

### 枚举值

- `Stretch = 0`：Stretch the stylebox's texture.
- `Tile = 1`：Repeats the stylebox's texture to match the stylebox's size according to the nine-patch system.
- `TileFit = 2`：Repeats the stylebox's texture to match the stylebox's size according to the nine-patch system.
