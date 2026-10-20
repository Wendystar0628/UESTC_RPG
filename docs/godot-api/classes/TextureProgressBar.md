<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextureProgressBar

命名空间 `Godot`；class；继承 [`Range`](Range.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

TextureProgressBar works like [`ProgressBar`](ProgressBar.md), but uses up to 3 textures instead of Godot's [`Theme`](Theme.md) resource. It can be used to create horizontal, vertical and radial progress bars.

## 构造函数

### TextureProgressBar

```csharp
public TextureProgressBar()
```

## 属性

### FillMode

```csharp
public int FillMode { get; set; }
```

引擎名：`fill_mode`

The fill direction. See [`TextureProgressBar.FillModeEnum`](TextureProgressBar.md) for possible values.

### NinePatchStretch

```csharp
public bool NinePatchStretch { get; set; }
```

引擎名：`nine_patch_stretch`

If `true`, Godot treats the bar's textures like in [`NinePatchRect`](NinePatchRect.md). Use the `stretch_margin_*` properties like `StretchMarginBottom` to set up the nine patch's 3×3 grid. When using a radial `FillMode`, this setting will only enable stretching for `TextureProgress`, while `TextureUnder` and `TextureOver` will be treated like in [`NinePatchRect`](NinePatchRect.md).

### RadialCenterOffset

```csharp
public Vector2 RadialCenterOffset { get; set; }
```

引擎名：`radial_center_offset`

Offsets `TextureProgress` if `FillMode` is [`TextureProgressBar.FillModeEnum.Clockwise`](TextureProgressBar.md), [`TextureProgressBar.FillModeEnum.CounterClockwise`](TextureProgressBar.md), or [`TextureProgressBar.FillModeEnum.ClockwiseAndCounterClockwise`](TextureProgressBar.md).

**Note:** The effective radial center always stays within the `TextureProgress` bounds. If you need to move it outside the texture's bounds, modify the `TextureProgress` to contain additional empty space where needed.

### RadialFillDegrees

```csharp
public float RadialFillDegrees { get; set; }
```

引擎名：`radial_fill_degrees`

Upper limit for the fill of `TextureProgress` if `FillMode` is [`TextureProgressBar.FillModeEnum.Clockwise`](TextureProgressBar.md), [`TextureProgressBar.FillModeEnum.CounterClockwise`](TextureProgressBar.md), or [`TextureProgressBar.FillModeEnum.ClockwiseAndCounterClockwise`](TextureProgressBar.md). When the node's `value` is equal to its `max_value`, the texture fills up to this angle.

See [`Range.Value`](Range.md), [`Range.MaxValue`](Range.md).

### RadialInitialAngle

```csharp
public float RadialInitialAngle { get; set; }
```

引擎名：`radial_initial_angle`

Starting angle for the fill of `TextureProgress` if `FillMode` is [`TextureProgressBar.FillModeEnum.Clockwise`](TextureProgressBar.md), [`TextureProgressBar.FillModeEnum.CounterClockwise`](TextureProgressBar.md), or [`TextureProgressBar.FillModeEnum.ClockwiseAndCounterClockwise`](TextureProgressBar.md). When the node's `value` is equal to its `min_value`, the texture doesn't show up at all. When the `value` increases, the texture fills and tends towards `RadialFillDegrees`.

**Note:** `RadialInitialAngle` is wrapped between `0` and `360` degrees (inclusive).

### StretchMarginBottom

```csharp
public int StretchMarginBottom { get; set; }
```

引擎名：`stretch_margin_bottom`

The height of the 9-patch's bottom row. A margin of 16 means the 9-slice's bottom corners and side will have a height of 16 pixels. You can set all 4 margin values individually to create panels with non-uniform borders. Only effective if `NinePatchStretch` is `true`.

### StretchMarginLeft

```csharp
public int StretchMarginLeft { get; set; }
```

引擎名：`stretch_margin_left`

The width of the 9-patch's left column. Only effective if `NinePatchStretch` is `true`.

### StretchMarginRight

```csharp
public int StretchMarginRight { get; set; }
```

引擎名：`stretch_margin_right`

The width of the 9-patch's right column. Only effective if `NinePatchStretch` is `true`.

### StretchMarginTop

```csharp
public int StretchMarginTop { get; set; }
```

引擎名：`stretch_margin_top`

The height of the 9-patch's top row. Only effective if `NinePatchStretch` is `true`.

### TextureOver

```csharp
public Texture2D TextureOver { get; set; }
```

引擎名：`texture_over`

[`Texture2D`](Texture2D.md) that draws over the progress bar. Use it to add highlights or an upper-frame that hides part of `TextureProgress`.

### TextureProgress

```csharp
public Texture2D TextureProgress { get; set; }
```

引擎名：`texture_progress`

[`Texture2D`](Texture2D.md) that clips based on the node's `value` and `FillMode`. As `value` increased, the texture fills up. It shows entirely when `value` reaches `max_value`. It doesn't show at all if `value` is equal to `min_value`.

The `value` property comes from [`Range`](Range.md). See [`Range.Value`](Range.md), [`Range.MinValue`](Range.md), [`Range.MaxValue`](Range.md).

### TextureProgressOffset

```csharp
public Vector2 TextureProgressOffset { get; set; }
```

引擎名：`texture_progress_offset`

The offset of `TextureProgress`. Useful for `TextureOver` and `TextureUnder` with fancy borders, to avoid transparent margins in your progress texture.

### TextureUnder

```csharp
public Texture2D TextureUnder { get; set; }
```

引擎名：`texture_under`

[`Texture2D`](Texture2D.md) that draws under the progress bar. The bar's background.

### TintOver

```csharp
public Color TintOver { get; set; }
```

引擎名：`tint_over`

Multiplies the color of the bar's `TextureOver` texture. The effect is similar to [`CanvasItem.Modulate`](CanvasItem.md), except it only affects this specific texture instead of the entire node.

### TintProgress

```csharp
public Color TintProgress { get; set; }
```

引擎名：`tint_progress`

Multiplies the color of the bar's `TextureProgress` texture.

### TintUnder

```csharp
public Color TintUnder { get; set; }
```

引擎名：`tint_under`

Multiplies the color of the bar's `TextureUnder` texture.

## 方法

### GetFillDegrees

```csharp
public float GetFillDegrees()
```

引擎名：`get_fill_degrees`

### GetFillMode

```csharp
public int GetFillMode()
```

引擎名：`get_fill_mode`

### GetNinePatchStretch

```csharp
public bool GetNinePatchStretch()
```

引擎名：`get_nine_patch_stretch`

### GetOverTexture

```csharp
public Texture2D GetOverTexture()
```

引擎名：`get_over_texture`

### GetProgressTexture

```csharp
public Texture2D GetProgressTexture()
```

引擎名：`get_progress_texture`

### GetRadialCenterOffset

```csharp
public Vector2 GetRadialCenterOffset()
```

引擎名：`get_radial_center_offset`

### GetRadialInitialAngle

```csharp
public float GetRadialInitialAngle()
```

引擎名：`get_radial_initial_angle`

### GetStretchMargin

```csharp
public int GetStretchMargin(Side margin)
```

引擎名：`get_stretch_margin`

Returns the stretch margin with the specified index. See `StretchMarginBottom` and related properties.

### GetTextureProgressOffset

```csharp
public Vector2 GetTextureProgressOffset()
```

引擎名：`get_texture_progress_offset`

### GetTintOver

```csharp
public Color GetTintOver()
```

引擎名：`get_tint_over`

### GetTintProgress

```csharp
public Color GetTintProgress()
```

引擎名：`get_tint_progress`

### GetTintUnder

```csharp
public Color GetTintUnder()
```

引擎名：`get_tint_under`

### GetUnderTexture

```csharp
public Texture2D GetUnderTexture()
```

引擎名：`get_under_texture`

### SetFillDegrees

```csharp
public void SetFillDegrees(float mode)
```

引擎名：`set_fill_degrees`

### SetFillMode

```csharp
public void SetFillMode(int mode)
```

引擎名：`set_fill_mode`

### SetNinePatchStretch

```csharp
public void SetNinePatchStretch(bool stretch)
```

引擎名：`set_nine_patch_stretch`

### SetOverTexture

```csharp
public void SetOverTexture(Texture2D tex)
```

引擎名：`set_over_texture`

### SetProgressTexture

```csharp
public void SetProgressTexture(Texture2D tex)
```

引擎名：`set_progress_texture`

### SetRadialCenterOffset

```csharp
public void SetRadialCenterOffset(Vector2 mode)
```

引擎名：`set_radial_center_offset`

### SetRadialInitialAngle

```csharp
public void SetRadialInitialAngle(float mode)
```

引擎名：`set_radial_initial_angle`

### SetStretchMargin

```csharp
public void SetStretchMargin(Side margin, int value)
```

引擎名：`set_stretch_margin`

Sets the stretch margin with the specified index. See `StretchMarginBottom` and related properties.

### SetTextureProgressOffset

```csharp
public void SetTextureProgressOffset(Vector2 offset)
```

引擎名：`set_texture_progress_offset`

### SetTintOver

```csharp
public void SetTintOver(Color tint)
```

引擎名：`set_tint_over`

### SetTintProgress

```csharp
public void SetTintProgress(Color tint)
```

引擎名：`set_tint_progress`

### SetTintUnder

```csharp
public void SetTintUnder(Color tint)
```

引擎名：`set_tint_under`

### SetUnderTexture

```csharp
public void SetUnderTexture(Texture2D tex)
```

引擎名：`set_under_texture`

## FillModeEnum

命名空间 `Godot`；enum

### 枚举值

- `LeftToRight = 0`：The [`TextureProgressBar.TextureProgress`](TextureProgressBar.md) fills from left to right.
- `RightToLeft = 1`：The [`TextureProgressBar.TextureProgress`](TextureProgressBar.md) fills from right to left.
- `TopToBottom = 2`：The [`TextureProgressBar.TextureProgress`](TextureProgressBar.md) fills from top to bottom.
- `BottomToTop = 3`：The [`TextureProgressBar.TextureProgress`](TextureProgressBar.md) fills from bottom to top.
- `Clockwise = 4`：Turns the node into a radial bar.
- `CounterClockwise = 5`：Turns the node into a radial bar.
- `BilinearLeftAndRight = 6`：The [`TextureProgressBar.TextureProgress`](TextureProgressBar.md) fills from the center, expanding both towards the left and the right.
- `BilinearTopAndBottom = 7`：The [`TextureProgressBar.TextureProgress`](TextureProgressBar.md) fills from the center, expanding both towards the top and the bottom.
- `ClockwiseAndCounterClockwise = 8`：Turns the node into a radial bar.
