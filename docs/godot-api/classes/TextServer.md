<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextServer

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`TextServer` is the API backend for managing fonts and rendering text.

**Note:** This is a low-level API, consider using [`TextLine`](TextLine.md), [`TextParagraph`](TextParagraph.md), and [`Font`](Font.md) classes instead.

This is an abstract class, so to get the currently active `TextServer` instance, use the following code:

```csharp
var ts = TextServerManager.GetPrimaryInterface();

```

## 方法

### CreateFont

```csharp
public Rid CreateFont()
```

引擎名：`create_font`

Creates a new, empty font cache entry resource. To free the resulting resource, use the `FreeRid()` method.

### CreateFontLinkedVariation

```csharp
public Rid CreateFontLinkedVariation(Rid fontRid)
```

引擎名：`create_font_linked_variation`

Creates a new variation existing font which is reusing the same glyph cache and font data. To free the resulting resource, use the `FreeRid()` method.

### CreateShapedText

```csharp
public Rid CreateShapedText(TextServer.Direction direction = TextServer.Direction.Auto, TextServer.Orientation orientation = TextServer.Orientation.Horizontal)
```

引擎名：`create_shaped_text`

Creates a new buffer for complex text layout, with the given `direction` and `orientation`. To free the resulting buffer, use `FreeRid()` method.

**Note:** Direction is ignored if server does not support [`TextServer.Feature.BidiLayout`](TextServer.md) feature (supported by [`TextServerAdvanced`](TextServerAdvanced.md)).

**Note:** Orientation is ignored if server does not support [`TextServer.Feature.VerticalLayout`](TextServer.md) feature (supported by [`TextServerAdvanced`](TextServerAdvanced.md)).

### DrawHexCodeBox

```csharp
public void DrawHexCodeBox(Rid canvas, long size, Vector2 pos, long index, Color color)
```

引擎名：`draw_hex_code_box`

Draws box displaying character hexadecimal code. Used for replacing missing characters.

### FontClearGlyphs

```csharp
public void FontClearGlyphs(Rid fontRid, Vector2I size)
```

引擎名：`font_clear_glyphs`

Removes all rendered glyph information from the cache entry.

**Note:** This function will not remove textures associated with the glyphs, use `FontRemoveTexture()` to remove them manually.

### FontClearKerningMap

```csharp
public void FontClearKerningMap(Rid fontRid, long size)
```

引擎名：`font_clear_kerning_map`

Removes all kerning overrides.

### FontClearSizeCache

```csharp
public void FontClearSizeCache(Rid fontRid)
```

引擎名：`font_clear_size_cache`

Removes all font sizes from the cache entry.

### FontClearSystemFallbackCache

```csharp
public void FontClearSystemFallbackCache()
```

引擎名：`font_clear_system_fallback_cache`

Frees all automatically loaded system fonts.

### FontClearTextures

```csharp
public void FontClearTextures(Rid fontRid, Vector2I size)
```

引擎名：`font_clear_textures`

Removes all textures from font cache entry.

**Note:** This function will not remove glyphs associated with the texture, use `FontRemoveGlyph()` to remove them manually.

### FontDrawGlyph

```csharp
public void FontDrawGlyph(Rid fontRid, Rid canvas, long size, Vector2 pos, long index, Color? color = null, float oversampling = 0f)
```

引擎名：`font_draw_glyph`

Draws single glyph into a canvas item at the position, using `fontRid` at the size `size`. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

**Note:** Glyph index is specific to the font, use glyphs indices returned by `ShapedTextGetGlyphs()` or `FontGetGlyphIndex()`.

**Note:** If there are pending glyphs to render, calling this function might trigger the texture cache update.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### FontDrawGlyph

```csharp
public void FontDrawGlyph(Rid fontRid, Rid canvas, long size, Vector2 pos, long index, Color? color)
```

引擎名：`font_draw_glyph`

Draws single glyph into a canvas item at the position, using `fontRid` at the size `size`. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

**Note:** Glyph index is specific to the font, use glyphs indices returned by `ShapedTextGetGlyphs()` or `FontGetGlyphIndex()`.

**Note:** If there are pending glyphs to render, calling this function might trigger the texture cache update.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### FontDrawGlyphOutline

```csharp
public void FontDrawGlyphOutline(Rid fontRid, Rid canvas, long size, long outlineSize, Vector2 pos, long index, Color? color = null, float oversampling = 0f)
```

引擎名：`font_draw_glyph_outline`

Draws single glyph outline of size `outlineSize` into a canvas item at the position, using `fontRid` at the size `size`. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

**Note:** Glyph index is specific to the font, use glyphs indices returned by `ShapedTextGetGlyphs()` or `FontGetGlyphIndex()`.

**Note:** If there are pending glyphs to render, calling this function might trigger the texture cache update.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### FontDrawGlyphOutline

```csharp
public void FontDrawGlyphOutline(Rid fontRid, Rid canvas, long size, long outlineSize, Vector2 pos, long index, Color? color)
```

引擎名：`font_draw_glyph_outline`

Draws single glyph outline of size `outlineSize` into a canvas item at the position, using `fontRid` at the size `size`. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

**Note:** Glyph index is specific to the font, use glyphs indices returned by `ShapedTextGetGlyphs()` or `FontGetGlyphIndex()`.

**Note:** If there are pending glyphs to render, calling this function might trigger the texture cache update.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### FontGetAntialiasing

```csharp
public TextServer.FontAntialiasing FontGetAntialiasing(Rid fontRid)
```

引擎名：`font_get_antialiasing`

Returns font anti-aliasing mode.

### FontGetAscent

```csharp
public double FontGetAscent(Rid fontRid, long size)
```

引擎名：`font_get_ascent`

Returns the font ascent (number of pixels above the baseline).

### FontGetBaselineOffset

```csharp
public double FontGetBaselineOffset(Rid fontRid)
```

引擎名：`font_get_baseline_offset`

Returns extra baseline offset (as a fraction of font height).

### FontGetCharFromGlyphIndex

```csharp
public long FontGetCharFromGlyphIndex(Rid fontRid, long size, long glyphIndex)
```

引擎名：`font_get_char_from_glyph_index`

Returns character code associated with `glyphIndex`, or `0` if `glyphIndex` is invalid. See `FontGetGlyphIndex()`.

### FontGetDescent

```csharp
public double FontGetDescent(Rid fontRid, long size)
```

引擎名：`font_get_descent`

Returns the font descent (number of pixels below the baseline).

### FontGetDisableEmbeddedBitmaps

```csharp
public bool FontGetDisableEmbeddedBitmaps(Rid fontRid)
```

引擎名：`font_get_disable_embedded_bitmaps`

Returns whether the font's embedded bitmap loading is disabled.

### FontGetEmbolden

```csharp
public double FontGetEmbolden(Rid fontRid)
```

引擎名：`font_get_embolden`

Returns font embolden strength.

### FontGetFaceCount

```csharp
public long FontGetFaceCount(Rid fontRid)
```

引擎名：`font_get_face_count`

Returns number of faces in the TrueType / OpenType collection.

### FontGetFaceIndex

```csharp
public long FontGetFaceIndex(Rid fontRid)
```

引擎名：`font_get_face_index`

Returns an active face index in the TrueType / OpenType collection.

### FontGetFixedSize

```csharp
public long FontGetFixedSize(Rid fontRid)
```

引擎名：`font_get_fixed_size`

Returns bitmap font fixed size.

### FontGetFixedSizeScaleMode

```csharp
public TextServer.FixedSizeScaleMode FontGetFixedSizeScaleMode(Rid fontRid)
```

引擎名：`font_get_fixed_size_scale_mode`

Returns bitmap font scaling mode.

### FontGetGenerateMipmaps

```csharp
public bool FontGetGenerateMipmaps(Rid fontRid)
```

引擎名：`font_get_generate_mipmaps`

Returns `true` if font texture mipmap generation is enabled.

### FontGetGlobalOversampling

```csharp
public double FontGetGlobalOversampling()
```

引擎名：`font_get_global_oversampling`

This method does nothing and always returns `1.0`.

### FontGetGlyphAdvance

```csharp
public Vector2 FontGetGlyphAdvance(Rid fontRid, long size, long glyph)
```

引擎名：`font_get_glyph_advance`

Returns glyph advance (offset of the next glyph).

**Note:** Advance for glyphs outlines is the same as the base glyph advance and is not saved.

### FontGetGlyphContours

```csharp
public Dictionary FontGetGlyphContours(Rid font, long size, long index)
```

引擎名：`font_get_glyph_contours`

Returns outline contours of the glyph as a [`Collections.Dictionary`](Collections.Dictionary.md) with the following contents:

`points`         - [`Vector3`](Vector3.md)[], containing outline points. `x` and `y` are point coordinates. `z` is the type of the point, using the [`TextServer.ContourPointTag`](TextServer.md) values.

`contours`       - `System.Int32`[], containing indices the end points of each contour.

`orientation`    - `System.Boolean`, contour orientation. If `true`, clockwise contours must be filled.

- Two successive [`TextServer.ContourPointTag.On`](TextServer.md) points indicate a line segment.

- One [`TextServer.ContourPointTag.OffConic`](TextServer.md) point between two [`TextServer.ContourPointTag.On`](TextServer.md) points indicates a single conic (quadratic) Bézier arc.

- Two [`TextServer.ContourPointTag.OffCubic`](TextServer.md) points between two [`TextServer.ContourPointTag.On`](TextServer.md) points indicate a single cubic Bézier arc.

- Two successive [`TextServer.ContourPointTag.OffConic`](TextServer.md) points indicate two successive conic (quadratic) Bézier arcs with a virtual [`TextServer.ContourPointTag.On`](TextServer.md) point at their middle.

- Each contour is closed. The last point of a contour uses the first point of a contour as its next point, and vice versa. The first point can be [`TextServer.ContourPointTag.OffConic`](TextServer.md) point.

### FontGetGlyphIndex

```csharp
public long FontGetGlyphIndex(Rid fontRid, long size, long char, long variationSelector)
```

引擎名：`font_get_glyph_index`

