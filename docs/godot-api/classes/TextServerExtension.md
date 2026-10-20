<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextServerExtension

命名空间 `Godot`；class；继承 [`TextServer`](TextServer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

External [`TextServer`](TextServer.md) implementations should inherit from this class.

## 构造函数

### TextServerExtension

```csharp
public TextServerExtension()
```

## 方法

### _Cleanup

```csharp
public virtual void _Cleanup()
```

引擎名：`_cleanup`

This method is called before text server is unregistered.

### _CreateFont

```csharp
public virtual Rid _CreateFont()
```

引擎名：`_create_font`

Creates a new, empty font cache entry resource.

### _CreateFontLinkedVariation

```csharp
public virtual Rid _CreateFontLinkedVariation(Rid fontRid)
```

引擎名：`_create_font_linked_variation`

Optional, implement if font supports extra spacing or baseline offset.

Creates a new variation existing font which is reusing the same glyph cache and font data.

### _CreateShapedText

```csharp
public virtual Rid _CreateShapedText(TextServer.Direction direction, TextServer.Orientation orientation)
```

引擎名：`_create_shaped_text`

Creates a new buffer for complex text layout, with the given `direction` and `orientation`.

### _DrawHexCodeBox

```csharp
public virtual void _DrawHexCodeBox(Rid canvas, long size, Vector2 pos, long index, Color color)
```

引擎名：`_draw_hex_code_box`

Draws box displaying character hexadecimal code.

### _FontClearGlyphs

```csharp
public virtual void _FontClearGlyphs(Rid fontRid, Vector2I size)
```

引擎名：`_font_clear_glyphs`

Removes all rendered glyph information from the cache entry.

### _FontClearKerningMap

```csharp
public virtual void _FontClearKerningMap(Rid fontRid, long size)
```

引擎名：`_font_clear_kerning_map`

Removes all kerning overrides.

### _FontClearSizeCache

```csharp
public virtual void _FontClearSizeCache(Rid fontRid)
```

引擎名：`_font_clear_size_cache`

Removes all font sizes from the cache entry.

### _FontClearSystemFallbackCache

```csharp
public virtual void _FontClearSystemFallbackCache()
```

引擎名：`_font_clear_system_fallback_cache`

Frees all automatically loaded system fonts.

### _FontClearTextures

```csharp
public virtual void _FontClearTextures(Rid fontRid, Vector2I size)
```

引擎名：`_font_clear_textures`

Removes all textures from font cache entry.

### _FontDrawGlyph

```csharp
public virtual void _FontDrawGlyph(Rid fontRid, Rid canvas, long size, Vector2 pos, long index, Color color, float oversampling)
```

引擎名：`_font_draw_glyph`

Draws single glyph into a canvas item at the position, using `fontRid` at the size `size`. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

### _FontDrawGlyphOutline

```csharp
public virtual void _FontDrawGlyphOutline(Rid fontRid, Rid canvas, long size, long outlineSize, Vector2 pos, long index, Color color, float oversampling)
```

引擎名：`_font_draw_glyph_outline`

Draws single glyph outline of size `outlineSize` into a canvas item at the position, using `fontRid` at the size `size`. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

### _FontGetAntialiasing

```csharp
public virtual TextServer.FontAntialiasing _FontGetAntialiasing(Rid fontRid)
```

引擎名：`_font_get_antialiasing`

Returns font anti-aliasing mode.

### _FontGetAscent

```csharp
public virtual double _FontGetAscent(Rid fontRid, long size)
```

引擎名：`_font_get_ascent`

Returns the font ascent (number of pixels above the baseline).

### _FontGetBaselineOffset

```csharp
public virtual double _FontGetBaselineOffset(Rid fontRid)
```

引擎名：`_font_get_baseline_offset`

Returns extra baseline offset (as a fraction of font height).

### _FontGetCharFromGlyphIndex

```csharp
public virtual long _FontGetCharFromGlyphIndex(Rid fontRid, long size, long glyphIndex)
```

引擎名：`_font_get_char_from_glyph_index`

Returns character code associated with `glyphIndex`, or `0` if `glyphIndex` is invalid.

### _FontGetDescent

```csharp
public virtual double _FontGetDescent(Rid fontRid, long size)
```

引擎名：`_font_get_descent`

Returns the font descent (number of pixels below the baseline).

### _FontGetDisableEmbeddedBitmaps

```csharp
public virtual bool _FontGetDisableEmbeddedBitmaps(Rid fontRid)
```

引擎名：`_font_get_disable_embedded_bitmaps`

Returns whether the font's embedded bitmap loading is disabled.

### _FontGetEmbolden

```csharp
public virtual double _FontGetEmbolden(Rid fontRid)
```

引擎名：`_font_get_embolden`

Returns font embolden strength.

### _FontGetFaceCount

```csharp
public virtual long _FontGetFaceCount(Rid fontRid)
```

引擎名：`_font_get_face_count`

Returns number of faces in the TrueType / OpenType collection.

### _FontGetFaceIndex

```csharp
public virtual long _FontGetFaceIndex(Rid fontRid)
```

引擎名：`_font_get_face_index`

Returns an active face index in the TrueType / OpenType collection.

### _FontGetFixedSize

```csharp
public virtual long _FontGetFixedSize(Rid fontRid)
```

引擎名：`_font_get_fixed_size`

Returns bitmap font fixed size.

### _FontGetFixedSizeScaleMode

```csharp
public virtual TextServer.FixedSizeScaleMode _FontGetFixedSizeScaleMode(Rid fontRid)
```

引擎名：`_font_get_fixed_size_scale_mode`

Returns bitmap font scaling mode.

### _FontGetGenerateMipmaps

```csharp
public virtual bool _FontGetGenerateMipmaps(Rid fontRid)
```

引擎名：`_font_get_generate_mipmaps`

Returns `true` if font texture mipmap generation is enabled.

### _FontGetGlobalOversampling

```csharp
public virtual double _FontGetGlobalOversampling()
```

引擎名：`_font_get_global_oversampling`

Returns the font oversampling factor, shared by all fonts in the TextServer.

### _FontGetGlyphAdvance

```csharp
public virtual Vector2 _FontGetGlyphAdvance(Rid fontRid, long size, long glyph)
```

引擎名：`_font_get_glyph_advance`

Returns glyph advance (offset of the next glyph).

### _FontGetGlyphContours

```csharp
public virtual Dictionary _FontGetGlyphContours(Rid fontRid, long size, long index)
```

引擎名：`_font_get_glyph_contours`

Returns outline contours of the glyph.

### _FontGetGlyphIndex

```csharp
public virtual long _FontGetGlyphIndex(Rid fontRid, long size, long char, long variationSelector)
```

引擎名：`_font_get_glyph_index`

Returns the glyph index of a `char`, optionally modified by the `variationSelector`.

### _FontGetGlyphList

```csharp
public virtual int[] _FontGetGlyphList(Rid fontRid, Vector2I size)
```

引擎名：`_font_get_glyph_list`

Returns list of rendered glyphs in the cache entry.

### _FontGetGlyphOffset

```csharp
public virtual Vector2 _FontGetGlyphOffset(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`_font_get_glyph_offset`

Returns glyph offset from the baseline.

### _FontGetGlyphSize

```csharp
public virtual Vector2 _FontGetGlyphSize(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`_font_get_glyph_size`

Returns size of the glyph.

### _FontGetGlyphTextureIdx

```csharp
public virtual long _FontGetGlyphTextureIdx(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`_font_get_glyph_texture_idx`

Returns index of the cache texture containing the glyph.

### _FontGetGlyphTextureRid

```csharp
public virtual Rid _FontGetGlyphTextureRid(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`_font_get_glyph_texture_rid`

Returns resource ID of the cache texture containing the glyph.

### _FontGetGlyphTextureSize

```csharp
public virtual Vector2 _FontGetGlyphTextureSize(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`_font_get_glyph_texture_size`

Returns size of the cache texture containing the glyph.

### _FontGetGlyphUVRect

```csharp
public virtual Rect2 _FontGetGlyphUVRect(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`_font_get_glyph_uv_rect`

Returns rectangle in the cache texture containing the glyph.

### _FontGetHinting

```csharp
public virtual TextServer.Hinting _FontGetHinting(Rid fontRid)
```

引擎名：`_font_get_hinting`

Returns the font hinting mode. Used by dynamic fonts only.

### _FontGetKeepRoundingRemainders

```csharp
public virtual bool _FontGetKeepRoundingRemainders(Rid fontRid)
```

引擎名：`_font_get_keep_rounding_remainders`

Returns glyph position rounding behavior. If set to `true`, when aligning glyphs to the pixel boundaries rounding remainders are accumulated to ensure more uniform glyph distribution. This setting has no effect if subpixel positioning is enabled.

### _FontGetKerning

```csharp
public virtual Vector2 _FontGetKerning(Rid fontRid, long size, Vector2I glyphPair)
```

引擎名：`_font_get_kerning`

Returns kerning for the pair of glyphs.

### _FontGetKerningList

```csharp
public virtual Array<Vector2I> _FontGetKerningList(Rid fontRid, long size)
```

引擎名：`_font_get_kerning_list`

Returns list of the kerning overrides.

### _FontGetLanguageSupportOverride

```csharp
public virtual bool _FontGetLanguageSupportOverride(Rid fontRid, string language)
```

引擎名：`_font_get_language_support_override`

Returns `true` if support override is enabled for the `language`.

### _FontGetLanguageSupportOverrides

```csharp
public virtual string[] _FontGetLanguageSupportOverrides(Rid fontRid)
```

引擎名：`_font_get_language_support_overrides`

Returns list of language support overrides.

### _FontGetMsdfPixelRange

```csharp
public virtual long _FontGetMsdfPixelRange(Rid fontRid)
```

引擎名：`_font_get_msdf_pixel_range`

Returns the width of the range around the shape between the minimum and maximum representable signed distance.

### _FontGetMsdfSize

```csharp
public virtual long _FontGetMsdfSize(Rid fontRid)
```

引擎名：`_font_get_msdf_size`

Returns source font size used to generate MSDF textures.

### _FontGetName

```csharp
public virtual string _FontGetName(Rid fontRid)
```

引擎名：`_font_get_name`

Returns font family name.

### _FontGetOpentypeFeatureOverrides

```csharp
public virtual Dictionary _FontGetOpentypeFeatureOverrides(Rid fontRid)
```

引擎名：`_font_get_opentype_feature_overrides`

Returns font OpenType feature set override.

### _FontGetOtNameStrings

```csharp
public virtual Dictionary _FontGetOtNameStrings(Rid fontRid)
```

引擎名：`_font_get_ot_name_strings`

Returns [`Collections.Dictionary`](Collections.Dictionary.md) with OpenType font name strings (localized font names, version, description, license information, sample text, etc.).

### _FontGetOversampling

```csharp
public virtual double _FontGetOversampling(Rid fontRid)
```

引擎名：`_font_get_oversampling`

Returns oversampling factor override. If set to a positive value, overrides the oversampling factor of the viewport this font is used in. See [`Viewport.Oversampling`](Viewport.md). This value doesn't override the `oversampling` parameter of `draw_*` methods. Used by dynamic fonts only.

### _FontGetPaletteColors

```csharp
public virtual Color[] _FontGetPaletteColors(Rid fontRid, long index)
```

引擎名：`_font_get_palette_colors`

Returns the array in the predefined color palette at `index`. Palette contains all colors used to render font glyphs. Each palette has the same number of colors. Colors can be overridden using `_FontSetPaletteCustomColors()`.

### _FontGetPaletteCount

```csharp
public virtual long _FontGetPaletteCount(Rid fontRid)
```

引擎名：`_font_get_palette_count`

Returns the number of predefined color palettes. Palette contains all colors used to render font glyphs. Each palette has the same number of colors.

### _FontGetPaletteCustomColors

```csharp
public virtual Color[] _FontGetPaletteCustomColors(Rid fontRid)
```

引擎名：`_font_get_palette_custom_colors`

Returns array of custom colors to override predefined palette.

### _FontGetPaletteName

```csharp
public virtual string _FontGetPaletteName(Rid fontRid, long index)
```

引擎名：`_font_get_palette_name`

Returns the name of the predefined color palette at `index`. Palette contains all colors used to render font glyphs. Each palette has the same number of colors.

### _FontGetScale

```csharp
public virtual double _FontGetScale(Rid fontRid, long size)
```

引擎名：`_font_get_scale`

Returns scaling factor of the color bitmap font.

### _FontGetScriptSupportOverride

```csharp
public virtual bool _FontGetScriptSupportOverride(Rid fontRid, string script)
```

引擎名：`_font_get_script_support_override`

Returns `true` if support override is enabled for the `script`.

### _FontGetScriptSupportOverrides

```csharp
public virtual string[] _FontGetScriptSupportOverrides(Rid fontRid)
```

引擎名：`_font_get_script_support_overrides`

Returns list of script support overrides.

### _FontGetSizeCacheInfo

```csharp
public virtual Array<Dictionary> _FontGetSizeCacheInfo(Rid fontRid)
```

引擎名：`_font_get_size_cache_info`

Returns font cache information, each entry contains the following fields: `Vector2i size_px` - font size in pixels, `float viewport_oversampling` - viewport oversampling factor, `int glyphs` - number of rendered glyphs, `int textures` - number of used textures, `int textures_size` - size of texture data in bytes.

### _FontGetSizeCacheList

```csharp
public virtual Array<Vector2I> _FontGetSizeCacheList(Rid fontRid)
```

引擎名：`_font_get_size_cache_list`

Returns list of the font sizes in the cache. Each size is [`Vector2I`](Vector2I.md) with font size and outline size.

### _FontGetSpacing

```csharp
public virtual long _FontGetSpacing(Rid fontRid, TextServer.SpacingType spacing)
```

引擎名：`_font_get_spacing`

Returns the spacing for `spacing` in pixels (not relative to the font size).

### _FontGetStretch

```csharp
public virtual long _FontGetStretch(Rid fontRid)
```

引擎名：`_font_get_stretch`

Returns font stretch amount, compared to a normal width. A percentage value between `50%` and `200%`.

### _FontGetStyle

```csharp
public virtual TextServer.FontStyle _FontGetStyle(Rid fontRid)
```

引擎名：`_font_get_style`

Returns font style flags.

### _FontGetStyleName

```csharp
public virtual string _FontGetStyleName(Rid fontRid)
```

引擎名：`_font_get_style_name`

Returns font style name.

### _FontGetSubpixelPositioning

```csharp
public virtual TextServer.SubpixelPositioning _FontGetSubpixelPositioning(Rid fontRid)
```

引擎名：`_font_get_subpixel_positioning`

Returns font subpixel glyph positioning mode.

### _FontGetSupportedChars

```csharp
public virtual string _FontGetSupportedChars(Rid fontRid)
```

引擎名：`_font_get_supported_chars`

Returns a string containing all the characters available in the font.

### _FontGetSupportedGlyphs

```csharp
public virtual int[] _FontGetSupportedGlyphs(Rid fontRid)
```

引擎名：`_font_get_supported_glyphs`

Returns an array containing all glyph indices in the font.

### _FontGetTextureCount

```csharp
public virtual long _FontGetTextureCount(Rid fontRid, Vector2I size)
```

引擎名：`_font_get_texture_count`

Returns number of textures used by font cache entry.

### _FontGetTextureImage

```csharp
public virtual Image _FontGetTextureImage(Rid fontRid, Vector2I size, long textureIndex)
```

引擎名：`_font_get_texture_image`

Returns font cache texture image data.

### _FontGetTextureOffsets

```csharp
public virtual int[] _FontGetTextureOffsets(Rid fontRid, Vector2I size, long textureIndex)
```

引擎名：`_font_get_texture_offsets`

Returns array containing glyph packing data.

### _FontGetTransform

```csharp
public virtual Transform2D _FontGetTransform(Rid fontRid)
```

引擎名：`_font_get_transform`

Returns 2D transform applied to the font outlines.

### _FontGetUnderlinePosition

```csharp
public virtual double _FontGetUnderlinePosition(Rid fontRid, long size)
```

引擎名：`_font_get_underline_position`

Returns pixel offset of the underline below the baseline.

### _FontGetUnderlineThickness

```csharp
public virtual double _FontGetUnderlineThickness(Rid fontRid, long size)
```

引擎名：`_font_get_underline_thickness`

Returns thickness of the underline in pixels.

### _FontGetUsedPalette

```csharp
public virtual long _FontGetUsedPalette(Rid fontRid)
```

引擎名：`_font_get_used_palette`

Returns used palette index.

### _FontGetVariationCoordinates

```csharp
public virtual Dictionary _FontGetVariationCoordinates(Rid fontRid)
```

引擎名：`_font_get_variation_coordinates`

Returns variation coordinates for the specified font cache entry.

### _FontGetWeight

```csharp
public virtual long _FontGetWeight(Rid fontRid)
```

引擎名：`_font_get_weight`

Returns weight (boldness) of the font. A value in the `100...999` range, normal font weight is `400`, bold font weight is `700`.

### _FontHasChar

```csharp
public virtual bool _FontHasChar(Rid fontRid, long char)
```

引擎名：`_font_has_char`

Returns `true` if a Unicode `char` is available in the font.

### _FontIsAllowSystemFallback

```csharp
public virtual bool _FontIsAllowSystemFallback(Rid fontRid)
```

引擎名：`_font_is_allow_system_fallback`

Returns `true` if system fonts can be automatically used as fallbacks.

### _FontIsForceAutohinter

```csharp
public virtual bool _FontIsForceAutohinter(Rid fontRid)
```

引擎名：`_font_is_force_autohinter`

Returns `true` if auto-hinting is supported and preferred over font built-in hinting.

### _FontIsLanguageSupported

```csharp
public virtual bool _FontIsLanguageSupported(Rid fontRid, string language)
```

引擎名：`_font_is_language_supported`

Returns `true` if the font supports the given language (as a [ISO 639](https://en.wikipedia.org/wiki/ISO_639-1) code).

### _FontIsModulateColorGlyphs

```csharp
public virtual bool _FontIsModulateColorGlyphs(Rid fontRid)
```

引擎名：`_font_is_modulate_color_glyphs`

Returns `true` if color modulation is applied when drawing the font's colored glyphs.

### _FontIsMultichannelSignedDistanceField

```csharp
public virtual bool _FontIsMultichannelSignedDistanceField(Rid fontRid)
```

引擎名：`_font_is_multichannel_signed_distance_field`

Returns `true` if glyphs of all sizes are rendered using single multichannel signed distance field generated from the dynamic font vector data.

### _FontIsScriptSupported

```csharp
public virtual bool _FontIsScriptSupported(Rid fontRid, string script)
```

引擎名：`_font_is_script_supported`

Returns `true` if the font supports the given script (as a [ISO 15924](https://en.wikipedia.org/wiki/ISO_15924) code).

### _FontRemoveGlyph

```csharp
public virtual void _FontRemoveGlyph(Rid fontRid, Vector2I size, long glyph)
```

引擎名：`_font_remove_glyph`

Removes specified rendered glyph information from the cache entry.

### _FontRemoveKerning

```csharp
public virtual void _FontRemoveKerning(Rid fontRid, long size, Vector2I glyphPair)
```

引擎名：`_font_remove_kerning`

Removes kerning override for the pair of glyphs.

### _FontRemoveLanguageSupportOverride

```csharp
public virtual void _FontRemoveLanguageSupportOverride(Rid fontRid, string language)
```

引擎名：`_font_remove_language_support_override`

Remove language support override.

### _FontRemoveScriptSupportOverride

```csharp
public virtual void _FontRemoveScriptSupportOverride(Rid fontRid, string script)
```

引擎名：`_font_remove_script_support_override`

Removes script support override.

### _FontRemoveSizeCache

```csharp
public virtual void _FontRemoveSizeCache(Rid fontRid, Vector2I size)
```

引擎名：`_font_remove_size_cache`

Removes specified font size from the cache entry.

### _FontRemoveTexture

```csharp
public virtual void _FontRemoveTexture(Rid fontRid, Vector2I size, long textureIndex)
```

引擎名：`_font_remove_texture`

Removes specified texture from the cache entry.

### _FontRenderGlyph

```csharp
public virtual void _FontRenderGlyph(Rid fontRid, Vector2I size, long index)
```

引擎名：`_font_render_glyph`

Renders specified glyph to the font cache texture.

### _FontRenderRange

```csharp
public virtual void _FontRenderRange(Rid fontRid, Vector2I size, long start, long end)
```

引擎名：`_font_render_range`

Renders the range of characters to the font cache texture.

### _FontSetAllowSystemFallback

```csharp
public virtual void _FontSetAllowSystemFallback(Rid fontRid, bool allowSystemFallback)
```

引擎名：`_font_set_allow_system_fallback`

If set to `true`, system fonts can be automatically used as fallbacks.

### _FontSetAntialiasing

```csharp
public virtual void _FontSetAntialiasing(Rid fontRid, TextServer.FontAntialiasing antialiasing)
```

引擎名：`_font_set_antialiasing`

Sets font anti-aliasing mode.

### _FontSetAscent

```csharp
public virtual void _FontSetAscent(Rid fontRid, long size, double ascent)
```

引擎名：`_font_set_ascent`

Sets the font ascent (number of pixels above the baseline).

### _FontSetBaselineOffset

```csharp
public virtual void _FontSetBaselineOffset(Rid fontRid, double baselineOffset)
```

引擎名：`_font_set_baseline_offset`

Sets extra baseline offset (as a fraction of font height).

### _FontSetData

```csharp
public virtual void _FontSetData(Rid fontRid, byte[] data)
```

引擎名：`_font_set_data`

Sets font source data, e.g contents of the dynamic font source file.

### _FontSetDescent

```csharp
public virtual void _FontSetDescent(Rid fontRid, long size, double descent)
```

引擎名：`_font_set_descent`

Sets the font descent (number of pixels below the baseline).

### _FontSetDisableEmbeddedBitmaps

```csharp
public virtual void _FontSetDisableEmbeddedBitmaps(Rid fontRid, bool disableEmbeddedBitmaps)
```

引擎名：`_font_set_disable_embedded_bitmaps`

If set to `true`, embedded font bitmap loading is disabled.

### _FontSetEmbolden

```csharp
public virtual void _FontSetEmbolden(Rid fontRid, double strength)
```

引擎名：`_font_set_embolden`

Sets font embolden strength. If `strength` is not equal to zero, emboldens the font outlines. Negative values reduce the outline thickness.

### _FontSetFaceIndex

```csharp
public virtual void _FontSetFaceIndex(Rid fontRid, long faceIndex)
```

引擎名：`_font_set_face_index`

Sets an active face index in the TrueType / OpenType collection.

### _FontSetFixedSize

```csharp
public virtual void _FontSetFixedSize(Rid fontRid, long fixedSize)
```

引擎名：`_font_set_fixed_size`

Sets bitmap font fixed size. If set to value greater than zero, same cache entry will be used for all font sizes.

### _FontSetFixedSizeScaleMode

```csharp
public virtual void _FontSetFixedSizeScaleMode(Rid fontRid, TextServer.FixedSizeScaleMode fixedSizeScaleMode)
```

引擎名：`_font_set_fixed_size_scale_mode`

Sets bitmap font scaling mode. This property is used only if `fixed_size` is greater than zero.

### _FontSetForceAutohinter

```csharp
public virtual void _FontSetForceAutohinter(Rid fontRid, bool forceAutohinter)
```

引擎名：`_font_set_force_autohinter`

If set to `true` auto-hinting is preferred over font built-in hinting.

### _FontSetGenerateMipmaps

```csharp
public virtual void _FontSetGenerateMipmaps(Rid fontRid, bool generateMipmaps)
```

引擎名：`_font_set_generate_mipmaps`

If set to `true` font texture mipmap generation is enabled.

### _FontSetGlobalOversampling

```csharp
public virtual void _FontSetGlobalOversampling(double oversampling)
```

引擎名：`_font_set_global_oversampling`

Sets oversampling factor, shared by all font in the TextServer.

### _FontSetGlyphAdvance

```csharp
public virtual void _FontSetGlyphAdvance(Rid fontRid, long size, long glyph, Vector2 advance)
```

引擎名：`_font_set_glyph_advance`

Sets glyph advance (offset of the next glyph).

### _FontSetGlyphOffset

```csharp
public virtual void _FontSetGlyphOffset(Rid fontRid, Vector2I size, long glyph, Vector2 offset)
```

引擎名：`_font_set_glyph_offset`

Sets glyph offset from the baseline.

### _FontSetGlyphSize

```csharp
public virtual void _FontSetGlyphSize(Rid fontRid, Vector2I size, long glyph, Vector2 glSize)
```

引擎名：`_font_set_glyph_size`

Sets size of the glyph.

### _FontSetGlyphTextureIdx

```csharp
public virtual void _FontSetGlyphTextureIdx(Rid fontRid, Vector2I size, long glyph, long textureIdx)
```

引擎名：`_font_set_glyph_texture_idx`

Sets index of the cache texture containing the glyph.

### _FontSetGlyphUVRect

```csharp
public virtual void _FontSetGlyphUVRect(Rid fontRid, Vector2I size, long glyph, Rect2 uVRect)
```

引擎名：`_font_set_glyph_uv_rect`

Sets rectangle in the cache texture containing the glyph.

### _FontSetHinting

```csharp
public virtual void _FontSetHinting(Rid fontRid, TextServer.Hinting hinting)
```

引擎名：`_font_set_hinting`

Sets font hinting mode. Used by dynamic fonts only.

### _FontSetKeepRoundingRemainders

```csharp
public virtual void _FontSetKeepRoundingRemainders(Rid fontRid, bool keepRoundingRemainders)
```

引擎名：`_font_set_keep_rounding_remainders`

Sets glyph position rounding behavior. If set to `true`, when aligning glyphs to the pixel boundaries rounding remainders are accumulated to ensure more uniform glyph distribution. This setting has no effect if subpixel positioning is enabled.

### _FontSetKerning

```csharp
public virtual void _FontSetKerning(Rid fontRid, long size, Vector2I glyphPair, Vector2 kerning)
```

引擎名：`_font_set_kerning`

Sets kerning for the pair of glyphs.

### _FontSetLanguageSupportOverride

```csharp
public virtual void _FontSetLanguageSupportOverride(Rid fontRid, string language, bool supported)
```

引擎名：`_font_set_language_support_override`

Adds override for `_FontIsLanguageSupported()`.

### _FontSetModulateColorGlyphs

```csharp
public virtual void _FontSetModulateColorGlyphs(Rid fontRid, bool modulate)
```

引擎名：`_font_set_modulate_color_glyphs`

If set to `true`, color modulation is applied when drawing colored glyphs, otherwise it's applied to the monochrome glyphs only.

### _FontSetMsdfPixelRange

```csharp
public virtual void _FontSetMsdfPixelRange(Rid fontRid, long msdfPixelRange)
```

引擎名：`_font_set_msdf_pixel_range`

Sets the width of the range around the shape between the minimum and maximum representable signed distance.

### _FontSetMsdfSize

```csharp
public virtual void _FontSetMsdfSize(Rid fontRid, long msdfSize)
```

引擎名：`_font_set_msdf_size`

Sets source font size used to generate MSDF textures.

### _FontSetMultichannelSignedDistanceField

```csharp
public virtual void _FontSetMultichannelSignedDistanceField(Rid fontRid, bool msdf)
```

引擎名：`_font_set_multichannel_signed_distance_field`

If set to `true`, glyphs of all sizes are rendered using single multichannel signed distance field generated from the dynamic font vector data. MSDF rendering allows displaying the font at any scaling factor without blurriness, and without incurring a CPU cost when the font size changes (since the font no longer needs to be rasterized on the CPU). As a downside, font hinting is not available with MSDF. The lack of font hinting may result in less crisp and less readable fonts at small sizes.

### _FontSetName

```csharp
public virtual void _FontSetName(Rid fontRid, string name)
```

引擎名：`_font_set_name`

Sets the font family name.

### _FontSetOpentypeFeatureOverrides

```csharp
public virtual void _FontSetOpentypeFeatureOverrides(Rid fontRid, Dictionary overrides)
```

引擎名：`_font_set_opentype_feature_overrides`

Sets font OpenType feature set override.

### _FontSetOversampling

```csharp
public virtual void _FontSetOversampling(Rid fontRid, double oversampling)
```

引擎名：`_font_set_oversampling`

If set to a positive value, overrides the oversampling factor of the viewport this font is used in. See [`Viewport.Oversampling`](Viewport.md). This value doesn't override the `oversampling` parameter of `draw_*` methods. Used by dynamic fonts only.

### _FontSetPaletteCustomColors

```csharp
public virtual void _FontSetPaletteCustomColors(Rid fontRid, Color[] colors)
```

引擎名：`_font_set_palette_custom_colors`

Sets array of custom colors to override predefined palette. Set to empty array to reset overrides. Use `Color(0, 0, 0, 0)`, to keep predefined palette color at specific position.

### _FontSetScale

```csharp
public virtual void _FontSetScale(Rid fontRid, long size, double scale)
```

引擎名：`_font_set_scale`

Sets scaling factor of the color bitmap font.

### _FontSetScriptSupportOverride

```csharp
public virtual void _FontSetScriptSupportOverride(Rid fontRid, string script, bool supported)
```

引擎名：`_font_set_script_support_override`

Adds override for `_FontIsScriptSupported()`.

### _FontSetSpacing

```csharp
public virtual void _FontSetSpacing(Rid fontRid, TextServer.SpacingType spacing, long value)
```

引擎名：`_font_set_spacing`

Sets the spacing for `spacing` to `value` in pixels (not relative to the font size).

### _FontSetStretch

```csharp
public virtual void _FontSetStretch(Rid fontRid, long stretch)
```

引擎名：`_font_set_stretch`

Sets font stretch amount, compared to a normal width. A percentage value between `50%` and `200%`.

### _FontSetStyle

```csharp
public virtual void _FontSetStyle(Rid fontRid, TextServer.FontStyle style)
```

引擎名：`_font_set_style`

Sets the font style flags.

### _FontSetStyleName

```csharp
public virtual void _FontSetStyleName(Rid fontRid, string nameStyle)
```

引擎名：`_font_set_style_name`

Sets the font style name.

### _FontSetSubpixelPositioning

```csharp
public virtual void _FontSetSubpixelPositioning(Rid fontRid, TextServer.SubpixelPositioning subpixelPositioning)
```

引擎名：`_font_set_subpixel_positioning`

Sets font subpixel glyph positioning mode.

### _FontSetTextureImage

```csharp
public virtual void _FontSetTextureImage(Rid fontRid, Vector2I size, long textureIndex, Image image)
```

引擎名：`_font_set_texture_image`

Sets font cache texture image data.

### _FontSetTextureOffsets

```csharp
public virtual void _FontSetTextureOffsets(Rid fontRid, Vector2I size, long textureIndex, int[] offset)
```

引擎名：`_font_set_texture_offsets`

Sets array containing glyph packing data.

### _FontSetTransform

```csharp
public virtual void _FontSetTransform(Rid fontRid, Transform2D transform)
```

引擎名：`_font_set_transform`

Sets 2D transform, applied to the font outlines, can be used for slanting, flipping, and rotating glyphs.

### _FontSetUnderlinePosition

```csharp
public virtual void _FontSetUnderlinePosition(Rid fontRid, long size, double underlinePosition)
```

引擎名：`_font_set_underline_position`

Sets pixel offset of the underline below the baseline.

### _FontSetUnderlineThickness

```csharp
public virtual void _FontSetUnderlineThickness(Rid fontRid, long size, double underlineThickness)
```

引擎名：`_font_set_underline_thickness`

Sets thickness of the underline in pixels.

### _FontSetUsedPalette

```csharp
public virtual void _FontSetUsedPalette(Rid fontRid, long index)
```

引擎名：`_font_set_used_palette`

Sets used palette index.

### _FontSetVariationCoordinates

```csharp
public virtual void _FontSetVariationCoordinates(Rid fontRid, Dictionary variationCoordinates)
```

引擎名：`_font_set_variation_coordinates`

Sets variation coordinates for the specified font cache entry.

### _FontSetWeight

```csharp
public virtual void _FontSetWeight(Rid fontRid, long weight)
```

引擎名：`_font_set_weight`

Sets weight (boldness) of the font. A value in the `100...999` range, normal font weight is `400`, bold font weight is `700`.

### _FontSupportedFeatureList

```csharp
public virtual Dictionary _FontSupportedFeatureList(Rid fontRid)
```

引擎名：`_font_supported_feature_list`

Returns the dictionary of the supported OpenType features.

### _FontSupportedVariationList

```csharp
public virtual Dictionary _FontSupportedVariationList(Rid fontRid)
```

引擎名：`_font_supported_variation_list`

Returns the dictionary of the supported OpenType variation coordinates.

### _FormatNumber

```csharp
public virtual string _FormatNumber(string number, string language)
```

引擎名：`_format_number`

Converts a number from Western Arabic (0..9) to the numeral system used in the given `language`.

If `language` is an empty string, the active locale will be used.

### _FreeRid

```csharp
public virtual void _FreeRid(Rid rid)
```

引擎名：`_free_rid`

Frees an object created by this [`TextServer`](TextServer.md).

### _GetFeatures

```csharp
public virtual long _GetFeatures()
```

引擎名：`_get_features`

Returns text server features, see [`TextServer.Feature`](TextServer.md).

### _GetHexCodeBoxSize

```csharp
public virtual Vector2 _GetHexCodeBoxSize(long size, long index)
```

引擎名：`_get_hex_code_box_size`

Returns size of the replacement character (box with character hexadecimal code that is drawn in place of invalid characters).

### _GetName

```csharp
public virtual string _GetName()
```

引擎名：`_get_name`

Returns the name of the server interface.

### _GetSupportData

```csharp
public virtual byte[] _GetSupportData()
```

引擎名：`_get_support_data`

Returns default TextServer database (e.g. ICU break iterators and dictionaries).

### _GetSupportDataFileName

```csharp
public virtual string _GetSupportDataFileName()
```

Returns default TextServer database (e.g. ICU break iterators and dictionaries) filename.

### _GetSupportDataInfo

```csharp
public virtual string _GetSupportDataInfo()
```

引擎名：`_get_support_data_info`

Returns TextServer database (e.g. ICU break iterators and dictionaries) description.

### _Has

```csharp
public virtual bool _Has(Rid rid)
```

引擎名：`_has`

Returns `true` if `rid` is valid resource owned by this text server.

### _HasFeature

```csharp
public virtual bool _HasFeature(TextServer.Feature feature)
```

引擎名：`_has_feature`

Returns `true` if the server supports a feature.

### _IsConfusable

```csharp
public virtual long _IsConfusable(string string, string[] dict)
```

引擎名：`_is_confusable`

Returns index of the first string in `dict` which is visually confusable with the `string`, or `-1` if none is found.

### _IsLocaleRightToLeft

```csharp
public virtual bool _IsLocaleRightToLeft(string locale)
```

引擎名：`_is_locale_right_to_left`

Returns `true` if locale is right-to-left.

### _IsLocaleUsingSupportData

```csharp
public virtual bool _IsLocaleUsingSupportData(string locale)
```

引擎名：`_is_locale_using_support_data`

Returns `true` if the locale requires text server support data for line/word breaking.

### _IsValidIdentifier

```csharp
public virtual bool _IsValidIdentifier(string string)
```

引擎名：`_is_valid_identifier`

Returns `true` if `string` is a valid identifier.

### _IsValidLetter

```csharp
public virtual bool _IsValidLetter(ulong unicode)
```

引擎名：`_is_valid_letter`

### _LoadSupportData

```csharp
public virtual bool _LoadSupportData(string fileName)
```

引擎名：`_load_support_data`

Loads optional TextServer database (e.g. ICU break iterators and dictionaries).

### _NameToTag

```csharp
public virtual long _NameToTag(string name)
```

引擎名：`_name_to_tag`

Converts the given readable name of a feature, variation, script, or language to an OpenType tag.

### _ParseNumber

```csharp
public virtual string _ParseNumber(string number, string language)
```

引擎名：`_parse_number`

Converts `number` from the numeral system used in the given `language` to Western Arabic (0..9).

If `language` is an empty string, the active locale will be used.

### _ParseStructuredText

```csharp
public virtual Array<Vector3I> _ParseStructuredText(TextServer.StructuredTextParser parserType, Array args, string text)
```

引擎名：`_parse_structured_text`

Default implementation of the BiDi algorithm override function.

### _PercentSign

```csharp
public virtual string _PercentSign(string language)
```

引擎名：`_percent_sign`

Returns percent sign used in the given `language`.

### _ReferenceOversamplingLevel

```csharp
public virtual void _ReferenceOversamplingLevel(double oversampling)
```

引擎名：`_reference_oversampling_level`

Increases the reference count of the specified oversampling level. This method is called by [`Viewport`](Viewport.md), and should not be used directly.

### _SaveSupportData

```csharp
public virtual bool _SaveSupportData(string fileName)
```

引擎名：`_save_support_data`

Saves optional TextServer database (e.g. ICU break iterators and dictionaries) to the file.

### _ShapedGetRunCount

```csharp
public virtual long _ShapedGetRunCount(Rid shaped)
```

引擎名：`_shaped_get_run_count`

Returns the number of uniform text runs in the buffer.

### _ShapedGetRunDirection

```csharp
public virtual TextServer.Direction _ShapedGetRunDirection(Rid shaped, long index)
```

引擎名：`_shaped_get_run_direction`

Returns the direction of the `index` text run (in visual order).

### _ShapedGetRunFontRid

```csharp
public virtual Rid _ShapedGetRunFontRid(Rid shaped, long index)
```

引擎名：`_shaped_get_run_font_rid`

Returns the font RID of the `index` text run (in visual order).

### _ShapedGetRunFontSize

```csharp
public virtual int _ShapedGetRunFontSize(Rid shaped, long index)
```

引擎名：`_shaped_get_run_font_size`

Returns the font size of the `index` text run (in visual order).

### _ShapedGetRunGlyphRange

```csharp
public virtual Vector2I _ShapedGetRunGlyphRange(Rid shaped, long index)
```

引擎名：`_shaped_get_run_glyph_range`

Returns the glyph range of the `index` text run (in visual order).

### _ShapedGetRunLanguage

```csharp
public virtual string _ShapedGetRunLanguage(Rid shaped, long index)
```

引擎名：`_shaped_get_run_language`

Returns the language of the `index` text run (in visual order).

### _ShapedGetRunObject

```csharp
public virtual Variant _ShapedGetRunObject(Rid shaped, long index)
```

引擎名：`_shaped_get_run_object`

Returns the embedded object of the `index` text run (in visual order).

### _ShapedGetRunRange

```csharp
public virtual Vector2I _ShapedGetRunRange(Rid shaped, long index)
```

引擎名：`_shaped_get_run_range`

Returns the source text range of the `index` text run (in visual order).

### _ShapedGetRunText

```csharp
public virtual string _ShapedGetRunText(Rid shaped, long index)
```

引擎名：`_shaped_get_run_text`

Returns the source text of the `index` text run (in visual order).

### _ShapedGetSpanCount

```csharp
public virtual long _ShapedGetSpanCount(Rid shaped)
```

引擎名：`_shaped_get_span_count`

Returns number of text spans added using `_ShapedTextAddString()` or `_ShapedTextAddObject()`.

### _ShapedGetSpanEmbeddedObject

```csharp
public virtual Variant _ShapedGetSpanEmbeddedObject(Rid shaped, long index)
```

引擎名：`_shaped_get_span_embedded_object`

Returns text embedded object key.

### _ShapedGetSpanMeta

```csharp
public virtual Variant _ShapedGetSpanMeta(Rid shaped, long index)
```

引擎名：`_shaped_get_span_meta`

Returns text span metadata.

### _ShapedGetSpanObject

```csharp
public virtual Variant _ShapedGetSpanObject(Rid shaped, long index)
```

引擎名：`_shaped_get_span_object`

Returns the text span embedded object key.

### _ShapedGetSpanText

```csharp
public virtual string _ShapedGetSpanText(Rid shaped, long index)
```

引擎名：`_shaped_get_span_text`

Returns the text span source text.

### _ShapedGetText

```csharp
public virtual string _ShapedGetText(Rid shaped)
```

引擎名：`_shaped_get_text`

Returns the text buffer source text, including object replacement characters.

### _ShapedSetSpanUpdateFont

```csharp
public virtual void _ShapedSetSpanUpdateFont(Rid shaped, long index, Array<Rid> fonts, long size, Dictionary opentypeFeatures)
```

引擎名：`_shaped_set_span_update_font`

Changes text span font, font size, and OpenType features, without changing the text.

### _ShapedTextAddObject

```csharp
public virtual bool _ShapedTextAddObject(Rid shaped, Variant key, Vector2 size, InlineAlignment inlineAlign, long length, double baseline)
```

引擎名：`_shaped_text_add_object`

Adds inline object to the text buffer, `key` must be unique. In the text, object is represented as `length` object replacement characters.

### _ShapedTextAddString

```csharp
public virtual bool _ShapedTextAddString(Rid shaped, string text, Array<Rid> fonts, long size, Dictionary opentypeFeatures, string language, Variant meta)
```

引擎名：`_shaped_text_add_string`

Adds text span and font to draw it to the text buffer.

### _ShapedTextClear

```csharp
public virtual void _ShapedTextClear(Rid shaped)
```

引擎名：`_shaped_text_clear`

Clears text buffer (removes text and inline objects).

### _ShapedTextClosestCharacterPos

```csharp
public virtual long _ShapedTextClosestCharacterPos(Rid shaped, long pos)
```

引擎名：`_shaped_text_closest_character_pos`

Returns composite character position closest to the `pos`.

### _ShapedTextDraw

```csharp
public virtual void _ShapedTextDraw(Rid shaped, Rid canvas, Vector2 pos, double clipL, double clipR, Color color, float oversampling)
```

引擎名：`_shaped_text_draw`

Draw shaped text into a canvas item at a given position, with `color`. `pos` specifies the leftmost point of the baseline (for horizontal layout) or topmost point of the baseline (for vertical layout). If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

### _ShapedTextDrawOutline

```csharp
public virtual void _ShapedTextDrawOutline(Rid shaped, Rid canvas, Vector2 pos, double clipL, double clipR, long outlineSize, Color color, float oversampling)
```

引擎名：`_shaped_text_draw_outline`

Draw the outline of the shaped text into a canvas item at a given position, with `color`. `pos` specifies the leftmost point of the baseline (for horizontal layout) or topmost point of the baseline (for vertical layout). If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

### _ShapedTextDuplicate

```csharp
public virtual Rid _ShapedTextDuplicate(Rid shaped)
```

引擎名：`_shaped_text_duplicate`

Duplicates shaped text buffer.

### _ShapedTextFitToWidth

```csharp
public virtual double _ShapedTextFitToWidth(Rid shaped, double width, TextServer.JustificationFlag justificationFlags)
```

引擎名：`_shaped_text_fit_to_width`

Adjusts text width to fit to specified width, returns new text width.

### _ShapedTextGetAscent

```csharp
public virtual double _ShapedTextGetAscent(Rid shaped)
```

引擎名：`_shaped_text_get_ascent`

Returns the text ascent (number of pixels above the baseline for horizontal layout or to the left of baseline for vertical).

### _ShapedTextGetCharacterBreaks

```csharp
public virtual int[] _ShapedTextGetCharacterBreaks(Rid shaped)
```

引擎名：`_shaped_text_get_character_breaks`

Returns array of the composite character boundaries.

### _ShapedTextGetCustomEllipsis

```csharp
public virtual long _ShapedTextGetCustomEllipsis(Rid shaped)
```

引擎名：`_shaped_text_get_custom_ellipsis`

Returns ellipsis character used for text clipping.

### _ShapedTextGetCustomPunctuation

```csharp
public virtual string _ShapedTextGetCustomPunctuation(Rid shaped)
```

引擎名：`_shaped_text_get_custom_punctuation`

Returns custom punctuation character list, used for word breaking. If set to empty string, server defaults are used.

### _ShapedTextGetDescent

```csharp
public virtual double _ShapedTextGetDescent(Rid shaped)
```

引擎名：`_shaped_text_get_descent`

Returns the text descent (number of pixels below the baseline for horizontal layout or to the right of baseline for vertical).

### _ShapedTextGetDirection

```csharp
public virtual TextServer.Direction _ShapedTextGetDirection(Rid shaped)
```

引擎名：`_shaped_text_get_direction`

Returns direction of the text.

### _ShapedTextGetDominantDirectionInRange

```csharp
public virtual long _ShapedTextGetDominantDirectionInRange(Rid shaped, long start, long end)
```

引擎名：`_shaped_text_get_dominant_direction_in_range`

Returns dominant direction of in the range of text.

### _ShapedTextGetEllipsisGlyphCount

```csharp
public virtual long _ShapedTextGetEllipsisGlyphCount(Rid shaped)
```

引擎名：`_shaped_text_get_ellipsis_glyph_count`

Returns number of glyphs in the ellipsis.

### _ShapedTextGetEllipsisPos

```csharp
public virtual long _ShapedTextGetEllipsisPos(Rid shaped)
```

引擎名：`_shaped_text_get_ellipsis_pos`

Returns position of the ellipsis.

### _ShapedTextGetGlyphCount

```csharp
public virtual long _ShapedTextGetGlyphCount(Rid shaped)
```

引擎名：`_shaped_text_get_glyph_count`

Returns number of glyphs in the buffer.

### _ShapedTextGetGraphemeBounds

```csharp
public virtual Vector2 _ShapedTextGetGraphemeBounds(Rid shaped, long pos)
```

引擎名：`_shaped_text_get_grapheme_bounds`

Returns composite character's bounds as offsets from the start of the line.

### _ShapedTextGetInferredDirection

```csharp
public virtual TextServer.Direction _ShapedTextGetInferredDirection(Rid shaped)
```

引擎名：`_shaped_text_get_inferred_direction`

Returns direction of the text, inferred by the BiDi algorithm.

### _ShapedTextGetLineBreaks

```csharp
public virtual int[] _ShapedTextGetLineBreaks(Rid shaped, double width, long start, TextServer.LineBreakFlag breakFlags)
```

引擎名：`_shaped_text_get_line_breaks`

Breaks text to the lines and returns character ranges for each line.

### _ShapedTextGetLineBreaksAdv

```csharp
public virtual int[] _ShapedTextGetLineBreaksAdv(Rid shaped, float[] width, long start, bool once, TextServer.LineBreakFlag breakFlags)
```

引擎名：`_shaped_text_get_line_breaks_adv`

Breaks text to the lines and columns. Returns character ranges for each segment.

### _ShapedTextGetObjectGlyph

```csharp
public virtual long _ShapedTextGetObjectGlyph(Rid shaped, Variant key)
```

引擎名：`_shaped_text_get_object_glyph`

Returns the glyph index of the inline object.

### _ShapedTextGetObjectRange

```csharp
public virtual Vector2I _ShapedTextGetObjectRange(Rid shaped, Variant key)
```

引擎名：`_shaped_text_get_object_range`

Returns the character range of the inline object.

### _ShapedTextGetObjectRect

```csharp
public virtual Rect2 _ShapedTextGetObjectRect(Rid shaped, Variant key)
```

引擎名：`_shaped_text_get_object_rect`

Returns bounding rectangle of the inline object.

### _ShapedTextGetObjects

```csharp
public virtual Array _ShapedTextGetObjects(Rid shaped)
```

引擎名：`_shaped_text_get_objects`

Returns array of inline objects.

### _ShapedTextGetOrientation

```csharp
public virtual TextServer.Orientation _ShapedTextGetOrientation(Rid shaped)
```

引擎名：`_shaped_text_get_orientation`

Returns text orientation.

### _ShapedTextGetParent

```csharp
public virtual Rid _ShapedTextGetParent(Rid shaped)
```

引擎名：`_shaped_text_get_parent`

Returns the parent buffer from which the substring originates.

### _ShapedTextGetPreserveControl

```csharp
public virtual bool _ShapedTextGetPreserveControl(Rid shaped)
```

引擎名：`_shaped_text_get_preserve_control`

Returns `true` if text buffer is configured to display control characters.

### _ShapedTextGetPreserveInvalid

```csharp
public virtual bool _ShapedTextGetPreserveInvalid(Rid shaped)
```

引擎名：`_shaped_text_get_preserve_invalid`

Returns `true` if text buffer is configured to display hexadecimal codes in place of invalid characters.

### _ShapedTextGetRange

```csharp
public virtual Vector2I _ShapedTextGetRange(Rid shaped)
```

引擎名：`_shaped_text_get_range`

Returns substring buffer character range in the parent buffer.

### _ShapedTextGetSelection

```csharp
public virtual Vector2[] _ShapedTextGetSelection(Rid shaped, long start, long end)
```

引擎名：`_shaped_text_get_selection`

Returns selection rectangles for the specified character range.

### _ShapedTextGetSize

```csharp
public virtual Vector2 _ShapedTextGetSize(Rid shaped)
```

引擎名：`_shaped_text_get_size`

Returns size of the text.

### _ShapedTextGetSpacing

```csharp
public virtual long _ShapedTextGetSpacing(Rid shaped, TextServer.SpacingType spacing)
```

引擎名：`_shaped_text_get_spacing`

Returns extra spacing added between glyphs or lines in pixels.

### _ShapedTextGetTrimPos

```csharp
public virtual long _ShapedTextGetTrimPos(Rid shaped)
```

引擎名：`_shaped_text_get_trim_pos`

Returns the position of the overrun trim.

### _ShapedTextGetUnderlinePosition

```csharp
public virtual double _ShapedTextGetUnderlinePosition(Rid shaped)
```

引擎名：`_shaped_text_get_underline_position`

Returns pixel offset of the underline below the baseline.

### _ShapedTextGetUnderlineThickness

```csharp
public virtual double _ShapedTextGetUnderlineThickness(Rid shaped)
```

引擎名：`_shaped_text_get_underline_thickness`

Returns thickness of the underline.

### _ShapedTextGetWidth

```csharp
public virtual double _ShapedTextGetWidth(Rid shaped)
```

引擎名：`_shaped_text_get_width`

Returns width (for horizontal layout) or height (for vertical) of the text.

### _ShapedTextGetWordBreaks

```csharp
public virtual int[] _ShapedTextGetWordBreaks(Rid shaped, TextServer.GraphemeFlag graphemeFlags, TextServer.GraphemeFlag skipGraphemeFlags)
```

引擎名：`_shaped_text_get_word_breaks`

Breaks text into words and returns array of character ranges. Use `graphemeFlags` to set what characters are used for breaking.

### _ShapedTextHasObject

```csharp
public virtual bool _ShapedTextHasObject(Rid shaped, Variant key)
```

引擎名：`_shaped_text_has_object`

Returns `true` if an object with `key` is embedded in this shaped text buffer.

### _ShapedTextHitTestGrapheme

```csharp
public virtual long _ShapedTextHitTestGrapheme(Rid shaped, double coord)
```

引擎名：`_shaped_text_hit_test_grapheme`

Returns grapheme index at the specified pixel offset at the baseline, or `-1` if none is found.

### _ShapedTextHitTestPosition

```csharp
public virtual long _ShapedTextHitTestPosition(Rid shaped, double coord)
```

引擎名：`_shaped_text_hit_test_position`

Returns caret character offset at the specified pixel offset at the baseline. This function always returns a valid position.

### _ShapedTextIsReady

```csharp
public virtual bool _ShapedTextIsReady(Rid shaped)
```

引擎名：`_shaped_text_is_ready`

Returns `true` if buffer is successfully shaped.

### _ShapedTextNextCharacterPos

```csharp
public virtual long _ShapedTextNextCharacterPos(Rid shaped, long pos)
```

引擎名：`_shaped_text_next_character_pos`

Returns composite character end position closest to the `pos`.

### _ShapedTextNextGraphemePos

```csharp
public virtual long _ShapedTextNextGraphemePos(Rid shaped, long pos)
```

引擎名：`_shaped_text_next_grapheme_pos`

Returns grapheme end position closest to the `pos`.

### _ShapedTextOverrunTrimToWidth

```csharp
public virtual void _ShapedTextOverrunTrimToWidth(Rid shaped, double width, TextServer.TextOverrunFlag trimFlags)
```

引擎名：`_shaped_text_overrun_trim_to_width`

Trims text if it exceeds the given width.

### _ShapedTextPrevCharacterPos

```csharp
public virtual long _ShapedTextPrevCharacterPos(Rid shaped, long pos)
```

引擎名：`_shaped_text_prev_character_pos`

Returns composite character start position closest to the `pos`.

### _ShapedTextPrevGraphemePos

```csharp
public virtual long _ShapedTextPrevGraphemePos(Rid shaped, long pos)
```

引擎名：`_shaped_text_prev_grapheme_pos`

Returns grapheme start position closest to the `pos`.

### _ShapedTextResizeObject

```csharp
public virtual bool _ShapedTextResizeObject(Rid shaped, Variant key, Vector2 size, InlineAlignment inlineAlign, double baseline)
```

引擎名：`_shaped_text_resize_object`

Sets new size and alignment of embedded object.

### _ShapedTextSetBidiOverride

```csharp
public virtual void _ShapedTextSetBidiOverride(Rid shaped, Array override)
```

引擎名：`_shaped_text_set_bidi_override`

Overrides BiDi for the structured text.

### _ShapedTextSetCustomEllipsis

```csharp
public virtual void _ShapedTextSetCustomEllipsis(Rid shaped, long char)
```

引擎名：`_shaped_text_set_custom_ellipsis`

Sets ellipsis character used for text clipping.

### _ShapedTextSetCustomPunctuation

```csharp
public virtual void _ShapedTextSetCustomPunctuation(Rid shaped, string punct)
```

引擎名：`_shaped_text_set_custom_punctuation`

Sets custom punctuation character list, used for word breaking. If set to empty string, server defaults are used.

### _ShapedTextSetDirection

```csharp
public virtual void _ShapedTextSetDirection(Rid shaped, TextServer.Direction direction)
```

引擎名：`_shaped_text_set_direction`

Sets desired text direction. If set to [`TextServer.Direction.Auto`](TextServer.md), direction will be detected based on the buffer contents and current locale.

### _ShapedTextSetOrientation

```csharp
public virtual void _ShapedTextSetOrientation(Rid shaped, TextServer.Orientation orientation)
```

引擎名：`_shaped_text_set_orientation`

Sets desired text orientation.

### _ShapedTextSetPreserveControl

```csharp
public virtual void _ShapedTextSetPreserveControl(Rid shaped, bool enabled)
```

引擎名：`_shaped_text_set_preserve_control`

If set to `true` text buffer will display control characters.

### _ShapedTextSetPreserveInvalid

```csharp
public virtual void _ShapedTextSetPreserveInvalid(Rid shaped, bool enabled)
```

引擎名：`_shaped_text_set_preserve_invalid`

If set to `true` text buffer will display invalid characters as hexadecimal codes, otherwise nothing is displayed.

### _ShapedTextSetSpacing

```csharp
public virtual void _ShapedTextSetSpacing(Rid shaped, TextServer.SpacingType spacing, long value)
```

引擎名：`_shaped_text_set_spacing`

Sets extra spacing added between glyphs or lines in pixels.

### _ShapedTextShape

```csharp
public virtual bool _ShapedTextShape(Rid shaped)
```

引擎名：`_shaped_text_shape`

Shapes buffer if it's not shaped. Returns `true` if the string is shaped successfully.

### _ShapedTextSubstr

```csharp
public virtual Rid _ShapedTextSubstr(Rid shaped, long start, long length)
```

引擎名：`_shaped_text_substr`

Returns text buffer for the substring of the text in the `shaped` text buffer (including inline objects).

### _ShapedTextTabAlign

```csharp
public virtual double _ShapedTextTabAlign(Rid shaped, float[] tabStops)
```

引擎名：`_shaped_text_tab_align`

Aligns shaped text to the given tab-stops.

### _ShapedTextUpdateBreaks

```csharp
public virtual bool _ShapedTextUpdateBreaks(Rid shaped)
```

引擎名：`_shaped_text_update_breaks`

Updates break points in the shaped text. This method is called by default implementation of text breaking functions.

### _ShapedTextUpdateJustificationOps

```csharp
public virtual bool _ShapedTextUpdateJustificationOps(Rid shaped)
```

引擎名：`_shaped_text_update_justification_ops`

Updates justification points in the shaped text. This method is called by default implementation of text justification functions.

### _SpoofCheck

```csharp
public virtual bool _SpoofCheck(string string)
```

引擎名：`_spoof_check`

Returns `true` if `string` is likely to be an attempt at confusing the reader.

### _StringGetCharacterBreaks

```csharp
public virtual int[] _StringGetCharacterBreaks(string string, string language)
```

引擎名：`_string_get_character_breaks`

Returns array of the composite character boundaries.

### _StringGetWordBreaks

```csharp
public virtual int[] _StringGetWordBreaks(string string, string language, long charsPerLine)
```

引擎名：`_string_get_word_breaks`

Returns an array of the word break boundaries. Elements in the returned array are the offsets of the start and end of words. Therefore the length of the array is always even.

### _StringToLower

```csharp
public virtual string _StringToLower(string string, string language)
```

引擎名：`_string_to_lower`

Returns the string converted to `lowercase`.

### _StringToTitle

```csharp
public virtual string _StringToTitle(string string, string language)
```

引擎名：`_string_to_title`

Returns the string converted to `Title Case`.

### _StringToUpper

```csharp
public virtual string _StringToUpper(string string, string language)
```

引擎名：`_string_to_upper`

Returns the string converted to `UPPERCASE`.

### _StripDiacritics

```csharp
public virtual string _StripDiacritics(string string)
```

引擎名：`_strip_diacritics`

Strips diacritics from the string.

### _TagToName

```csharp
public virtual string _TagToName(long tag)
```

引擎名：`_tag_to_name`

Converts the given OpenType tag to the readable name of a feature, variation, script, or language.

### _UnreferenceOversamplingLevel

```csharp
public virtual void _UnreferenceOversamplingLevel(double oversampling)
```

引擎名：`_unreference_oversampling_level`

Decreases the reference count of the specified oversampling level, and frees the font cache for oversampling level when the reference count reaches zero. This method is called by [`Viewport`](Viewport.md), and should not be used directly.
