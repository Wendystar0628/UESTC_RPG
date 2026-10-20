<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NinePatchRect

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Also known as 9-slice panels, `NinePatchRect` produces clean panels of any size based on a small texture. To do so, it splits the texture in a 3×3 grid. When you scale the node, it tiles the texture's edges horizontally or vertically, tiles the center on both axes, and leaves the corners unchanged.

## 构造函数

### NinePatchRect

```csharp
public NinePatchRect()
```

## 属性

### AxisStretchHorizontal

```csharp
public NinePatchRect.AxisStretchMode AxisStretchHorizontal { get; set; }
```

引擎名：`axis_stretch_horizontal`

The stretch mode to use for horizontal stretching/tiling.

### AxisStretchVertical

```csharp
public NinePatchRect.AxisStretchMode AxisStretchVertical { get; set; }
```

引擎名：`axis_stretch_vertical`

The stretch mode to use for vertical stretching/tiling.

### DrawCenter

```csharp
public bool DrawCenter { get; set; }
```

引擎名：`draw_center`

If `true`, draw the panel's center. Else, only draw the 9-slice's borders.

### PatchMarginBottom

```csharp
public int PatchMarginBottom { get; set; }
```

引擎名：`patch_margin_bottom`

The height of the 9-slice's bottom row. A margin of 16 means the 9-slice's bottom corners and side will have a height of 16 pixels. You can set all 4 margin values individually to create panels with non-uniform borders.

### PatchMarginLeft

```csharp
public int PatchMarginLeft { get; set; }
```

引擎名：`patch_margin_left`

The width of the 9-slice's left column. A margin of 16 means the 9-slice's left corners and side will have a width of 16 pixels. You can set all 4 margin values individually to create panels with non-uniform borders.

### PatchMarginRight

```csharp
public int PatchMarginRight { get; set; }
```

引擎名：`patch_margin_right`

The width of the 9-slice's right column. A margin of 16 means the 9-slice's right corners and side will have a width of 16 pixels. You can set all 4 margin values individually to create panels with non-uniform borders.

### PatchMarginTop

```csharp
public int PatchMarginTop { get; set; }
```

引擎名：`patch_margin_top`

The height of the 9-slice's top row. A margin of 16 means the 9-slice's top corners and side will have a height of 16 pixels. You can set all 4 margin values individually to create panels with non-uniform borders.

### RegionRect

```csharp
public Rect2 RegionRect { get; set; }
```

引擎名：`region_rect`

Rectangular region of the texture to sample from. If you're working with an atlas, use this property to define the area the 9-slice should use. All other properties are relative to this one. If the rect is empty, NinePatchRect will use the whole texture.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

引擎名：`texture`

The node's texture resource.

## 方法

### GetHAxisStretchMode

```csharp
public NinePatchRect.AxisStretchMode GetHAxisStretchMode()
```

引擎名：`get_h_axis_stretch_mode`

### GetPatchMargin

```csharp
public int GetPatchMargin(Side margin)
```

引擎名：`get_patch_margin`

Returns the size of the margin on the specified [`Side`](Side.md).

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

### GetVAxisStretchMode

```csharp
public NinePatchRect.AxisStretchMode GetVAxisStretchMode()
```

引擎名：`get_v_axis_stretch_mode`

### IsDrawCenterEnabled

```csharp
public bool IsDrawCenterEnabled()
```

引擎名：`is_draw_center_enabled`

### SetDrawCenter

```csharp
public void SetDrawCenter(bool drawCenter)
```

引擎名：`set_draw_center`

### SetHAxisStretchMode

```csharp
public void SetHAxisStretchMode(NinePatchRect.AxisStretchMode mode)
```

引擎名：`set_h_axis_stretch_mode`

### SetPatchMargin

```csharp
public void SetPatchMargin(Side margin, int value)
```

引擎名：`set_patch_margin`

Sets the size of the margin on the specified [`Side`](Side.md) to `value` pixels.

### SetRegionRect

```csharp
public void SetRegionRect(Rect2 rect)
```

引擎名：`set_region_rect`

### SetTexture

```csharp
public void SetTexture(Texture2D texture)
```

引擎名：`set_texture`

### SetVAxisStretchMode

```csharp
public void SetVAxisStretchMode(NinePatchRect.AxisStretchMode mode)
```

引擎名：`set_v_axis_stretch_mode`

## 事件（信号）

### TextureChanged

```csharp
public event Action TextureChanged
```

引擎名：`texture_changed`

Emitted when the node's texture changes.

## AxisStretchMode

命名空间 `Godot`；enum

### 枚举值

- `Stretch = 0`：Stretches the center texture across the NinePatchRect.
- `Tile = 1`：Repeats the center texture across the NinePatchRect.
- `TileFit = 2`：Repeats the center texture across the NinePatchRect, but will also stretch the texture to make sure each tile is visible in full.