Returns the glyph index of a `char`, optionally modified by the `variationSelector`. See `FontGetCharFromGlyphIndex()`.

### FontGetGlyphList

```csharp
public int[] FontGetGlyphList(Rid fontRid, Vector2I size)
```

引擎名：`font_get_glyph_list`

Returns list of rendered glyphs in the cache entry.

### FontGetGlyphOffset

```csharp
public Vector2 FontGetGlyphOffset(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`font_get_glyph_offset`

Returns glyph offset from the baseline.

### FontGetGlyphSize

```csharp
public Vector2 FontGetGlyphSize(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`font_get_glyph_size`

Returns size of the glyph.

### FontGetGlyphTextureIdx

```csharp
public long FontGetGlyphTextureIdx(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`font_get_glyph_texture_idx`

Returns index of the cache texture containing the glyph.

### FontGetGlyphTextureRid

```csharp
public Rid FontGetGlyphTextureRid(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`font_get_glyph_texture_rid`

Returns resource ID of the cache texture containing the glyph.

**Note:** If there are pending glyphs to render, calling this function might trigger the texture cache update.

### FontGetGlyphTextureSize

```csharp
public Vector2 FontGetGlyphTextureSize(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`font_get_glyph_texture_size`

Returns size of the cache texture containing the glyph.

**Note:** If there are pending glyphs to render, calling this function might trigger the texture cache update.

### FontGetGlyphUVRect

```csharp
public Rect2 FontGetGlyphUVRect(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`font_get_glyph_uv_rect`

Returns rectangle in the cache texture containing the glyph.

### FontGetHinting

```csharp
public TextServer.Hinting FontGetHinting(Rid fontRid)
```

引擎名：`font_get_hinting`

Returns the font hinting mode. Used by dynamic fonts only.

### FontGetKeepRoundingRemainders

```csharp
public bool FontGetKeepRoundingRemainders(Rid fontRid)
```

引擎名：`font_get_keep_rounding_remainders`

Returns glyph position rounding behavior. If set to `true`, when aligning glyphs to the pixel boundaries rounding remainders are accumulated to ensure more uniform glyph distribution. This setting has no effect if subpixel positioning is enabled.

### FontGetKerning

```csharp
public Vector2 FontGetKerning(Rid fontRid, long size, Vector2I glyphPair)
```

引擎名：`font_get_kerning`

Returns kerning for the pair of glyphs.

### FontGetKerningList

```csharp
public Array<Vector2I> FontGetKerningList(Rid fontRid, long size)
```

引擎名：`font_get_kerning_list`

Returns list of the kerning overrides.

### FontGetLanguageSupportOverride

```csharp
public bool FontGetLanguageSupportOverride(Rid fontRid, string language)
```

引擎名：`font_get_language_support_override`

Returns `true` if support override is enabled for the `language`.

### FontGetLanguageSupportOverrides

```csharp
public string[] FontGetLanguageSupportOverrides(Rid fontRid)
```

引擎名：`font_get_language_support_overrides`

Returns list of language support overrides.

### FontGetMsdfPixelRange

```csharp
public long FontGetMsdfPixelRange(Rid fontRid)
```

引擎名：`font_get_msdf_pixel_range`

Returns the width of the range around the shape between the minimum and maximum representable signed distance.

### FontGetMsdfSize

```csharp
public long FontGetMsdfSize(Rid fontRid)
```

引擎名：`font_get_msdf_size`

Returns source font size used to generate MSDF textures.

### FontGetName

```csharp
public string FontGetName(Rid fontRid)
```

引擎名：`font_get_name`

Returns font family name.

### FontGetOpentypeFeatureOverrides

```csharp
public Dictionary FontGetOpentypeFeatureOverrides(Rid fontRid)
```

引擎名：`font_get_opentype_feature_overrides`

Returns font OpenType feature set override.

### FontGetOtNameStrings

```csharp
public Dictionary FontGetOtNameStrings(Rid fontRid)
```

引擎名：`font_get_ot_name_strings`

Returns [`Collections.Dictionary`](Collections.Dictionary.md) with OpenType font name strings (localized font names, version, description, license information, sample text, etc.).

### FontGetOversampling

```csharp
public double FontGetOversampling(Rid fontRid)
```

引擎名：`font_get_oversampling`

Returns oversampling factor override. If set to a positive value, overrides the oversampling factor of the viewport this font is used in. See [`Viewport.Oversampling`](Viewport.md). This value doesn't override the `oversampling` parameter of `draw_*` methods. Used by dynamic fonts only.

### FontGetPaletteColors

```csharp
public Color[] FontGetPaletteColors(Rid fontRid, long index)
```

引擎名：`font_get_palette_colors`

Returns the array in the predefined color palette at `index`. Palette contains all colors used to render font glyphs. Each palette has the same number of colors. Colors can be overridden using `FontSetPaletteCustomColors()`.

### FontGetPaletteCount

```csharp
public long FontGetPaletteCount(Rid fontRid)
```

引擎名：`font_get_palette_count`

Returns the number of predefined color palettes. Palette contains all colors used to render font glyphs. Each palette has the same number of colors.

### FontGetPaletteCustomColors

```csharp
public Color[] FontGetPaletteCustomColors(Rid fontRid)
```

引擎名：`font_get_palette_custom_colors`

Returns array of custom colors to override predefined palette.

### FontGetPaletteName

```csharp
public string FontGetPaletteName(Rid fontRid, long index)
```

引擎名：`font_get_palette_name`

Returns the name of the predefined color palette at `index`. Palette contains all colors used to render font glyphs. Each palette has the same number of colors.

### FontGetScale

```csharp
public double FontGetScale(Rid fontRid, long size)
```

引擎名：`font_get_scale`

Returns scaling factor of the color bitmap font.

### FontGetScriptSupportOverride

```csharp
public bool FontGetScriptSupportOverride(Rid fontRid, string script)
```

引擎名：`font_get_script_support_override`

Returns `true` if support override is enabled for the `script`.

### FontGetScriptSupportOverrides

```csharp
public string[] FontGetScriptSupportOverrides(Rid fontRid)
```

引擎名：`font_get_script_support_overrides`

Returns list of script support overrides.

### FontGetSizeCacheInfo

```csharp
public Array<Dictionary> FontGetSizeCacheInfo(Rid fontRid)
```

引擎名：`font_get_size_cache_info`

Returns font cache information, each entry contains the following fields: `Vector2i size_px` - font size in pixels, `float viewport_oversampling` - viewport oversampling factor, `int glyphs` - number of rendered glyphs, `int textures` - number of used textures, `int textures_size` - size of texture data in bytes.

### FontGetSizeCacheList

```csharp
public Array<Vector2I> FontGetSizeCacheList(Rid fontRid)
```

引擎名：`font_get_size_cache_list`

Returns list of the font sizes in the cache. Each size is [`Vector2I`](Vector2I.md) with font size and outline size.

### FontGetSpacing

```csharp
public long FontGetSpacing(Rid fontRid, TextServer.SpacingType spacing)
```

引擎名：`font_get_spacing`

Returns the spacing for `spacing` in pixels (not relative to the font size).

### FontGetStretch

```csharp
public long FontGetStretch(Rid fontRid)
```

引擎名：`font_get_stretch`

Returns font stretch amount, compared to a normal width. A percentage value between `50%` and `200%`.

### FontGetStyle

```csharp
public TextServer.FontStyle FontGetStyle(Rid fontRid)
```

引擎名：`font_get_style`

Returns font style flags.

### FontGetStyleName

```csharp
public string FontGetStyleName(Rid fontRid)
```

引擎名：`font_get_style_name`

Returns font style name.

### FontGetSubpixelPositioning

```csharp
public TextServer.SubpixelPositioning FontGetSubpixelPositioning(Rid fontRid)
```

引擎名：`font_get_subpixel_positioning`

Returns font subpixel glyph positioning mode.

### FontGetSupportedChars

```csharp
public string FontGetSupportedChars(Rid fontRid)
```

引擎名：`font_get_supported_chars`

Returns a string containing all the characters available in the font.

### FontGetSupportedGlyphs

```csharp
public int[] FontGetSupportedGlyphs(Rid fontRid)
```

引擎名：`font_get_supported_glyphs`

Returns an array containing all glyph indices in the font.

### FontGetTextureCount

```csharp
public long FontGetTextureCount(Rid fontRid, Vector2I size)
```

引擎名：`font_get_texture_count`

Returns number of textures used by font cache entry.

### FontGetTextureImage

```csharp
public Image FontGetTextureImage(Rid fontRid, Vector2I size, long textureIndex)
```

引擎名：`font_get_texture_image`

Returns font cache texture image data.

### FontGetTextureOffsets

```csharp
public int[] FontGetTextureOffsets(Rid fontRid, Vector2I size, long textureIndex)
```

引擎名：`font_get_texture_offsets`

Returns array containing glyph packing data.

### FontGetTransform

```csharp
public Transform2D FontGetTransform(Rid fontRid)
```

引擎名：`font_get_transform`

Returns 2D transform applied to the font outlines.

### FontGetUnderlinePosition

```csharp
public double FontGetUnderlinePosition(Rid fontRid, long size)
```

引擎名：`font_get_underline_position`

Returns pixel offset of the underline below the baseline.

### FontGetUnderlineThickness

```csharp
public double FontGetUnderlineThickness(Rid fontRid, long size)
```

引擎名：`font_get_underline_thickness`

Returns thickness of the underline in pixels.

### FontGetUsedPalette

```csharp
public long FontGetUsedPalette(Rid fontRid)
```

引擎名：`font_get_used_palette`

Returns used palette index.

### FontGetVariationCoordinates

```csharp
public Dictionary FontGetVariationCoordinates(Rid fontRid)
```

引擎名：`font_get_variation_coordinates`

Returns variation coordinates for the specified font cache entry. See `FontSupportedVariationList()` for more info.

### FontGetWeight

```csharp
public long FontGetWeight(Rid fontRid)
```

引擎名：`font_get_weight`

Returns weight (boldness) of the font. A value in the `100...999` range, normal font weight is `400`, bold font weight is `700`.

### FontHasChar

```csharp
public bool FontHasChar(Rid fontRid, long char)
```

引擎名：`font_has_char`

Returns `true` if a Unicode `char` is available in the font.

### FontIsAllowSystemFallback

```csharp
public bool FontIsAllowSystemFallback(Rid fontRid)
```

引擎名：`font_is_allow_system_fallback`

Returns `true` if system fonts can be automatically used as fallbacks.

### FontIsForceAutohinter

```csharp
public bool FontIsForceAutohinter(Rid fontRid)
```

引擎名：`font_is_force_autohinter`

Returns `true` if auto-hinting is supported and preferred over font built-in hinting. Used by dynamic fonts only.

### FontIsLanguageSupported

```csharp
public bool FontIsLanguageSupported(Rid fontRid, string language)
```

引擎名：`font_is_language_supported`

Returns `true` if the font supports the given language (as a [ISO 639](https://en.wikipedia.org/wiki/ISO_639-1) code).

### FontIsModulateColorGlyphs

```csharp
public bool FontIsModulateColorGlyphs(Rid fontRid)
```

引擎名：`font_is_modulate_color_glyphs`

Returns `true` if color modulation is applied when drawing the font's colored glyphs.

### FontIsMultichannelSignedDistanceField

```csharp
public bool FontIsMultichannelSignedDistanceField(Rid fontRid)
```

引擎名：`font_is_multichannel_signed_distance_field`

Returns `true` if glyphs of all sizes are rendered using single multichannel signed distance field generated from the dynamic font vector data.

### FontIsScriptSupported

```csharp
public bool FontIsScriptSupported(Rid fontRid, string script)
```

引擎名：`font_is_script_supported`

Returns `true` if the font supports the given script (as a [ISO 15924](https://en.wikipedia.org/wiki/ISO_15924) code).

### FontRemoveGlyph

```csharp
public void FontRemoveGlyph(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`font_remove_glyph`

Removes specified rendered glyph information from the cache entry.

**Note:** This function will not remove textures associated with the glyphs, use `FontRemoveTexture()` to remove them manually.

### FontRemoveKerning

```csharp
public void FontRemoveKerning(Rid fontRid, long size, Vector2I glyphPair)
```

引擎名：`font_remove_kerning`

Removes kerning override for the pair of glyphs.

### FontRemoveLanguageSupportOverride

```csharp
public void FontRemoveLanguageSupportOverride(Rid fontRid, string language)
```

引擎名：`font_remove_language_support_override`

Remove language support override.

### FontRemoveScriptSupportOverride

```csharp
public void FontRemoveScriptSupportOverride(Rid fontRid, string script)
```

引擎名：`font_remove_script_support_override`

Removes script support override.

### FontRemoveSizeCache

```csharp
public void FontRemoveSizeCache(Rid fontRid, Vector2I size)
```

引擎名：`font_remove_size_cache`

Removes specified font size from the cache entry.

### FontRemoveTexture

```csharp
public void FontRemoveTexture(Rid fontRid, Vector2I size, long textureIndex)
```

引擎名：`font_remove_texture`

Removes specified texture from the cache entry.

**Note:** This function will not remove glyphs associated with the texture, remove them manually, using `FontRemoveGlyph()`.

### FontRenderGlyph

```csharp
public void FontRenderGlyph(Rid fontRid, Vector2I size, long index)
```

引擎名：`font_render_glyph`

Renders specified glyph to the font cache texture.

### FontRenderRange

```csharp
public void FontRenderRange(Rid fontRid, Vector2I size, long start, long end)
```

引擎名：`font_render_range`

Renders the range of characters to the font cache texture.

### FontSetAllowSystemFallback

```csharp
public void FontSetAllowSystemFallback(Rid fontRid, bool allowSystemFallback)
```

引擎名：`font_set_allow_system_fallback`

If set to `true`, system fonts can be automatically used as fallbacks.

### FontSetAntialiasing

```csharp
public void FontSetAntialiasing(Rid fontRid, TextServer.FontAntialiasing antialiasing)
```

引擎名：`font_set_antialiasing`

Sets font anti-aliasing mode.

### FontSetAscent

```csharp
public void FontSetAscent(Rid fontRid, long size, double ascent)
```

引擎名：`font_set_ascent`

Sets the font ascent (number of pixels above the baseline).

### FontSetBaselineOffset

```csharp
public void FontSetBaselineOffset(Rid fontRid, double baselineOffset)
```

引擎名：`font_set_baseline_offset`

Sets extra baseline offset (as a fraction of font height).

### FontSetData

```csharp
public void FontSetData(Rid fontRid, ReadOnlySpan<byte> data)
```

引擎名：`font_set_data`

Sets font source data, e.g contents of the dynamic font source file.

### FontSetData

```csharp
public void FontSetData(Rid fontRid, byte[] data)
```

引擎名：`font_set_data`

Sets font source data, e.g contents of the dynamic font source file.

### FontSetDescent

```csharp
public void FontSetDescent(Rid fontRid, long size, double descent)
```

引擎名：`font_set_descent`

Sets the font descent (number of pixels below the baseline).

### FontSetDisableEmbeddedBitmaps

```csharp
public void FontSetDisableEmbeddedBitmaps(Rid fontRid, bool disableEmbeddedBitmaps)
```

引擎名：`font_set_disable_embedded_bitmaps`

If set to `true`, embedded font bitmap loading is disabled (bitmap-only and color fonts ignore this property).

### FontSetEmbolden

```csharp
public void FontSetEmbolden(Rid fontRid, double strength)
```

引擎名：`font_set_embolden`

Sets font embolden strength. If `strength` is not equal to zero, emboldens the font outlines. Negative values reduce the outline thickness.

### FontSetFaceIndex

```csharp
public void FontSetFaceIndex(Rid fontRid, long faceIndex)
```

引擎名：`font_set_face_index`

Sets an active face index in the TrueType / OpenType collection.

### FontSetFixedSize

```csharp
public void FontSetFixedSize(Rid fontRid, long fixedSize)
```

引擎名：`font_set_fixed_size`

Sets bitmap font fixed size. If set to value greater than zero, same cache entry will be used for all font sizes.

### FontSetFixedSizeScaleMode

```csharp
public void FontSetFixedSizeScaleMode(Rid fontRid, TextServer.FixedSizeScaleMode fixedSizeScaleMode)
```

引擎名：`font_set_fixed_size_scale_mode`

Sets bitmap font scaling mode. This property is used only if `fixed_size` is greater than zero.

### FontSetForceAutohinter

```csharp
public void FontSetForceAutohinter(Rid fontRid, bool forceAutohinter)
```

引擎名：`font_set_force_autohinter`

If set to `true` auto-hinting is preferred over font built-in hinting.

### FontSetGenerateMipmaps

```csharp
public void FontSetGenerateMipmaps(Rid fontRid, bool generateMipmaps)
```

引擎名：`font_set_generate_mipmaps`

If set to `true` font texture mipmap generation is enabled.

### FontSetGlobalOversampling

```csharp
public void FontSetGlobalOversampling(double oversampling)
```

引擎名：`font_set_global_oversampling`

This method does nothing.

### FontSetGlyphAdvance

```csharp
public void FontSetGlyphAdvance(Rid fontRid, long size, long glyph, Vector2 advance)
```

引擎名：`font_set_glyph_advance`

Sets glyph advance (offset of the next glyph).

**Note:** Advance for glyphs outlines is the same as the base glyph advance and is not saved.

### FontSetGlyphOffset

```csharp
public void FontSetGlyphOffset(Rid fontRid, Vector2I size, long glyph, Vector2 offset)
```

引擎名：`font_set_glyph_offset`

Sets glyph offset from the baseline.

### FontSetGlyphSize

```csharp
public void FontSetGlyphSize(Rid fontRid, Vector2I size, long glyph, Vector2 glSize)
```

引擎名：`font_set_glyph_size`

Sets size of the glyph.

### FontSetGlyphTextureIdx

```csharp
public void FontSetGlyphTextureIdx(Rid fontRid, Vector2I size, long glyph, long textureIdx)
```

引擎名：`font_set_glyph_texture_idx`

Sets index of the cache texture containing the glyph.

### FontSetGlyphUVRect

```csharp
public void FontSetGlyphUVRect(Rid fontRid, Vector2I size, long glyph, Rect2 uVRect)
```

引擎名：`font_set_glyph_uv_rect`

Sets rectangle in the cache texture containing the glyph.

### FontSetHinting

```csharp
public void FontSetHinting(Rid fontRid, TextServer.Hinting hinting)
```

引擎名：`font_set_hinting`

Sets font hinting mode. Used by dynamic fonts only.

### FontSetKeepRoundingRemainders

```csharp
public void FontSetKeepRoundingRemainders(Rid fontRid, bool keepRoundingRemainders)
```

引擎名：`font_set_keep_rounding_remainders`

Sets glyph position rounding behavior. If set to `true`, when aligning glyphs to the pixel boundaries rounding remainders are accumulated to ensure more uniform glyph distribution. This setting has no effect if subpixel positioning is enabled.

### FontSetKerning

```csharp
public void FontSetKerning(Rid fontRid, long size, Vector2I glyphPair, Vector2 kerning)
```

引擎名：`font_set_kerning`

Sets kerning for the pair of glyphs.

### FontSetLanguageSupportOverride

```csharp
public void FontSetLanguageSupportOverride(Rid fontRid, string language, bool supported)
```

引擎名：`font_set_language_support_override`

Adds override for `FontIsLanguageSupported()`.

### FontSetModulateColorGlyphs

```csharp
public void FontSetModulateColorGlyphs(Rid fontRid, bool modulate)
```

引擎名：`font_set_modulate_color_glyphs`

If set to `true`, color modulation is applied when drawing colored glyphs, otherwise it's applied to the monochrome glyphs only.

### FontSetMsdfPixelRange

```csharp
public void FontSetMsdfPixelRange(Rid fontRid, long msdfPixelRange)
```

引擎名：`font_set_msdf_pixel_range`

Sets the width of the range around the shape between the minimum and maximum representable signed distance.

### FontSetMsdfSize

```csharp
public void FontSetMsdfSize(Rid fontRid, long msdfSize)
```

引擎名：`font_set_msdf_size`

Sets source font size used to generate MSDF textures.

### FontSetMultichannelSignedDistanceField

```csharp
public void FontSetMultichannelSignedDistanceField(Rid fontRid, bool msdf)
```

引擎名：`font_set_multichannel_signed_distance_field`

If set to `true`, glyphs of all sizes are rendered using single multichannel signed distance field generated from the dynamic font vector data. MSDF rendering allows displaying the font at any scaling factor without blurriness, and without incurring a CPU cost when the font size changes (since the font no longer needs to be rasterized on the CPU). As a downside, font hinting is not available with MSDF. The lack of font hinting may result in less crisp and less readable fonts at small sizes.

**Note:** MSDF font rendering does not render glyphs with overlapping shapes correctly. Overlapping shapes are not valid per the OpenType standard, but are still commonly found in many font files, especially those converted by Google Fonts. To avoid issues with overlapping glyphs, consider downloading the font file directly from the type foundry instead of relying on Google Fonts.

### FontSetName

```csharp
public void FontSetName(Rid fontRid, string name)
```

引擎名：`font_set_name`

Sets the font family name.

### FontSetOpentypeFeatureOverrides

```csharp
public void FontSetOpentypeFeatureOverrides(Rid fontRid, Dictionary overrides)
```

引擎名：`font_set_opentype_feature_overrides`

Sets font OpenType feature set override.

### FontSetOversampling

```csharp
public void FontSetOversampling(Rid fontRid, double oversampling)
```

引擎名：`font_set_oversampling`

If set to a positive value, overrides the oversampling factor of the viewport this font is used in. See [`Viewport.Oversampling`](Viewport.md). This value doesn't override the `oversampling` parameter of `draw_*` methods. Used by dynamic fonts only.

### FontSetPaletteCustomColors

```csharp
public void FontSetPaletteCustomColors(Rid fontRid, Color[] colors)
```

引擎名：`font_set_palette_custom_colors`

Sets array of custom colors to override predefined palette. Set to empty array to reset overrides. Use `Color(0, 0, 0, 0)`, to keep predefined palette color at specific position.

### FontSetPaletteCustomColors

```csharp
public void FontSetPaletteCustomColors(Rid fontRid, ReadOnlySpan<Color> colors)
```

引擎名：`font_set_palette_custom_colors`

Sets array of custom colors to override predefined palette. Set to empty array to reset overrides. Use `Color(0, 0, 0, 0)`, to keep predefined palette color at specific position.

### FontSetScale

```csharp
public void FontSetScale(Rid fontRid, long size, double scale)
```

引擎名：`font_set_scale`

Sets scaling factor of the color bitmap font.

### FontSetScriptSupportOverride

```csharp
public void FontSetScriptSupportOverride(Rid fontRid, string script, bool supported)
```

引擎名：`font_set_script_support_override`

Adds override for `FontIsScriptSupported()`.

### FontSetSpacing

```csharp
public void FontSetSpacing(Rid fontRid, TextServer.SpacingType spacing, long value)
```

引擎名：`font_set_spacing`

Sets the spacing for `spacing` to `value` in pixels (not relative to the font size).

### FontSetStretch

```csharp
public void FontSetStretch(Rid fontRid, long weight)
```

引擎名：`font_set_stretch`

Sets font stretch amount, compared to a normal width. A percentage value between `50%` and `200%`.

**Note:** This value is used for font matching only and will not affect font rendering. Use `FontSetFaceIndex()`, `FontSetVariationCoordinates()`, or `FontSetTransform()` instead.

### FontSetStyle

```csharp
public void FontSetStyle(Rid fontRid, TextServer.FontStyle style)
```

引擎名：`font_set_style`

Sets the font style flags.

**Note:** This value is used for font matching only and will not affect font rendering. Use `FontSetFaceIndex()`, `FontSetVariationCoordinates()`, `FontSetEmbolden()`, or `FontSetTransform()` instead.

### FontSetStyleName

```csharp
public void FontSetStyleName(Rid fontRid, string name)
```

引擎名：`font_set_style_name`

Sets the font style name.

### FontSetSubpixelPositioning

```csharp
public void FontSetSubpixelPositioning(Rid fontRid, TextServer.SubpixelPositioning subpixelPositioning)
```

引擎名：`font_set_subpixel_positioning`

Sets font subpixel glyph positioning mode.

### FontSetTextureImage

```csharp
public void FontSetTextureImage(Rid fontRid, Vector2I size, long textureIndex, Image image)
```

引擎名：`font_set_texture_image`

Sets font cache texture image data.

### FontSetTextureOffsets

```csharp
public void FontSetTextureOffsets(Rid fontRid, Vector2I size, long textureIndex, ReadOnlySpan<int> offset)
```

引擎名：`font_set_texture_offsets`

Sets array containing glyph packing data.

### FontSetTextureOffsets

```csharp
public void FontSetTextureOffsets(Rid fontRid, Vector2I size, long textureIndex, int[] offset)
```

引擎名：`font_set_texture_offsets`

Sets array containing glyph packing data.

### FontSetTransform

```csharp
public void FontSetTransform(Rid fontRid, Transform2D transform)
```

引擎名：`font_set_transform`

Sets 2D transform, applied to the font outlines, can be used for slanting, flipping, and rotating glyphs.

For example, to simulate italic typeface by slanting, apply the following transform `Transform2D(1.0, slant, 0.0, 1.0, 0.0, 0.0)`.

### FontSetUnderlinePosition

```csharp
public void FontSetUnderlinePosition(Rid fontRid, long size, double underlinePosition)
```

引擎名：`font_set_underline_position`

Sets pixel offset of the underline below the baseline.

### FontSetUnderlineThickness

```csharp
public void FontSetUnderlineThickness(Rid fontRid, long size, double underlineThickness)
```

引擎名：`font_set_underline_thickness`

Sets thickness of the underline in pixels.

### FontSetUsedPalette

```csharp
public void FontSetUsedPalette(Rid fontRid, long index)
```

引擎名：`font_set_used_palette`

Sets used palette index.

### FontSetVariationCoordinates

```csharp
public void FontSetVariationCoordinates(Rid fontRid, Dictionary variationCoordinates)
```

引擎名：`font_set_variation_coordinates`

Sets variation coordinates for the specified font cache entry. See `FontSupportedVariationList()` for more info.

### FontSetWeight

```csharp
public void FontSetWeight(Rid fontRid, long weight)
```

引擎名：`font_set_weight`

Sets weight (boldness) of the font. A value in the `100...999` range, normal font weight is `400`, bold font weight is `700`.

**Note:** This value is used for font matching only and will not affect font rendering. Use `FontSetFaceIndex()`, `FontSetVariationCoordinates()`, or `FontSetEmbolden()` instead.

### FontSupportedFeatureList

```csharp
public Dictionary FontSupportedFeatureList(Rid fontRid)
```

引擎名：`font_supported_feature_list`

Returns the dictionary of the supported OpenType features.

### FontSupportedVariationList

```csharp
public Dictionary FontSupportedVariationList(Rid fontRid)
```

引擎名：`font_supported_variation_list`

Returns the dictionary of the supported OpenType variation coordinates.

### FormatNumber

```csharp
public string FormatNumber(string number, string language = "")
```

引擎名：`format_number`

Converts a number from Western Arabic (0..9) to the numeral system used in the given `language`.

If `language` is an empty string, the active locale will be used.

### FreeRid

```csharp
public void FreeRid(Rid rid)
```

引擎名：`free_rid`

Frees an object created by this `TextServer`.

### GetFeatures

```csharp
public long GetFeatures()
```

引擎名：`get_features`

Returns text server features, see [`TextServer.Feature`](TextServer.md).

### GetHexCodeBoxSize

```csharp
public Vector2 GetHexCodeBoxSize(long size, long index)
```

引擎名：`get_hex_code_box_size`

Returns size of the replacement character (box with character hexadecimal code that is drawn in place of invalid characters).

### GetName

```csharp
public string GetName()
```

引擎名：`get_name`

Returns the name of the server interface.

### GetSupportData

```csharp
public byte[] GetSupportData()
```

引擎名：`get_support_data`

Returns default TextServer database (e.g. ICU break iterators and dictionaries).

### GetSupportDataFileName

```csharp
public string GetSupportDataFileName()
```

Returns default TextServer database (e.g. ICU break iterators and dictionaries) filename.

### GetSupportDataInfo

```csharp
public string GetSupportDataInfo()
```

引擎名：`get_support_data_info`

Returns TextServer database (e.g. ICU break iterators and dictionaries) description.

### Has

```csharp
public bool Has(Rid rid)
```

引擎名：`has`

Returns `true` if `rid` is valid resource owned by this text server.

### HasFeature

```csharp
public bool HasFeature(TextServer.Feature feature)
```

引擎名：`has_feature`

Returns `true` if the server supports a feature.

### IsConfusable

```csharp
public long IsConfusable(string string, ReadOnlySpan<string> dict)
```

引擎名：`is_confusable`

Returns index of the first string in `dict` which is visually confusable with the `string`, or `-1` if none is found.

**Note:** This method doesn't detect invisible characters, for spoof detection use it in combination with `SpoofCheck()`.

**Note:** Always returns `-1` if the server does not support the [`TextServer.Feature.UnicodeSecurity`](TextServer.md) feature.

### IsConfusable

```csharp
public long IsConfusable(string string, string[] dict)
```

引擎名：`is_confusable`

Returns index of the first string in `dict` which is visually confusable with the `string`, or `-1` if none is found.

**Note:** This method doesn't detect invisible characters, for spoof detection use it in combination with `SpoofCheck()`.

**Note:** Always returns `-1` if the server does not support the [`TextServer.Feature.UnicodeSecurity`](TextServer.md) feature.

### IsLocaleRightToLeft

```csharp
public bool IsLocaleRightToLeft(string locale)
```

引擎名：`is_locale_right_to_left`

Returns `true` if locale is right-to-left.

### IsLocaleUsingSupportData

```csharp
public bool IsLocaleUsingSupportData(string locale)
```

引擎名：`is_locale_using_support_data`

Returns `true` if the locale requires text server support data for line/word breaking.

### IsValidIdentifier

```csharp
public bool IsValidIdentifier(string string)
```

引擎名：`is_valid_identifier`

Returns `true` if `string` is a valid identifier.

If the text server supports the [`TextServer.Feature.UnicodeIdentifiers`](TextServer.md) feature, a valid identifier must:

- Conform to normalization form C.

- Begin with a Unicode character of class XID_Start or `"_"`.

- May contain Unicode characters of class XID_Continue in the other positions.

- Use UAX #31 recommended scripts only (mixed scripts are allowed).

If the [`TextServer.Feature.UnicodeIdentifiers`](TextServer.md) feature is not supported, a valid identifier must:

- Begin with a Unicode character of class XID_Start or `"_"`.

- May contain Unicode characters of class XID_Continue in the other positions.

### IsValidLetter

```csharp
public bool IsValidLetter(ulong unicode)
```

引擎名：`is_valid_letter`

Returns `true` if the given code point is a valid letter, i.e. it belongs to the Unicode category "L".

### LoadSupportData

```csharp
public bool LoadSupportData(string fileName)
```

引擎名：`load_support_data`

Loads optional TextServer database (e.g. ICU break iterators and dictionaries).

**Note:** This function should be called before any other TextServer functions used, otherwise it won't have any effect.

### NameToTag

```csharp
public long NameToTag(string name)
```

引擎名：`name_to_tag`

Converts the given readable name of a feature, variation, script, or language to an OpenType tag.

### ParseNumber

```csharp
public string ParseNumber(string number, string language = "")
```

引擎名：`parse_number`

Converts `number` from the numeral system used in the given `language` to Western Arabic (0..9).

If `language` is an empty string, the active locale will be used.

### ParseStructuredText

```csharp
public Array<Vector3I> ParseStructuredText(TextServer.StructuredTextParser parserType, Array args, string text)
```

引擎名：`parse_structured_text`

Default implementation of the BiDi algorithm override function.

### PercentSign

```csharp
public string PercentSign(string language = "")
```

引擎名：`percent_sign`

Returns the percent sign used in the given `language`.

If `language` is an empty string, the active locale will be used.

### SaveSupportData

```csharp
public bool SaveSupportData(string fileName)
```

引擎名：`save_support_data`

Saves optional TextServer database (e.g. ICU break iterators and dictionaries) to the file.

**Note:** This function is used by during project export, to include TextServer database.

### ShapedGetRunCount

```csharp
public long ShapedGetRunCount(Rid shaped)
```

引擎名：`shaped_get_run_count`

Returns the number of uniform text runs in the buffer.

### ShapedGetRunDirection

```csharp
public TextServer.Direction ShapedGetRunDirection(Rid shaped, long index)
```

引擎名：`shaped_get_run_direction`

Returns the direction of the `index` text run (in visual order).

### ShapedGetRunFontRid

```csharp
public Rid ShapedGetRunFontRid(Rid shaped, long index)
```

引擎名：`shaped_get_run_font_rid`

Returns the font RID of the `index` text run (in visual order).

### ShapedGetRunFontSize

```csharp
public int ShapedGetRunFontSize(Rid shaped, long index)
```

引擎名：`shaped_get_run_font_size`

Returns the font size of the `index` text run (in visual order).

### ShapedGetRunGlyphRange

```csharp
public Vector2I ShapedGetRunGlyphRange(Rid shaped, long index)
```

引擎名：`shaped_get_run_glyph_range`

Returns the glyph range of the `index` text run (in visual order).

### ShapedGetRunLanguage

```csharp
public string ShapedGetRunLanguage(Rid shaped, long index)
```

引擎名：`shaped_get_run_language`

Returns the language of the `index` text run (in visual order).

### ShapedGetRunObject

```csharp
public Variant ShapedGetRunObject(Rid shaped, long index)
```

引擎名：`shaped_get_run_object`

Returns the embedded object of the `index` text run (in visual order).

### ShapedGetRunRange

```csharp
public Vector2I ShapedGetRunRange(Rid shaped, long index)
```

引擎名：`shaped_get_run_range`

Returns the source text range of the `index` text run (in visual order).

### ShapedGetRunText

```csharp
public string ShapedGetRunText(Rid shaped, long index)
```

引擎名：`shaped_get_run_text`

Returns the source text of the `index` text run (in visual order).

### ShapedGetSpanCount

```csharp
public long ShapedGetSpanCount(Rid shaped)
```

引擎名：`shaped_get_span_count`

Returns number of text spans added using `ShapedTextAddString()` or `ShapedTextAddObject()`.

### ShapedGetSpanEmbeddedObject

```csharp
public Variant ShapedGetSpanEmbeddedObject(Rid shaped, long index)
```

引擎名：`shaped_get_span_embedded_object`

Returns text embedded object key.

### ShapedGetSpanMeta

```csharp
public Variant ShapedGetSpanMeta(Rid shaped, long index)
```

引擎名：`shaped_get_span_meta`

Returns text span metadata.

### ShapedGetSpanObject

```csharp
public Variant ShapedGetSpanObject(Rid shaped, long index)
```

引擎名：`shaped_get_span_object`

Returns the text span embedded object key.

### ShapedGetSpanText

```csharp
public string ShapedGetSpanText(Rid shaped, long index)
```

引擎名：`shaped_get_span_text`

Returns the text span source text.

### ShapedGetText

```csharp
public string ShapedGetText(Rid shaped)
```

引擎名：`shaped_get_text`

Returns the text buffer source text, including object replacement characters.

### ShapedSetSpanUpdateFont

```csharp
public void ShapedSetSpanUpdateFont(Rid shaped, long index, Array<Rid> fonts, long size, Dictionary opentypeFeatures = null)
```

引擎名：`shaped_set_span_update_font`

Changes text span font, font size, and OpenType features, without changing the text.

### ShapedTextAddObject

```csharp
public bool ShapedTextAddObject(Rid shaped, Variant key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, long length = 1, double baseline = 0)
```

引擎名：`shaped_text_add_object`

Adds inline object to the text buffer, `key` must be unique. In the text, object is represented as `length` object replacement characters.

### ShapedTextAddString

```csharp
public bool ShapedTextAddString(Rid shaped, string text, Array<Rid> fonts, long size, Dictionary opentypeFeatures = null, string language = "", Variant meta = default)
```

引擎名：`shaped_text_add_string`

Adds text span and font to draw it to the text buffer.

### ShapedTextClear

```csharp
public void ShapedTextClear(Rid rid)
```

引擎名：`shaped_text_clear`

Clears text buffer (removes text and inline objects).

### ShapedTextClosestCharacterPos

```csharp
public long ShapedTextClosestCharacterPos(Rid shaped, long pos)
```

引擎名：`shaped_text_closest_character_pos`

Returns composite character position closest to the `pos`.

### ShapedTextDraw

```csharp
public void ShapedTextDraw(Rid shaped, Rid canvas, Vector2 pos, double clipL = -1, double clipR = -1, Color? color = null, float oversampling = 0f)
```

引擎名：`shaped_text_draw`

Draw shaped text into a canvas item at a given position, with `color`. `pos` specifies the leftmost point of the baseline (for horizontal layout) or topmost point of the baseline (for vertical layout). If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

`clipL` and `clipR` are offsets relative to `pos`, going to the right in horizontal layout and downward in vertical layout. If `clipL` is not negative, glyphs starting before the offset are clipped. If `clipR` is not negative, glyphs ending after the offset are clipped.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### ShapedTextDraw

```csharp
public void ShapedTextDraw(Rid shaped, Rid canvas, Vector2 pos, double clipL, double clipR, Color? color)
```

引擎名：`shaped_text_draw`

Draw shaped text into a canvas item at a given position, with `color`. `pos` specifies the leftmost point of the baseline (for horizontal layout) or topmost point of the baseline (for vertical layout). If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

`clipL` and `clipR` are offsets relative to `pos`, going to the right in horizontal layout and downward in vertical layout. If `clipL` is not negative, glyphs starting before the offset are clipped. If `clipR` is not negative, glyphs ending after the offset are clipped.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### ShapedTextDrawOutline

```csharp
public void ShapedTextDrawOutline(Rid shaped, Rid canvas, Vector2 pos, double clipL = -1, double clipR = -1, long outlineSize = 1, Color? color = null, float oversampling = 0f)
```

引擎名：`shaped_text_draw_outline`

Draw the outline of the shaped text into a canvas item at a given position, with `color`. `pos` specifies the leftmost point of the baseline (for horizontal layout) or topmost point of the baseline (for vertical layout). If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

`clipL` and `clipR` are offsets relative to `pos`, going to the right in horizontal layout and downward in vertical layout. If `clipL` is not negative, glyphs starting before the offset are clipped. If `clipR` is not negative, glyphs ending after the offset are clipped.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### ShapedTextDrawOutline

```csharp
public void ShapedTextDrawOutline(Rid shaped, Rid canvas, Vector2 pos, double clipL, double clipR, long outlineSize, Color? color)
```

引擎名：`shaped_text_draw_outline`

Draw the outline of the shaped text into a canvas item at a given position, with `color`. `pos` specifies the leftmost point of the baseline (for horizontal layout) or topmost point of the baseline (for vertical layout). If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

`clipL` and `clipR` are offsets relative to `pos`, going to the right in horizontal layout and downward in vertical layout. If `clipL` is not negative, glyphs starting before the offset are clipped. If `clipR` is not negative, glyphs ending after the offset are clipped.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### ShapedTextDuplicate

```csharp
public Rid ShapedTextDuplicate(Rid rid)
```

引擎名：`shaped_text_duplicate`

Duplicates shaped text buffer.

### ShapedTextFitToWidth

```csharp
public double ShapedTextFitToWidth(Rid shaped, double width, TextServer.JustificationFlag justificationFlags = TextServer.JustificationFlag.)
```

引擎名：`shaped_text_fit_to_width`

Adjusts text width to fit to specified width, returns new text width.

### ShapedTextGetAscent

```csharp
public double ShapedTextGetAscent(Rid shaped)
```

引擎名：`shaped_text_get_ascent`

Returns the text ascent (number of pixels above the baseline for horizontal layout or to the left of baseline for vertical).

**Note:** Overall ascent can be higher than font ascent, if some glyphs are displaced from the baseline.

### ShapedTextGetCarets

```csharp
public Dictionary ShapedTextGetCarets(Rid shaped, long position)
```

引擎名：`shaped_text_get_carets`

Returns shapes of the carets corresponding to the character offset `position` in the text. Returned caret shape is 1 pixel wide rectangle.

### ShapedTextGetCharacterBreaks

```csharp
public int[] ShapedTextGetCharacterBreaks(Rid shaped)
```

引擎名：`shaped_text_get_character_breaks`

Returns array of the composite character boundaries.

### ShapedTextGetCustomEllipsis

```csharp
public long ShapedTextGetCustomEllipsis(Rid shaped)
```

引擎名：`shaped_text_get_custom_ellipsis`

Returns ellipsis character used for text clipping.

### ShapedTextGetCustomPunctuation

```csharp
public string ShapedTextGetCustomPunctuation(Rid shaped)
```

引擎名：`shaped_text_get_custom_punctuation`

Returns custom punctuation character list, used for word breaking. If set to empty string, server defaults are used.

### ShapedTextGetDescent

```csharp
public double ShapedTextGetDescent(Rid shaped)
```

引擎名：`shaped_text_get_descent`

Returns the text descent (number of pixels below the baseline for horizontal layout or to the right of baseline for vertical).

**Note:** Overall descent can be higher than font descent, if some glyphs are displaced from the baseline.

### ShapedTextGetDirection

```csharp
public TextServer.Direction ShapedTextGetDirection(Rid shaped)
```

引擎名：`shaped_text_get_direction`

Returns direction of the text.

### ShapedTextGetDominantDirectionInRange

```csharp
public TextServer.Direction ShapedTextGetDominantDirectionInRange(Rid shaped, long start, long end)
```

引擎名：`shaped_text_get_dominant_direction_in_range`

Returns dominant direction of in the range of text.

### ShapedTextGetEllipsisGlyphCount

```csharp
public long ShapedTextGetEllipsisGlyphCount(Rid shaped)
```

引擎名：`shaped_text_get_ellipsis_glyph_count`

Returns number of glyphs in the ellipsis.

### ShapedTextGetEllipsisGlyphs

```csharp
public Array<Dictionary> ShapedTextGetEllipsisGlyphs(Rid shaped)
```

引擎名：`shaped_text_get_ellipsis_glyphs`

Returns array of the glyphs in the ellipsis.

### ShapedTextGetEllipsisPos

```csharp
public long ShapedTextGetEllipsisPos(Rid shaped)
```

引擎名：`shaped_text_get_ellipsis_pos`

Returns position of the ellipsis.

### ShapedTextGetGlyphCount

```csharp
public long ShapedTextGetGlyphCount(Rid shaped)
```

引擎名：`shaped_text_get_glyph_count`

Returns number of glyphs in the buffer.

### ShapedTextGetGlyphs

```csharp
public Array<Dictionary> ShapedTextGetGlyphs(Rid shaped)
```

引擎名：`shaped_text_get_glyphs`

Returns an array of glyphs in the visual order.

### ShapedTextGetGraphemeBounds

```csharp
public Vector2 ShapedTextGetGraphemeBounds(Rid shaped, long pos)
```

引擎名：`shaped_text_get_grapheme_bounds`

Returns composite character's bounds as offsets from the start of the line.

### ShapedTextGetInferredDirection

```csharp
public TextServer.Direction ShapedTextGetInferredDirection(Rid shaped)
```

引擎名：`shaped_text_get_inferred_direction`

Returns direction of the text, inferred by the BiDi algorithm.

### ShapedTextGetLineBreaks

```csharp
public int[] ShapedTextGetLineBreaks(Rid shaped, double width, long start = 0, TextServer.LineBreakFlag breakFlags = TextServer.LineBreakFlag.)
```

引擎名：`shaped_text_get_line_breaks`

Breaks text to the lines and returns character ranges for each line.

### ShapedTextGetLineBreaksAdv

```csharp
public int[] ShapedTextGetLineBreaksAdv(Rid shaped, ReadOnlySpan<float> width, long start, bool once, TextServer.LineBreakFlag breakFlags)
```

引擎名：`shaped_text_get_line_breaks_adv`

Breaks text to the lines and columns. Returns character ranges for each segment.

### ShapedTextGetLineBreaksAdv

```csharp
public int[] ShapedTextGetLineBreaksAdv(Rid shaped, float[] width, long start = 0, bool once = true, TextServer.LineBreakFlag breakFlags = TextServer.LineBreakFlag.)
```

引擎名：`shaped_text_get_line_breaks_adv`

Breaks text to the lines and columns. Returns character ranges for each segment.

### ShapedTextGetObjectGlyph

```csharp
public long ShapedTextGetObjectGlyph(Rid shaped, Variant key)
```

引擎名：`shaped_text_get_object_glyph`

Returns the glyph index of the inline object.

### ShapedTextGetObjectRange

```csharp
public Vector2I ShapedTextGetObjectRange(Rid shaped, Variant key)
```

引擎名：`shaped_text_get_object_range`

Returns the character range of the inline object.

### ShapedTextGetObjectRect

```csharp
public Rect2 ShapedTextGetObjectRect(Rid shaped, Variant key)
```

引擎名：`shaped_text_get_object_rect`

Returns bounding rectangle of the inline object.

### ShapedTextGetObjects

```csharp
public Array ShapedTextGetObjects(Rid shaped)
```

引擎名：`shaped_text_get_objects`

Returns array of inline objects.

### ShapedTextGetOrientation

```csharp
public TextServer.Orientation ShapedTextGetOrientation(Rid shaped)
```

引擎名：`shaped_text_get_orientation`

Returns text orientation.

### ShapedTextGetParent

```csharp
public Rid ShapedTextGetParent(Rid shaped)
```

引擎名：`shaped_text_get_parent`

Returns the parent buffer from which the substring originates.

### ShapedTextGetPreserveControl

```csharp
public bool ShapedTextGetPreserveControl(Rid shaped)
```

引擎名：`shaped_text_get_preserve_control`

Returns `true` if text buffer is configured to display control characters.

### ShapedTextGetPreserveInvalid

```csharp
public bool ShapedTextGetPreserveInvalid(Rid shaped)
```

引擎名：`shaped_text_get_preserve_invalid`

Returns `true` if text buffer is configured to display hexadecimal codes in place of invalid characters.

**Note:** If set to `false`, nothing is displayed in place of invalid characters.

### ShapedTextGetRange

```csharp
public Vector2I ShapedTextGetRange(Rid shaped)
```

引擎名：`shaped_text_get_range`

Returns substring buffer character range in the parent buffer.

### ShapedTextGetSelection

```csharp
public Vector2[] ShapedTextGetSelection(Rid shaped, long start, long end)
```

引擎名：`shaped_text_get_selection`

Returns selection rectangles for the specified character range.

### ShapedTextGetSize

```csharp
public Vector2 ShapedTextGetSize(Rid shaped)
```

引擎名：`shaped_text_get_size`

Returns size of the text.

### ShapedTextGetSpacing

```csharp
public long ShapedTextGetSpacing(Rid shaped, TextServer.SpacingType spacing)
```

引擎名：`shaped_text_get_spacing`

Returns extra spacing added between glyphs or lines in pixels.

### ShapedTextGetTrimPos

```csharp
public long ShapedTextGetTrimPos(Rid shaped)
```

引擎名：`shaped_text_get_trim_pos`

Returns the position of the overrun trim.

### ShapedTextGetUnderlinePosition

```csharp
public double ShapedTextGetUnderlinePosition(Rid shaped)
```

引擎名：`shaped_text_get_underline_position`

Returns pixel offset of the underline below the baseline.

### ShapedTextGetUnderlineThickness

```csharp
public double ShapedTextGetUnderlineThickness(Rid shaped)
```

引擎名：`shaped_text_get_underline_thickness`

Returns thickness of the underline.

### ShapedTextGetWidth

```csharp
public double ShapedTextGetWidth(Rid shaped)
```

引擎名：`shaped_text_get_width`

Returns width (for horizontal layout) or height (for vertical) of the text.

### ShapedTextGetWordBreaks

```csharp
public int[] ShapedTextGetWordBreaks(Rid shaped, TextServer.GraphemeFlag graphemeFlags = TextServer.GraphemeFlag., TextServer.GraphemeFlag skipGraphemeFlags = TextServer.GraphemeFlag.Virtual)
```

引擎名：`shaped_text_get_word_breaks`

Breaks text into words and returns array of character ranges. Use `graphemeFlags` to set what characters are used for breaking.

### ShapedTextGetWordBreaks

```csharp
public int[] ShapedTextGetWordBreaks(Rid shaped, TextServer.GraphemeFlag graphemeFlags)
```

引擎名：`shaped_text_get_word_breaks`

Breaks text into words and returns array of character ranges. Use `graphemeFlags` to set what characters are used for breaking.

### ShapedTextHasObject

```csharp
public bool ShapedTextHasObject(Rid shaped, Variant key)
```

引擎名：`shaped_text_has_object`

Returns `true` if an object with `key` is embedded in this shaped text buffer.

### ShapedTextHasVisibleChars

```csharp
public bool ShapedTextHasVisibleChars(Rid shaped)
```

引擎名：`shaped_text_has_visible_chars`

Returns `true` if text buffer contains any visible characters.

### ShapedTextHitTestGrapheme

```csharp
public long ShapedTextHitTestGrapheme(Rid shaped, double coords)
```

引擎名：`shaped_text_hit_test_grapheme`

Returns grapheme index at the specified pixel offset at the baseline, or `-1` if none is found.

### ShapedTextHitTestPosition

```csharp
public long ShapedTextHitTestPosition(Rid shaped, double coords)
```

引擎名：`shaped_text_hit_test_position`

Returns caret character offset at the specified pixel offset at the baseline. This function always returns a valid position.

### ShapedTextIsReady

```csharp
public bool ShapedTextIsReady(Rid shaped)
```

引擎名：`shaped_text_is_ready`

Returns `true` if buffer is successfully shaped.

### ShapedTextNextCharacterPos

```csharp
public long ShapedTextNextCharacterPos(Rid shaped, long pos)
```

引擎名：`shaped_text_next_character_pos`

Returns composite character end position closest to the `pos`.

### ShapedTextNextGraphemePos

```csharp
public long ShapedTextNextGraphemePos(Rid shaped, long pos)
```

引擎名：`shaped_text_next_grapheme_pos`

Returns grapheme end position closest to the `pos`.

### ShapedTextOverrunTrimToWidth

```csharp
public void ShapedTextOverrunTrimToWidth(Rid shaped, double width = 0, TextServer.TextOverrunFlag overrunTrimFlags = TextServer.TextOverrunFlag.NoTrim)
```

引擎名：`shaped_text_overrun_trim_to_width`

Trims text if it exceeds the given width.

### ShapedTextPrevCharacterPos

```csharp
public long ShapedTextPrevCharacterPos(Rid shaped, long pos)
```

引擎名：`shaped_text_prev_character_pos`

Returns composite character start position closest to the `pos`.

### ShapedTextPrevGraphemePos

```csharp
public long ShapedTextPrevGraphemePos(Rid shaped, long pos)
```

引擎名：`shaped_text_prev_grapheme_pos`

Returns grapheme start position closest to the `pos`.

### ShapedTextResizeObject

```csharp
public bool ShapedTextResizeObject(Rid shaped, Variant key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, double baseline = 0)
```

引擎名：`shaped_text_resize_object`

Sets new size and alignment of embedded object.

### ShapedTextSetBidiOverride

```csharp
public void ShapedTextSetBidiOverride(Rid shaped, Array override)
```

引擎名：`shaped_text_set_bidi_override`

Overrides BiDi for the structured text.

Override ranges should cover full source text without overlaps. BiDi algorithm will be used on each range separately.

### ShapedTextSetCustomEllipsis

```csharp
public void ShapedTextSetCustomEllipsis(Rid shaped, long char)
```

引擎名：`shaped_text_set_custom_ellipsis`

Sets ellipsis character used for text clipping.

### ShapedTextSetCustomPunctuation

```csharp
public void ShapedTextSetCustomPunctuation(Rid shaped, string punct)
```

引擎名：`shaped_text_set_custom_punctuation`

Sets custom punctuation character list, used for word breaking. If set to empty string, server defaults are used.

### ShapedTextSetDirection

```csharp
public void ShapedTextSetDirection(Rid shaped, TextServer.Direction direction = TextServer.Direction.Auto)
```

引擎名：`shaped_text_set_direction`

Sets desired text direction. If set to [`TextServer.Direction.Auto`](TextServer.md), direction will be detected based on the buffer contents and current locale.

**Note:** Direction is ignored if server does not support [`TextServer.Feature.BidiLayout`](TextServer.md) feature (supported by [`TextServerAdvanced`](TextServerAdvanced.md)).

### ShapedTextSetOrientation

```csharp
public void ShapedTextSetOrientation(Rid shaped, TextServer.Orientation orientation = TextServer.Orientation.Horizontal)
```

引擎名：`shaped_text_set_orientation`

Sets desired text orientation.

**Note:** Orientation is ignored if server does not support [`TextServer.Feature.VerticalLayout`](TextServer.md) feature (supported by [`TextServerAdvanced`](TextServerAdvanced.md)).

### ShapedTextSetPreserveControl

```csharp
public void ShapedTextSetPreserveControl(Rid shaped, bool enabled)
```

引擎名：`shaped_text_set_preserve_control`

If set to `true` text buffer will display control characters.

### ShapedTextSetPreserveInvalid

```csharp
public void ShapedTextSetPreserveInvalid(Rid shaped, bool enabled)
```

引擎名：`shaped_text_set_preserve_invalid`

If set to `true` text buffer will display invalid characters as hexadecimal codes, otherwise nothing is displayed.

### ShapedTextSetSpacing

```csharp
public void ShapedTextSetSpacing(Rid shaped, TextServer.SpacingType spacing, long value)
```

引擎名：`shaped_text_set_spacing`

Sets extra spacing added between glyphs or lines in pixels.

### ShapedTextShape

```csharp
public bool ShapedTextShape(Rid shaped)
```

引擎名：`shaped_text_shape`

Shapes buffer if it's not shaped. Returns `true` if the string is shaped successfully.

**Note:** It is not necessary to call this function manually, buffer will be shaped automatically as soon as any of its output data is requested.

### ShapedTextSortLogical

```csharp
public Array<Dictionary> ShapedTextSortLogical(Rid shaped)
```

引擎名：`shaped_text_sort_logical`

Returns text glyphs in the logical order.

### ShapedTextSubstr

```csharp
public Rid ShapedTextSubstr(Rid shaped, long start, long length)
```

引擎名：`shaped_text_substr`

Returns text buffer for the substring of the text in the `shaped` text buffer (including inline objects).

### ShapedTextTabAlign

```csharp
public double ShapedTextTabAlign(Rid shaped, ReadOnlySpan<float> tabStops)
```

引擎名：`shaped_text_tab_align`

Aligns shaped text to the given tab-stops.

### ShapedTextTabAlign

```csharp
public double ShapedTextTabAlign(Rid shaped, float[] tabStops)
```

引擎名：`shaped_text_tab_align`

Aligns shaped text to the given tab-stops.

### SpoofCheck

```csharp
public bool SpoofCheck(string string)
```

引擎名：`spoof_check`

Returns `true` if `string` is likely to be an attempt at confusing the reader.

**Note:** Always returns `false` if the server does not support the [`TextServer.Feature.UnicodeSecurity`](TextServer.md) feature.

### StringGetCharacterBreaks

```csharp
public int[] StringGetCharacterBreaks(string string, string language = "")
```

引擎名：`string_get_character_breaks`

Returns array of the composite character boundaries.

```csharp
var ts = TextServerManager.get_primary_interface()
print(ts.string_get_character_breaks("Test ❤️‍🔥 Test")) # Prints [1, 2, 3, 4, 5, 9, 10, 11, 12, 13, 14]

```

### StringGetWordBreaks

```csharp
public int[] StringGetWordBreaks(string string, string language = "", long charsPerLine = 0)
```

引擎名：`string_get_word_breaks`

Returns an array of the word break boundaries. Elements in the returned array are the offsets of the start and end of words. Therefore the length of the array is always even.

When `charsPerLine` is greater than zero, line break boundaries are returned instead.

```csharp
var ts = TextServerManager.get_primary_interface()
# Corresponds to the substrings "The", "Godot", "Engine", and "4".
print(ts.string_get_word_breaks("The Godot Engine, 4")) # Prints [0, 3, 4, 9, 10, 16, 18, 19]
# Corresponds to the substrings "The", "Godot", "Engin", and "e, 4".
print(ts.string_get_word_breaks("The Godot Engine, 4", "en", 5)) # Prints [0, 3, 4, 9, 10, 15, 15, 19]
# Corresponds to the substrings "The Godot" and "Engine, 4".
print(ts.string_get_word_breaks("The Godot Engine, 4", "en", 10)) # Prints [0, 9, 10, 19]

```

### StringToLower

```csharp
public string StringToLower(string string, string language = "")
```

引擎名：`string_to_lower`

Returns the string converted to `lowercase`.

**Note:** Casing is locale dependent and context sensitive if server support [`TextServer.Feature.ContextSensitiveCaseConversion`](TextServer.md) feature (supported by [`TextServerAdvanced`](TextServerAdvanced.md)).

**Note:** The result may be longer or shorter than the original.

### StringToTitle

```csharp
public string StringToTitle(string string, string language = "")
```

引擎名：`string_to_title`

Returns the string converted to `Title Case`.

**Note:** Casing is locale dependent and context sensitive if server support [`TextServer.Feature.ContextSensitiveCaseConversion`](TextServer.md) feature (supported by [`TextServerAdvanced`](TextServerAdvanced.md)).

**Note:** The result may be longer or shorter than the original.

### StringToUpper

```csharp
public string StringToUpper(string string, string language = "")
```

引擎名：`string_to_upper`

Returns the string converted to `UPPERCASE`.

**Note:** Casing is locale dependent and context sensitive if server support [`TextServer.Feature.ContextSensitiveCaseConversion`](TextServer.md) feature (supported by [`TextServerAdvanced`](TextServerAdvanced.md)).

**Note:** The result may be longer or shorter than the original.

### StripDiacritics

```csharp
public string StripDiacritics(string string)
```

引擎名：`strip_diacritics`

Strips diacritics from the string.

**Note:** The result may be longer or shorter than the original.

### TagToName

```csharp
public string TagToName(long tag)
```

引擎名：`tag_to_name`

Converts the given OpenType tag to the readable name of a feature, variation, script, or language.

## AutowrapMode

命名空间 `Godot`；enum

### 枚举值

- `Off = 0`：Autowrap is disabled.
- `Arbitrary = 1`：Wraps the text inside the node's bounding rectangle by allowing to break lines at arbitrary positions, which is useful when very limited space is available.
- `Word = 2`：Wraps the text inside the node's bounding rectangle by soft-breaking between words.
- `WordSmart = 3`：Behaves similarly to `Word`, but force-breaks a word if that single word does not fit in one line.

## ContourPointTag

命名空间 `Godot`；enum

### 枚举值

- `On = 1`：Contour point is on the curve.
- `OffConic = 0`：Contour point isn't on the curve, but serves as a control point for a conic (quadratic) Bézier arc.
- `OffCubic = 2`：Contour point isn't on the curve, but serves as a control point for a cubic Bézier arc.

## Direction

命名空间 `Godot`；enum

### 枚举值

- `Auto = 0`：Text direction is determined based on contents and current locale.
- `Ltr = 1`：Text is written from left to right.
- `Rtl = 2`：Text is written from right to left.
- `Inherited = 3`：Text writing direction is the same as base string writing direction.

## Feature

命名空间 `Godot`；enum

### 枚举值

- `SimpleLayout = 1`：TextServer supports simple text layouts.
- `BidiLayout = 2`：TextServer supports bidirectional text layouts.
- `VerticalLayout = 4`：TextServer supports vertical layouts.
- `Shaping = 8`：TextServer supports complex text shaping.
- `KashidaJustification = 16`：TextServer supports justification using kashidas.
- `BreakIterators = 32`：TextServer supports complex line/word breaking rules (e.g.
- `FontBitmap = 64`：TextServer supports loading bitmap fonts.
- `FontDynamic = 128`：TextServer supports loading dynamic (TrueType, OpeType, etc.) fonts.
- `FontMsdf = 256`：TextServer supports multichannel signed distance field dynamic font rendering.
- `FontSystem = 512`：TextServer supports loading system fonts.
- `FontVariable = 1024`：TextServer supports variable fonts.
- `ContextSensitiveCaseConversion = 2048`：TextServer supports locale dependent and context sensitive case conversion.
- `UseSupportData = 4096`：TextServer require external data file for some features, see [`TextServer.LoadSupportData`](TextServer.md).
- `UnicodeIdentifiers = 8192`：TextServer supports UAX #31 identifier validation, see [`TextServer.IsValidIdentifier`](TextServer.md).
- `UnicodeSecurity = 16384`：TextServer supports [Unicode Technical Report #36](https://unicode.org/reports/tr36/) and [Unicode Technical Standard #39](https://unicode.org/reports/tr39/) based spoof detection features.

## FixedSizeScaleMode

命名空间 `Godot`；enum

### 枚举值

- `Disable = 0`：Bitmap font is not scaled.
- `IntegerOnly = 1`：Bitmap font is scaled to the closest integer multiple of the font's fixed size.
- `Enabled = 2`：Bitmap font is scaled to an arbitrary (fractional) size.

## FontAntialiasing

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Font glyphs are rasterized as 1-bit bitmaps.
- `Gray = 1`：Font glyphs are rasterized as 8-bit grayscale anti-aliased bitmaps.
- `Lcd = 2`：Font glyphs are rasterized for LCD screens.

## FontLcdSubpixelLayout

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Unknown or unsupported subpixel layout, LCD subpixel antialiasing is disabled.
- `Hrgb = 1`：Horizontal RGB subpixel layout.
- `Hbgr = 2`：Horizontal BGR subpixel layout.
- `Vrgb = 3`：Vertical RGB subpixel layout.
- `Vbgr = 4`：Vertical BGR subpixel layout.
- `Max = 5`：Represents the size of the `TextServer.FontLcdSubpixelLayout` enum.

## FontStyle

命名空间 `Godot`；enum

### 枚举值

- `Bold = 1`：Font is bold.
- `Italic = 2`：Font is italic or oblique.
- `FixedWidth = 4`：Font has fixed-width characters (also known as monospace).

## GraphemeFlag

命名空间 `Godot`；enum

### 枚举值

- `Valid = 1`：Grapheme is supported by the font, and can be drawn.
- `Rtl = 2`：Grapheme is part of right-to-left or bottom-to-top run.
- `Virtual = 4`：Grapheme is not part of source text, it was added by justification process.
- `Space = 8`：Grapheme is whitespace.
- `BreakHard = 16`：Grapheme is mandatory break point (e.g.
- `BreakSoft = 32`：Grapheme is optional break point (e.g.
- `Tab = 64`：Grapheme is the tabulation character.
- `Elongation = 128`：Grapheme is kashida.
- `Punctuation = 256`：Grapheme is punctuation character.
- `Underscore = 512`：Grapheme is underscore character.
- `Connected = 1024`：Grapheme is connected to the previous grapheme.
- `SafeToInsertTatweel = 2048`：It is safe to insert a U+0640 before this grapheme for elongation.
- `EmbeddedObject = 4096`：Grapheme is an object replacement character for the embedded object.
- `SoftHyphen = 8192`：Grapheme is a soft hyphen.

## Hinting

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Disables font hinting (smoother but less crisp).
- `Light = 1`：Use the light font hinting mode.
- `Normal = 2`：Use the default font hinting mode (crisper but less smooth).

## JustificationFlag

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Do not justify text.
- `Kashida = 1`：Justify text by adding and removing kashidas.
- `WordBound = 2`：Justify text by changing width of the spaces between the words.
- `TrimEdgeSpaces = 4`：Remove trailing and leading spaces from the justified text.
- `AfterLastTab = 8`：Only apply justification to the part of the text after the last tab.
- `ConstrainEllipsis = 16`：Apply justification to the trimmed line with ellipsis.
- `SkipLastLine = 32`：Do not apply justification to the last line of the paragraph.
- `SkipLastLineWithVisibleChars = 64`：Do not apply justification to the last line of the paragraph with visible characters (takes precedence over `SkipLastLine`).
- `DoNotSkipSingleLine = 128`：Always apply justification to the paragraphs with a single line (`SkipLastLine` and `SkipLastLineWithVisibleChars` are ignored).

## LineBreakFlag

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Do not break the line.
- `Mandatory = 1`：Break the line at the line mandatory break characters (e.g.
- `WordBound = 2`：Break the line between the words.
- `GraphemeBound = 4`：Break the line between any unconnected graphemes.
- `Adaptive = 8`：Should be used only in conjunction with `WordBound`, break the line between any unconnected graphemes, if it's impossible to break it between the words.
- `TrimEdgeSpaces = 16`：Remove edge spaces from the broken line segments.
- `TrimIndent = 32`：Subtract first line indentation width from all lines after the first one.
- `TrimStartEdgeSpaces = 64`：Remove spaces and line break characters from the start of broken line segments.
- `TrimEndEdgeSpaces = 128`：Remove spaces and line break characters from the end of broken line segments.

## Orientation

命名空间 `Godot`；enum

### 枚举值

- `Horizontal = 0`：Text is written horizontally.
- `Vertical = 1`：Left to right text is written vertically from top to bottom.

## OverrunBehavior

命名空间 `Godot`；enum

### 枚举值

- `NoTrimming = 0`：No text trimming is performed.
- `TrimChar = 1`：Trims the text per character.
- `TrimWord = 2`：Trims the text per word.
- `TrimEllipsis = 3`：Trims the text per character and adds an ellipsis to indicate that parts are hidden if trimmed text is 6 characters or longer.
- `TrimWordEllipsis = 4`：Trims the text per word and adds an ellipsis to indicate that parts are hidden if trimmed text is 6 characters or longer.
- `TrimEllipsisForce = 5`：Trims the text per character and adds an ellipsis to indicate that parts are hidden regardless of trimmed text length.
- `TrimWordEllipsisForce = 6`：Trims the text per word and adds an ellipsis to indicate that parts are hidden regardless of trimmed text length.

## SpacingType

命名空间 `Godot`；enum

### 枚举值

- `Glyph = 0`：Spacing for each glyph.
- `Space = 1`：Spacing for the space character.
- `Top = 2`：Spacing at the top of the line.
- `Bottom = 3`：Spacing at the bottom of the line.
- `Max = 4`：Represents the size of the `TextServer.SpacingType` enum.

## StructuredTextParser

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Use default Unicode BiDi algorithm.
- `Uri = 1`：BiDi override for URI.
- `File = 2`：BiDi override for file path.
- `Email = 3`：BiDi override for email.
- `List = 4`：BiDi override for lists.
- `Gdscript = 5`：BiDi override for GDScript.
- `Custom = 6`：User defined structured text BiDi override function.

## SubpixelPositioning

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Glyph horizontal position is rounded to the whole pixel size, each glyph is rasterized once.
- `Auto = 1`：Glyph horizontal position is rounded based on font size.
- `OneHalf = 2`：Glyph horizontal position is rounded to one half of the pixel size, each glyph is rasterized up to two times.
- `OneQuarter = 3`：Glyph horizontal position is rounded to one quarter of the pixel size, each glyph is rasterized up to four times.
- `OneHalfMaxSize = 20`：Maximum font size which will use "one half of the pixel" subpixel positioning in `Auto` mode.
- `OneQuarterMaxSize = 16`：Maximum font size which will use "one quarter of the pixel" subpixel positioning in `Auto` mode.

## TextOverrunFlag

命名空间 `Godot`；enum

### 枚举值

- `NoTrim = 0`：No trimming is performed.
- `Trim = 1`：Trims the text when it exceeds the given width.
- `TrimWordOnly = 2`：Trims the text per word instead of per grapheme.
- `AddEllipsis = 4`：Determines whether an ellipsis should be added at the end of the text.
- `EnforceEllipsis = 8`：Determines whether the ellipsis at the end of the text is enforced and may not be hidden.
- `JustificationAware = 16`：Accounts for the text being justified before attempting to trim it (see [`TextServer.JustificationFlag`](TextServer.md)).
- `ShortStringEllipsis = 32`：Determines whether the ellipsis should be added regardless of the string length, otherwise it is added only if the string is 6 characters or longer.

## VisibleCharactersBehavior

命名空间 `Godot`；enum

### 枚举值

- `CharsBeforeShaping = 0`：Trims text before the shaping.
- `CharsAfterShaping = 1`：Displays glyphs that are mapped to the first [`Label.VisibleCharacters`](Label.md) or [`RichTextLabel.VisibleCharacters`](RichTextLabel.md) characters from the beginning of the text.
- `GlyphsAuto = 2`：Displays [`Label.VisibleRatio`](Label.md) or [`RichTextLabel.VisibleRatio`](RichTextLabel.md) glyphs, starting from the left or from the right, depending on [`Control.LayoutDirection`](Control.md) value.
- `GlyphsLtr = 3`：Displays [`Label.VisibleRatio`](Label.md) or [`RichTextLabel.VisibleRatio`](RichTextLabel.md) glyphs, starting from the left.
- `GlyphsRtl = 4`：Displays [`Label.VisibleRatio`](Label.md) or [`RichTextLabel.VisibleRatio`](RichTextLabel.md) glyphs, starting from the right.
