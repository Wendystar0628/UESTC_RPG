<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# FontFile

命名空间 `Godot`；class；继承 [`Font`](Font.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`FontFile` contains a set of glyphs to represent Unicode characters imported from a font file, as well as a cache of rasterized glyphs, and a set of fallback [`Font`](Font.md)s to use.

Use [`FontVariation`](FontVariation.md) to access specific OpenType variation of the font, create simulated bold / slanted version, and draw lines of text.

For more complex text processing, use [`FontVariation`](FontVariation.md) in conjunction with [`TextLine`](TextLine.md) or [`TextParagraph`](TextParagraph.md).

Supported font formats:

- Dynamic font importer: TrueType (.ttf), TrueType collection (.ttc), OpenType (.otf), OpenType collection (.otc), WOFF (.woff), WOFF2 (.woff2), Type 1 (.pfb, .pfm).

- Bitmap font importer: AngelCode BMFont (.fnt, .font), text and binary (version 3) format variants.

- Monospace image font importer: All supported image formats.

**Note:** A character is a symbol that represents an item (letter, digit etc.) in an abstract way.

**Note:** A glyph is a bitmap or a shape used to draw one or more characters in a context-dependent manner. Glyph indices are bound to the specific font data source.

**Note:** If none of the font data sources contain glyphs for a character used in a string, the character in question will be replaced with a box displaying its hexadecimal code.

```csharp
var f = ResourceLoader.Load<FontFile>("res://BarlowCondensed-Bold.ttf");
GetNode("Label").AddThemeFontOverride("font", f);
GetNode("Label").AddThemeFontSizeOverride("font_size", 64);

```

## 构造函数

### FontFile

```csharp
public FontFile()
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

### Data

```csharp
public byte[] Data { get; set; }
```

引擎名：`data`

Contents of the dynamic font source file.

### DisableEmbeddedBitmaps

```csharp
public bool DisableEmbeddedBitmaps { get; set; }
```

引擎名：`disable_embedded_bitmaps`

If set to `true`, embedded font bitmap loading is disabled (bitmap-only and color fonts ignore this property).

### FixedSize

```csharp
public int FixedSize { get; set; }
```

引擎名：`fixed_size`

Font size, used only for the bitmap fonts.

### FixedSizeScaleMode

```csharp
public TextServer.FixedSizeScaleMode FixedSizeScaleMode { get; set; }
```

引擎名：`fixed_size_scale_mode`

Scaling mode, used only for the bitmap fonts with `FixedSize` greater than zero.

### FontName

```csharp
public string FontName { get; set; }
```

引擎名：`font_name`

Font family name.

### FontStretch

```csharp
public int FontStretch { get; set; }
```

引擎名：`font_stretch`

Font stretch amount, compared to a normal width. A percentage value between `50%` and `200%`.

### FontStyle

```csharp
public TextServer.FontStyle FontStyle { get; set; }
```

引擎名：`font_style`

Font style flags.

### FontWeight

```csharp
public int FontWeight { get; set; }
```

引擎名：`font_weight`

Weight (boldness) of the font. A value in the `100...999` range, normal font weight is `400`, bold font weight is `700`.

### ForceAutohinter

```csharp
public bool ForceAutohinter { get; set; }
```

引擎名：`force_autohinter`

If set to `true`, auto-hinting is supported and preferred over font built-in hinting. Used by dynamic fonts only (MSDF fonts don't support hinting).

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

Font hinting mode. Used by dynamic fonts only.

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

If set to `true`, glyphs of all sizes are rendered using single multichannel signed distance field (MSDF) generated from the dynamic font vector data. Since this approach does not rely on rasterizing the font every time its size changes, this allows for resizing the font in real-time without any performance penalty. Text will also not look grainy for [`Control`](Control.md)s that are scaled down (or for [`Label3D`](Label3D.md)s viewed from a long distance). As a downside, font hinting is not available with MSDF. The lack of font hinting may result in less crisp and less readable fonts at small sizes.

**Note:** If using font outlines, `MsdfPixelRange` must be set to at least *twice* the size of the largest font outline.

**Note:** MSDF font rendering does not render glyphs with overlapping shapes correctly. Overlapping shapes are not valid per the OpenType standard, but are still commonly found in many font files, especially those converted by Google Fonts. To avoid issues with overlapping glyphs, consider downloading the font file directly from the type foundry instead of relying on Google Fonts.

### OpentypeFeatureOverrides

```csharp
public Dictionary OpentypeFeatureOverrides { get; set; }
```

引擎名：`opentype_feature_overrides`

Font OpenType feature set override.

### Oversampling

```csharp
public float Oversampling { get; set; }
```

引擎名：`oversampling`

If set to a positive value, overrides the oversampling factor of the viewport this font is used in. See [`Viewport.Oversampling`](Viewport.md). This value doesn't override the `oversampling` parameter of `draw_*` methods.

### StyleName

```csharp
public string StyleName { get; set; }
```

引擎名：`style_name`

Font style name.

### SubpixelPositioning

```csharp
public TextServer.SubpixelPositioning SubpixelPositioning { get; set; }
```

引擎名：`subpixel_positioning`

Font glyph subpixel positioning mode. Subpixel positioning provides shaper text and better kerning for smaller font sizes, at the cost of higher memory usage and lower font rasterization speed. Use [`TextServer.SubpixelPositioning.Auto`](TextServer.md) to automatically enable it based on the font size.

## 方法

### ClearCache

```csharp
public void ClearCache()
```

引擎名：`clear_cache`

Removes all font cache entries.

### ClearGlyphs

```csharp
public void ClearGlyphs(int cacheIndex, Vector2I size)
```

引擎名：`clear_glyphs`

Removes all rendered glyph information from the cache entry.

**Note:** This function will not remove textures associated with the glyphs, use `RemoveTexture()` to remove them manually.

### ClearKerningMap

```csharp
public void ClearKerningMap(int cacheIndex, int size)
```

引擎名：`clear_kerning_map`

Removes all kerning overrides.

### ClearSizeCache

```csharp
public void ClearSizeCache(int cacheIndex)
```

引擎名：`clear_size_cache`

Removes all font sizes from the cache entry.

### ClearTextures

```csharp
public void ClearTextures(int cacheIndex, Vector2I size)
```

引擎名：`clear_textures`

Removes all textures from font cache entry.

**Note:** This function will not remove glyphs associated with the texture, use `RemoveGlyph()` to remove them manually.

### GetAntialiasing

```csharp
public TextServer.FontAntialiasing GetAntialiasing()
```

引擎名：`get_antialiasing`

### GetCacheAscent

```csharp
public float GetCacheAscent(int cacheIndex, int size)
```

引擎名：`get_cache_ascent`

Returns the font ascent (number of pixels above the baseline).

### GetCacheCount

```csharp
public int GetCacheCount()
```

引擎名：`get_cache_count`

Returns number of the font cache entries.

### GetCacheDescent

```csharp
public float GetCacheDescent(int cacheIndex, int size)
```

引擎名：`get_cache_descent`

Returns the font descent (number of pixels below the baseline).

### GetCacheScale

```csharp
public float GetCacheScale(int cacheIndex, int size)
```

引擎名：`get_cache_scale`

Returns scaling factor of the color bitmap font.

### GetCacheUnderlinePosition

```csharp
public float GetCacheUnderlinePosition(int cacheIndex, int size)
```

引擎名：`get_cache_underline_position`

Returns pixel offset of the underline below the baseline.

### GetCacheUnderlineThickness

```csharp
public float GetCacheUnderlineThickness(int cacheIndex, int size)
```

引擎名：`get_cache_underline_thickness`

Returns thickness of the underline in pixels.

### GetCharFromGlyphIndex

```csharp
public long GetCharFromGlyphIndex(int size, int glyphIndex)
```

引擎名：`get_char_from_glyph_index`

Returns character code associated with `glyphIndex`, or `0` if `glyphIndex` is invalid. See `GetGlyphIndex()`.

### GetData

```csharp
public byte[] GetData()
```

引擎名：`get_data`

### GetDisableEmbeddedBitmaps

```csharp
public bool GetDisableEmbeddedBitmaps()
```

引擎名：`get_disable_embedded_bitmaps`

### GetEmbolden

```csharp
public float GetEmbolden(int cacheIndex)
```

引擎名：`get_embolden`

Returns embolden strength, if is not equal to zero, emboldens the font outlines. Negative values reduce the outline thickness.

### GetExtraBaselineOffset

```csharp
public float GetExtraBaselineOffset(int cacheIndex)
```

引擎名：`get_extra_baseline_offset`

Returns extra baseline offset (as a fraction of font height).

### GetExtraSpacing

```csharp
public long GetExtraSpacing(int cacheIndex, TextServer.SpacingType spacing)
```

引擎名：`get_extra_spacing`

Returns spacing for `spacing` in pixels (not relative to the font size).

### GetFaceIndex

```csharp
public long GetFaceIndex(int cacheIndex)
```

引擎名：`get_face_index`

Returns an active face index in the TrueType / OpenType collection.

### GetFixedSize

```csharp
public int GetFixedSize()
```

引擎名：`get_fixed_size`

### GetFixedSizeScaleMode

```csharp
public TextServer.FixedSizeScaleMode GetFixedSizeScaleMode()
```

引擎名：`get_fixed_size_scale_mode`

### GetGenerateMipmaps

```csharp
public bool GetGenerateMipmaps()
```

引擎名：`get_generate_mipmaps`

### GetGlyphAdvance

```csharp
public Vector2 GetGlyphAdvance(int cacheIndex, int size, int glyph)
```

引擎名：`get_glyph_advance`

Returns glyph advance (offset of the next glyph).

**Note:** Advance for glyphs outlines is the same as the base glyph advance and is not saved.

### GetGlyphIndex

```csharp
public int GetGlyphIndex(int size, long char, long variationSelector)
```

引擎名：`get_glyph_index`

Returns the glyph index of a `char`, optionally modified by the `variationSelector`.

### GetGlyphList

```csharp
public int[] GetGlyphList(int cacheIndex, Vector2I size)
```

引擎名：`get_glyph_list`

Returns list of rendered glyphs in the cache entry.

### GetGlyphOffset

```csharp
public Vector2 GetGlyphOffset(int cacheIndex, Vector2I size, int glyph)
```

引擎名：`get_glyph_offset`

Returns glyph offset from the baseline.

### GetGlyphSize

```csharp
public Vector2 GetGlyphSize(int cacheIndex, Vector2I size, int glyph)
```

引擎名：`get_glyph_size`

Returns glyph size.

### GetGlyphTextureIdx

```csharp
public int GetGlyphTextureIdx(int cacheIndex, Vector2I size, int glyph)
```

引擎名：`get_glyph_texture_idx`

Returns index of the cache texture containing the glyph.

### GetGlyphUVRect

```csharp
public Rect2 GetGlyphUVRect(int cacheIndex, Vector2I size, int glyph)
```

引擎名：`get_glyph_uv_rect`

Returns rectangle in the cache texture containing the glyph.

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

### GetKerning

```csharp
public Vector2 GetKerning(int cacheIndex, int size, Vector2I glyphPair)
```

引擎名：`get_kerning`

Returns kerning for the pair of glyphs.

### GetKerningList

```csharp
public Array<Vector2I> GetKerningList(int cacheIndex, int size)
```

引擎名：`get_kerning_list`

Returns list of the kerning overrides.

### GetLanguageSupportOverride

```csharp
public bool GetLanguageSupportOverride(string language)
```

引擎名：`get_language_support_override`

Returns `true` if support override is enabled for the `language`.

### GetLanguageSupportOverrides

```csharp
public string[] GetLanguageSupportOverrides()
```

引擎名：`get_language_support_overrides`

Returns list of language support overrides.

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

### GetOpentypeFeatureOverrides

```csharp
public Dictionary GetOpentypeFeatureOverrides()
```

引擎名：`get_opentype_feature_overrides`

### GetOversampling

```csharp
public float GetOversampling()
```

引擎名：`get_oversampling`

### GetScriptSupportOverride

```csharp
public bool GetScriptSupportOverride(string script)
```

引擎名：`get_script_support_override`

Returns `true` if support override is enabled for the `script`.

### GetScriptSupportOverrides

```csharp
public string[] GetScriptSupportOverrides()
```

引擎名：`get_script_support_overrides`

Returns list of script support overrides.

### GetSizeCacheList

```csharp
public Array<Vector2I> GetSizeCacheList(int cacheIndex)
```

引擎名：`get_size_cache_list`

Returns list of the font sizes in the cache. Each size is [`Vector2I`](Vector2I.md) with font size and outline size.

### GetSubpixelPositioning

```csharp
public TextServer.SubpixelPositioning GetSubpixelPositioning()
```

引擎名：`get_subpixel_positioning`

### GetTextureCount

```csharp
public int GetTextureCount(int cacheIndex, Vector2I size)
```

引擎名：`get_texture_count`

Returns number of textures used by font cache entry.

### GetTextureImage

```csharp
public Image GetTextureImage(int cacheIndex, Vector2I size, int textureIndex)
```

引擎名：`get_texture_image`

Returns a copy of the font cache texture image.

### GetTextureOffsets

```csharp
public int[] GetTextureOffsets(int cacheIndex, Vector2I size, int textureIndex)
```

引擎名：`get_texture_offsets`

Returns a copy of the array containing glyph packing data.

### GetTransform

```csharp
public Transform2D GetTransform(int cacheIndex)
```

引擎名：`get_transform`

Returns 2D transform, applied to the font outlines, can be used for slanting, flipping and rotating glyphs.

### GetVariationCoordinates

```csharp
public Dictionary GetVariationCoordinates(int cacheIndex)
```

引擎名：`get_variation_coordinates`

Returns variation coordinates for the specified font cache entry. See [`Font.GetSupportedVariationList`](Font.md) for more info.

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

### LoadBitmapFont

```csharp
public Error LoadBitmapFont(string path)
```

引擎名：`load_bitmap_font`

Loads an AngelCode BMFont (.fnt, .font) bitmap font from file `path`.

**Warning:** This method should only be used in the editor or in cases when you need to load external fonts at run-time, such as fonts located at the `user://` directory.

### LoadDynamicFont

```csharp
public Error LoadDynamicFont(string path)
```

引擎名：`load_dynamic_font`

Loads a TrueType (.ttf), OpenType (.otf), WOFF (.woff), WOFF2 (.woff2) or Type 1 (.pfb, .pfm) dynamic font from file `path`.

**Warning:** This method should only be used in the editor or in cases when you need to load external fonts at run-time, such as fonts located at the `user://` directory.

### RemoveCache

```csharp
public void RemoveCache(int cacheIndex)
```

引擎名：`remove_cache`

Removes specified font cache entry.

### RemoveGlyph

```csharp
public void RemoveGlyph(int cacheIndex, Vector2I size, int glyph)
```

引擎名：`remove_glyph`

Removes specified rendered glyph information from the cache entry.

**Note:** This function will not remove textures associated with the glyphs, use `RemoveTexture()` to remove them manually.

### RemoveKerning

```csharp
public void RemoveKerning(int cacheIndex, int size, Vector2I glyphPair)
```

引擎名：`remove_kerning`

Removes kerning override for the pair of glyphs.

### RemoveLanguageSupportOverride

```csharp
public void RemoveLanguageSupportOverride(string language)
```

引擎名：`remove_language_support_override`

Remove language support override.

### RemoveScriptSupportOverride

```csharp
public void RemoveScriptSupportOverride(string script)
```

引擎名：`remove_script_support_override`

Removes script support override.

### RemoveSizeCache

```csharp
public void RemoveSizeCache(int cacheIndex, Vector2I size)
```

引擎名：`remove_size_cache`

Removes specified font size from the cache entry.

### RemoveTexture

```csharp
public void RemoveTexture(int cacheIndex, Vector2I size, int textureIndex)
```

引擎名：`remove_texture`

Removes specified texture from the cache entry.

**Note:** This function will not remove glyphs associated with the texture. Remove them manually using `RemoveGlyph()`.

### RenderGlyph

```csharp
public void RenderGlyph(int cacheIndex, Vector2I size, int index)
```

引擎名：`render_glyph`

Renders specified glyph to the font cache texture.

### RenderRange

```csharp
public void RenderRange(int cacheIndex, Vector2I size, long start, long end)
```

引擎名：`render_range`

Renders the range of characters to the font cache texture.

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

### SetCacheAscent

```csharp
public void SetCacheAscent(int cacheIndex, int size, float ascent)
```

引擎名：`set_cache_ascent`

Sets the font ascent (number of pixels above the baseline).

### SetCacheDescent

```csharp
public void SetCacheDescent(int cacheIndex, int size, float descent)
```

引擎名：`set_cache_descent`

Sets the font descent (number of pixels below the baseline).

### SetCacheScale

```csharp
public void SetCacheScale(int cacheIndex, int size, float scale)
```

引擎名：`set_cache_scale`

Sets scaling factor of the color bitmap font.

### SetCacheUnderlinePosition

```csharp
public void SetCacheUnderlinePosition(int cacheIndex, int size, float underlinePosition)
```

引擎名：`set_cache_underline_position`

Sets pixel offset of the underline below the baseline.

### SetCacheUnderlineThickness

```csharp
public void SetCacheUnderlineThickness(int cacheIndex, int size, float underlineThickness)
```

引擎名：`set_cache_underline_thickness`

Sets thickness of the underline in pixels.

### SetData

```csharp
public void SetData(ReadOnlySpan<byte> data)
```

引擎名：`set_data`

### SetData

```csharp
public void SetData(byte[] data)
```

引擎名：`set_data`

### SetDisableEmbeddedBitmaps

```csharp
public void SetDisableEmbeddedBitmaps(bool disableEmbeddedBitmaps)
```

引擎名：`set_disable_embedded_bitmaps`

### SetEmbolden

```csharp
public void SetEmbolden(int cacheIndex, float strength)
```

引擎名：`set_embolden`

Sets embolden strength, if is not equal to zero, emboldens the font outlines. Negative values reduce the outline thickness.

### SetExtraBaselineOffset

```csharp
public void SetExtraBaselineOffset(int cacheIndex, float baselineOffset)
```

引擎名：`set_extra_baseline_offset`

Sets extra baseline offset (as a fraction of font height).

### SetExtraSpacing

```csharp
public void SetExtraSpacing(int cacheIndex, TextServer.SpacingType spacing, long value)
```

引擎名：`set_extra_spacing`

Sets the spacing for `spacing` to `value` in pixels (not relative to the font size).

### SetFaceIndex

```csharp
public void SetFaceIndex(int cacheIndex, long faceIndex)
```

引擎名：`set_face_index`

Sets an active face index in the TrueType / OpenType collection.

### SetFixedSize

```csharp
public void SetFixedSize(int fixedSize)
```

引擎名：`set_fixed_size`

### SetFixedSizeScaleMode

```csharp
public void SetFixedSizeScaleMode(TextServer.FixedSizeScaleMode fixedSizeScaleMode)
```

引擎名：`set_fixed_size_scale_mode`

### SetFontName

```csharp
public void SetFontName(string name)
```

引擎名：`set_font_name`

### SetFontStretch

```csharp
public void SetFontStretch(int stretch)
```

引擎名：`set_font_stretch`

### SetFontStyle

```csharp
public void SetFontStyle(TextServer.FontStyle style)
```

引擎名：`set_font_style`

### SetFontStyleName

```csharp
public void SetFontStyleName(string name)
```

引擎名：`set_font_style_name`

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

### SetGlyphAdvance

```csharp
public void SetGlyphAdvance(int cacheIndex, int size, int glyph, Vector2 advance)
```

引擎名：`set_glyph_advance`

Sets glyph advance (offset of the next glyph).

**Note:** Advance for glyphs outlines is the same as the base glyph advance and is not saved.

### SetGlyphOffset

```csharp
public void SetGlyphOffset(int cacheIndex, Vector2I size, int glyph, Vector2 offset)
```

引擎名：`set_glyph_offset`

Sets glyph offset from the baseline.

### SetGlyphSize

```csharp
public void SetGlyphSize(int cacheIndex, Vector2I size, int glyph, Vector2 glSize)
```

引擎名：`set_glyph_size`

Sets glyph size.

### SetGlyphTextureIdx

```csharp
public void SetGlyphTextureIdx(int cacheIndex, Vector2I size, int glyph, int textureIdx)
```

引擎名：`set_glyph_texture_idx`

Sets index of the cache texture containing the glyph.

### SetGlyphUVRect

```csharp
public void SetGlyphUVRect(int cacheIndex, Vector2I size, int glyph, Rect2 uVRect)
```

引擎名：`set_glyph_uv_rect`

Sets rectangle in the cache texture containing the glyph.

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

### SetKerning

```csharp
public void SetKerning(int cacheIndex, int size, Vector2I glyphPair, Vector2 kerning)
```

引擎名：`set_kerning`

Sets kerning for the pair of glyphs.

### SetLanguageSupportOverride

```csharp
public void SetLanguageSupportOverride(string language, bool supported)
```

引擎名：`set_language_support_override`

Adds override for [`Font.IsLanguageSupported`](Font.md).

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

### SetOpentypeFeatureOverrides

```csharp
public void SetOpentypeFeatureOverrides(Dictionary overrides)
```

引擎名：`set_opentype_feature_overrides`

### SetOversampling

```csharp
public void SetOversampling(float oversampling)
```

引擎名：`set_oversampling`

### SetScriptSupportOverride

```csharp
public void SetScriptSupportOverride(string script, bool supported)
```

引擎名：`set_script_support_override`

Adds override for [`Font.IsScriptSupported`](Font.md).

### SetSubpixelPositioning

```csharp
public void SetSubpixelPositioning(TextServer.SubpixelPositioning subpixelPositioning)
```

引擎名：`set_subpixel_positioning`

### SetTextureImage

```csharp
public void SetTextureImage(int cacheIndex, Vector2I size, int textureIndex, Image image)
```

引擎名：`set_texture_image`

Sets font cache texture image.

### SetTextureOffsets

```csharp
public void SetTextureOffsets(int cacheIndex, Vector2I size, int textureIndex, ReadOnlySpan<int> offset)
```

引擎名：`set_texture_offsets`

Sets array containing glyph packing data.

### SetTextureOffsets

```csharp
public void SetTextureOffsets(int cacheIndex, Vector2I size, int textureIndex, int[] offset)
```

引擎名：`set_texture_offsets`

Sets array containing glyph packing data.

### SetTransform

```csharp
public void SetTransform(int cacheIndex, Transform2D transform)
```

引擎名：`set_transform`

Sets 2D transform, applied to the font outlines, can be used for slanting, flipping, and rotating glyphs.

### SetVariationCoordinates

```csharp
public void SetVariationCoordinates(int cacheIndex, Dictionary variationCoordinates)
```

引擎名：`set_variation_coordinates`

Sets variation coordinates for the specified font cache entry. See [`Font.GetSupportedVariationList`](Font.md) for more info.
