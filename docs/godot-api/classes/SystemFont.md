<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SystemFont

命名空间 `Godot`；class；继承 [`Font`](Font.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`SystemFont` loads a font from a system font with the first matching name from `FontNames`.

It will attempt to match font style, but it's not guaranteed.

The returned font might be part of a font collection or be a variable font with OpenType "weight", "width" and/or "italic" features set.

You can create [`FontVariation`](FontVariation.md) of the system font for precise control over its features.

**Note:** This class is implemented on iOS, Linux, macOS and Windows, on other platforms it will fallback to default theme font.

## 构造函数

### SystemFont

```csharp
public SystemFont()
```

## 属性

### AllowSystemFallback

```csharp
public bool AllowSystemFallback { get; set; }
```

引擎名：`allow_system_fallback`

If set to `true`, system fonts can be automatically used as fallbacks.

### Antialiasing

```csharp
public TextServer.FontAntialiasing Antialiasing { get; set; }
```

引擎名：`antialiasing`

Font anti-aliasing mode.

### DisableEmbeddedBitmaps

```csharp
public bool DisableEmbeddedBitmaps { get; set; }
```

引擎名：`disable_embedded_bitmaps`

If set to `true`, embedded font bitmap loading is disabled (bitmap-only and color fonts ignore this property).

### FontItalic

```csharp
public bool FontItalic { get; set; }
```

引擎名：`font_italic`

If set to `true`, italic or oblique font is preferred.

### FontNames

```csharp
public string[] FontNames { get; set; }
```

引擎名：`font_names`

Array of font family names to search, first matching font found is used.

### FontStretch

```csharp
public int FontStretch { get; set; }
```

引擎名：`font_stretch`

Preferred font stretch amount, compared to a normal width. A percentage value between `50%` and `200%`.

### FontWeight

```csharp
public int FontWeight { get; set; }
```

引擎名：`font_weight`

Preferred weight (boldness) of the font. A value in the `100...999` range, normal font weight is `400`, bold font weight is `700`.

### ForceAutohinter

```csharp
public bool ForceAutohinter { get; set; }
```

引擎名：`force_autohinter`

If set to `true`, auto-hinting is supported and preferred over font built-in hinting.

### GenerateMipmaps

```csharp
public bool GenerateMipmaps { get; set; }
```

引擎名：`generate_mipmaps`

If set to `true`, generate mipmaps for the font textures.

### Hinting

```csharp
public TextServer.Hinting Hinting { get; set; }
```

引擎名：`hinting`

Font hinting mode.

### KeepRoundingRemainders

```csharp
public bool KeepRoundingRemainders { get; set; }
```

引擎名：`keep_rounding_remainders`

If set to `true`, when aligning glyphs to the pixel boundaries rounding remainders are accumulated to ensure more uniform glyph distribution. This setting has no effect if subpixel positioning is enabled.

### ModulateColorGlyphs

```csharp
public bool ModulateColorGlyphs { get; set; }
```

引擎名：`modulate_color_glyphs`

If set to `true`, color modulation is applied when drawing colored glyphs, otherwise it's applied to the monochrome glyphs only.

### MsdfPixelRange

```csharp
public int MsdfPixelRange { get; set; }
```

引擎名：`msdf_pixel_range`

The width of the range around the shape between the minimum and maximum representable signed distance. If using font outlines, `MsdfPixelRange` must be set to at least *twice* the size of the largest font outline. The default `MsdfPixelRange` value of `16` allows outline sizes up to `8` to look correct.

### MsdfSize

```csharp
public int MsdfSize { get; set; }
```

引擎名：`msdf_size`

Source font size used to generate MSDF textures. Higher values allow for more precision, but are slower to render and require more memory. Only increase this value if you notice a visible lack of precision in glyph rendering.

### MultichannelSignedDistanceField

```csharp
public bool MultichannelSignedDistanceField { get; set; }
```

引擎名：`multichannel_signed_distance_field`

If set to `true`, glyphs of all sizes are rendered using single multichannel signed distance field generated from the dynamic font vector data.

### Oversampling

```csharp
public float Oversampling { get; set; }
```

引擎名：`oversampling`

If set to a positive value, overrides the oversampling factor of the viewport this font is used in. See [`Viewport.Oversampling`](Viewport.md). This value doesn't override the `oversampling` parameter of `draw_*` methods.

### SubpixelPositioning

```csharp
public TextServer.SubpixelPositioning SubpixelPositioning { get; set; }
```

引擎名：`subpixel_positioning`

Font glyph subpixel positioning mode. Subpixel positioning provides shaper text and better kerning for smaller font sizes, at the cost of memory usage and font rasterization speed. Use [`TextServer.SubpixelPositioning.Auto`](TextServer.md) to automatically enable it based on the font size.

## 方法

### GetAntialiasing

```csharp
public TextServer.FontAntialiasing GetAntialiasing()
```

引擎名：`get_antialiasing`

### GetDisableEmbeddedBitmaps

```csharp
public bool GetDisableEmbeddedBitmaps()
```

引擎名：`get_disable_embedded_bitmaps`

### GetFontItalic

```csharp
public bool GetFontItalic()
```

引擎名：`get_font_italic`

### GetFontNames

```csharp
public string[] GetFontNames()
```

引擎名：`get_font_names`

### GetGenerateMipmaps

```csharp
public bool GetGenerateMipmaps()
```

引擎名：`get_generate_mipmaps`

### GetHinting

```csharp
public TextServer.Hinting GetHinting()
```

引擎名：`get_hinting`

### GetKeepRoundingRemainders

```csharp
public bool GetKeepRoundingRemainders()
```

引擎名：`get_keep_rounding_remainders`

### GetMsdfPixelRange

```csharp
public int GetMsdfPixelRange()
```

引擎名：`get_msdf_pixel_range`

### GetMsdfSize

```csharp
public int GetMsdfSize()
```

引擎名：`get_msdf_size`

### GetOversampling

```csharp
public float GetOversampling()
```

引擎名：`get_oversampling`

### GetSubpixelPositioning

```csharp
public TextServer.SubpixelPositioning GetSubpixelPositioning()
```

引擎名：`get_subpixel_positioning`

### IsAllowSystemFallback

```csharp
public bool IsAllowSystemFallback()
```

引擎名：`is_allow_system_fallback`

### IsForceAutohinter

```csharp
public bool IsForceAutohinter()
```

引擎名：`is_force_autohinter`

### IsModulateColorGlyphs

```csharp
public bool IsModulateColorGlyphs()
```

引擎名：`is_modulate_color_glyphs`

### IsMultichannelSignedDistanceField

```csharp
public bool IsMultichannelSignedDistanceField()
```

引擎名：`is_multichannel_signed_distance_field`

### SetAllowSystemFallback

```csharp
public void SetAllowSystemFallback(bool allowSystemFallback)
```

引擎名：`set_allow_system_fallback`

### SetAntialiasing

```csharp
public void SetAntialiasing(TextServer.FontAntialiasing antialiasing)
```

引擎名：`set_antialiasing`

### SetDisableEmbeddedBitmaps

```csharp
public void SetDisableEmbeddedBitmaps(bool disableEmbeddedBitmaps)
```

引擎名：`set_disable_embedded_bitmaps`

### SetFontItalic

```csharp
public void SetFontItalic(bool italic)
```

引擎名：`set_font_italic`

### SetFontNames

```csharp
public void SetFontNames(ReadOnlySpan<string> names)
```

引擎名：`set_font_names`

### SetFontNames

```csharp
public void SetFontNames(string[] names)
```

引擎名：`set_font_names`

### SetFontStretch

```csharp
public void SetFontStretch(int stretch)
```

引擎名：`set_font_stretch`

### SetFontWeight

```csharp
public void SetFontWeight(int weight)
```

引擎名：`set_font_weight`

### SetForceAutohinter

```csharp
public void SetForceAutohinter(bool forceAutohinter)
```

引擎名：`set_force_autohinter`

### SetGenerateMipmaps

```csharp
public void SetGenerateMipmaps(bool generateMipmaps)
```

引擎名：`set_generate_mipmaps`

### SetHinting

```csharp
public void SetHinting(TextServer.Hinting hinting)
```

引擎名：`set_hinting`

### SetKeepRoundingRemainders

```csharp
public void SetKeepRoundingRemainders(bool keepRoundingRemainders)
```

引擎名：`set_keep_rounding_remainders`

### SetModulateColorGlyphs

```csharp
public void SetModulateColorGlyphs(bool modulate)
```

引擎名：`set_modulate_color_glyphs`

### SetMsdfPixelRange

```csharp
public void SetMsdfPixelRange(int msdfPixelRange)
```

引擎名：`set_msdf_pixel_range`

### SetMsdfSize

```csharp
public void SetMsdfSize(int msdfSize)
```

引擎名：`set_msdf_size`

### SetMultichannelSignedDistanceField

```csharp
public void SetMultichannelSignedDistanceField(bool msdf)
```

引擎名：`set_multichannel_signed_distance_field`

### SetOversampling

```csharp
public void SetOversampling(float oversampling)
```

引擎名：`set_oversampling`

### SetSubpixelPositioning

```csharp
public void SetSubpixelPositioning(TextServer.SubpixelPositioning subpixelPositioning)
```

引擎名：`set_subpixel_positioning`
