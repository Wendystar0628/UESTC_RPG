<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Font

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Abstract base class for different font types. It has methods for drawing text and font character introspection.

## 属性

### Fallbacks

```csharp
public Array<Font> Fallbacks { get; set; }
```

引擎名：`fallbacks`

Array of fallback `Font`s to use as a substitute if a glyph is not found in this current `Font`.

If this array is empty in a [`FontVariation`](FontVariation.md), the [`FontVariation.BaseFont`](FontVariation.md)'s fallbacks are used instead.

## 方法

### DrawChar

```csharp
public float DrawChar(Rid canvasItem, Vector2 pos, long char, int fontSize, Color? modulate = null, float oversampling = 0f)
```

引擎名：`draw_char`

Draw a single Unicode character `char` into a canvas item using the font, at a given position, with `modulate` color. `pos` specifies the baseline, not the top. To draw from the top, *ascent* must be added to the Y axis. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

**Note:** Do not use this function to draw strings character by character, use `DrawString()` or [`TextLine`](TextLine.md) instead.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawChar

```csharp
public float DrawChar(Rid canvasItem, Vector2 pos, long char, int fontSize, Color? modulate)
```

引擎名：`draw_char`

Draw a single Unicode character `char` into a canvas item using the font, at a given position, with `modulate` color. `pos` specifies the baseline, not the top. To draw from the top, *ascent* must be added to the Y axis. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

**Note:** Do not use this function to draw strings character by character, use `DrawString()` or [`TextLine`](TextLine.md) instead.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawCharOutline

```csharp
public float DrawCharOutline(Rid canvasItem, Vector2 pos, long char, int fontSize, int size = -1, Color? modulate = null, float oversampling = 0f)
```

引擎名：`draw_char_outline`

Draw a single Unicode character `char` outline into a canvas item using the font, at a given position, with `modulate` color and `size` outline size. `pos` specifies the baseline, not the top. To draw from the top, *ascent* must be added to the Y axis. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

**Note:** Do not use this function to draw strings character by character, use `DrawString()` or [`TextLine`](TextLine.md) instead.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawCharOutline

```csharp
public float DrawCharOutline(Rid canvasItem, Vector2 pos, long char, int fontSize, int size, Color? modulate)
```

引擎名：`draw_char_outline`

Draw a single Unicode character `char` outline into a canvas item using the font, at a given position, with `modulate` color and `size` outline size. `pos` specifies the baseline, not the top. To draw from the top, *ascent* must be added to the Y axis. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

**Note:** Do not use this function to draw strings character by character, use `DrawString()` or [`TextLine`](TextLine.md) instead.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawMultilineString

```csharp
public void DrawMultilineString(Rid canvasItem, Vector2 pos, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1f, int fontSize = 16, int maxLines = -1, Color? modulate = null, TextServer.LineBreakFlag brkFlags = TextServer.LineBreakFlag., TextServer.JustificationFlag justificationFlags = TextServer.JustificationFlag., TextServer.Direction direction = TextServer.Direction.Auto, TextServer.Orientation orientation = TextServer.Orientation.Horizontal, float oversampling = 0f)
```

引擎名：`draw_multiline_string`

Breaks `text` into lines using rules specified by `brkFlags` and draws it into a canvas item using the font, at a given position, with `modulate` color, optionally clipping the width and aligning horizontally. `pos` specifies the baseline of the first line, not the top. To draw from the top, *ascent* must be added to the Y axis. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

See also [`CanvasItem.DrawMultilineString`](CanvasItem.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawMultilineString

```csharp
public void DrawMultilineString(Rid canvasItem, Vector2 pos, string text, HorizontalAlignment alignment, float width, int fontSize, int maxLines, Color? modulate, TextServer.LineBreakFlag brkFlags, TextServer.JustificationFlag justificationFlags, TextServer.Direction direction, TextServer.Orientation orientation)
```

引擎名：`draw_multiline_string`

Breaks `text` into lines using rules specified by `brkFlags` and draws it into a canvas item using the font, at a given position, with `modulate` color, optionally clipping the width and aligning horizontally. `pos` specifies the baseline of the first line, not the top. To draw from the top, *ascent* must be added to the Y axis. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

See also [`CanvasItem.DrawMultilineString`](CanvasItem.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawMultilineStringOutline

```csharp
public void DrawMultilineStringOutline(Rid canvasItem, Vector2 pos, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1f, int fontSize = 16, int maxLines = -1, int size = 1, Color? modulate = null, TextServer.LineBreakFlag brkFlags = TextServer.LineBreakFlag., TextServer.JustificationFlag justificationFlags = TextServer.JustificationFlag., TextServer.Direction direction = TextServer.Direction.Auto, TextServer.Orientation orientation = TextServer.Orientation.Horizontal, float oversampling = 0f)
```

引擎名：`draw_multiline_string_outline`

Breaks `text` to the lines using rules specified by `brkFlags` and draws text outline into a canvas item using the font, at a given position, with `modulate` color and `size` outline size, optionally clipping the width and aligning horizontally. `pos` specifies the baseline of the first line, not the top. To draw from the top, *ascent* must be added to the Y axis. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

See also [`CanvasItem.DrawMultilineStringOutline`](CanvasItem.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawMultilineStringOutline

```csharp
public void DrawMultilineStringOutline(Rid canvasItem, Vector2 pos, string text, HorizontalAlignment alignment, float width, int fontSize, int maxLines, int size, Color? modulate, TextServer.LineBreakFlag brkFlags, TextServer.JustificationFlag justificationFlags, TextServer.Direction direction, TextServer.Orientation orientation)
```

引擎名：`draw_multiline_string_outline`

Breaks `text` to the lines using rules specified by `brkFlags` and draws text outline into a canvas item using the font, at a given position, with `modulate` color and `size` outline size, optionally clipping the width and aligning horizontally. `pos` specifies the baseline of the first line, not the top. To draw from the top, *ascent* must be added to the Y axis. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

See also [`CanvasItem.DrawMultilineStringOutline`](CanvasItem.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawString

```csharp
public void DrawString(Rid canvasItem, Vector2 pos, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1f, int fontSize = 16, Color? modulate = null, TextServer.JustificationFlag justificationFlags = TextServer.JustificationFlag., TextServer.Direction direction = TextServer.Direction.Auto, TextServer.Orientation orientation = TextServer.Orientation.Horizontal, float oversampling = 0f)
```

引擎名：`draw_string`

Draw `text` into a canvas item using the font, at a given position, with `modulate` color, optionally clipping the width and aligning horizontally. `pos` specifies the baseline, not the top. To draw from the top, *ascent* must be added to the Y axis. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

See also [`CanvasItem.DrawString`](CanvasItem.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawString

```csharp
public void DrawString(Rid canvasItem, Vector2 pos, string text, HorizontalAlignment alignment, float width, int fontSize, Color? modulate, TextServer.JustificationFlag justificationFlags, TextServer.Direction direction, TextServer.Orientation orientation)
```

引擎名：`draw_string`

Draw `text` into a canvas item using the font, at a given position, with `modulate` color, optionally clipping the width and aligning horizontally. `pos` specifies the baseline, not the top. To draw from the top, *ascent* must be added to the Y axis. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

See also [`CanvasItem.DrawString`](CanvasItem.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawStringOutline

```csharp
public void DrawStringOutline(Rid canvasItem, Vector2 pos, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1f, int fontSize = 16, int size = 1, Color? modulate = null, TextServer.JustificationFlag justificationFlags = TextServer.JustificationFlag., TextServer.Direction direction = TextServer.Direction.Auto, TextServer.Orientation orientation = TextServer.Orientation.Horizontal, float oversampling = 0f)
```

引擎名：`draw_string_outline`

Draw `text` outline into a canvas item using the font, at a given position, with `modulate` color and `size` outline size, optionally clipping the width and aligning horizontally. `pos` specifies the baseline, not the top. To draw from the top, *ascent* must be added to the Y axis. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

See also [`CanvasItem.DrawStringOutline`](CanvasItem.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawStringOutline

```csharp
public void DrawStringOutline(Rid canvasItem, Vector2 pos, string text, HorizontalAlignment alignment, float width, int fontSize, int size, Color? modulate, TextServer.JustificationFlag justificationFlags, TextServer.Direction direction, TextServer.Orientation orientation)
```

引擎名：`draw_string_outline`

Draw `text` outline into a canvas item using the font, at a given position, with `modulate` color and `size` outline size, optionally clipping the width and aligning horizontally. `pos` specifies the baseline, not the top. To draw from the top, *ascent* must be added to the Y axis. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

See also [`CanvasItem.DrawStringOutline`](CanvasItem.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### FindVariation

```csharp
public Rid FindVariation(Dictionary variationCoordinates, int faceIndex = 0, float strength = 0f, Transform2D? transform = null, int spacingTop = 0, int spacingBottom = 0, int spacingSpace = 0, int spacingGlyph = 0, float baselineOffset = 0f, long paletteIndex = 0, Color[] customColors = null)
```

引擎名：`find_variation`

Returns [`TextServer`](TextServer.md) RID of the font cache for specific variation.

参数：
- `transform`：If the parameter is null, then the default value is `Transform2D.Identity`.
- `customColors`：If the parameter is null, then the default value is `Array.Empty<Color>()`.

### FindVariation

```csharp
public Rid FindVariation(Dictionary variationCoordinates, int faceIndex, float strength, Transform2D? transform)
```

引擎名：`find_variation`

Returns [`TextServer`](TextServer.md) RID of the font cache for specific variation.

参数：
- `transform`：If the parameter is null, then the default value is `Transform2D.Identity`.

### FindVariation

```csharp
public Rid FindVariation(Dictionary variationCoordinates, int faceIndex, float strength, Transform2D? transform, int spacingTop, int spacingBottom, int spacingSpace, int spacingGlyph)
```

引擎名：`find_variation`

Returns [`TextServer`](TextServer.md) RID of the font cache for specific variation.

参数：
- `transform`：If the parameter is null, then the default value is `Transform2D.Identity`.

### FindVariation

```csharp
public Rid FindVariation(Dictionary variationCoordinates, int faceIndex, float strength, Transform2D? transform, int spacingTop, int spacingBottom, int spacingSpace, int spacingGlyph, float baselineOffset)
```

引擎名：`find_variation`

Returns [`TextServer`](TextServer.md) RID of the font cache for specific variation.

参数：
- `transform`：If the parameter is null, then the default value is `Transform2D.Identity`.

### FindVariation

```csharp
public Rid FindVariation(Dictionary variationCoordinates, int faceIndex, float strength, Transform2D? transform, int spacingTop, int spacingBottom, int spacingSpace, int spacingGlyph, float baselineOffset, long paletteIndex, ReadOnlySpan<Color> customColors)
```

引擎名：`find_variation`

Returns [`TextServer`](TextServer.md) RID of the font cache for specific variation.

参数：
- `transform`：If the parameter is null, then the default value is `Transform2D.Identity`.

### GetAscent

```csharp
public float GetAscent(int fontSize = 16)
```

引擎名：`get_ascent`

Returns the maximum font ascent (number of pixels above the baseline) of this font and all fallback fonts.

**Note:** Real ascent of the string is context-dependent and can be significantly different from the value returned by this function. Use it only as rough estimate (e.g. as the ascent of empty line).

### GetCharSize

```csharp
public Vector2 GetCharSize(long char, int fontSize)
```

引擎名：`get_char_size`

Returns the size of a character. Does not take kerning into account.

**Note:** Do not use this function to calculate width of the string character by character, use `GetStringSize()` or [`TextLine`](TextLine.md) instead. The height returned is the font height (see also `GetHeight()`) and has no relation to the glyph height.

### GetDescent

```csharp
public float GetDescent(int fontSize = 16)
```

引擎名：`get_descent`

Returns the maximum font descent (number of pixels below the baseline) of this font and all fallback fonts.

**Note:** Real descent of the string is context-dependent and can be significantly different from the value returned by this function. Use it only as rough estimate (e.g. as the descent of empty line).

### GetFaceCount

```csharp
public long GetFaceCount()
```

引擎名：`get_face_count`

Returns number of faces in the TrueType / OpenType collection.

### GetFallbacks

```csharp
public Array<Font> GetFallbacks()
```

引擎名：`get_fallbacks`

### GetFontName

```csharp
public string GetFontName()
```

引擎名：`get_font_name`

Returns font family name.

### GetFontStretch

```csharp
public int GetFontStretch()
```

引擎名：`get_font_stretch`

Returns font stretch amount, compared to a normal width. A percentage value between `50%` and `200%`.

### GetFontStyle

```csharp
public TextServer.FontStyle GetFontStyle()
```

引擎名：`get_font_style`

Returns font style flags.

### GetFontStyleName

```csharp
public string GetFontStyleName()
```

引擎名：`get_font_style_name`

Returns font style name.

### GetFontWeight

```csharp
public int GetFontWeight()
```

引擎名：`get_font_weight`

Returns weight (boldness) of the font. A value in the `100...999` range, normal font weight is `400`, bold font weight is `700`.

### GetHeight

```csharp
public float GetHeight(int fontSize = 16)
```

引擎名：`get_height`

Returns the total average font height (ascent plus descent) in pixels.

**Note:** Real height of the string is context-dependent and can be significantly different from the value returned by this function. Use it only as rough estimate (e.g. as the height of empty line).

### GetMultilineStringSize

```csharp
public Vector2 GetMultilineStringSize(string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1f, int fontSize = 16, int maxLines = -1, TextServer.LineBreakFlag brkFlags = TextServer.LineBreakFlag., TextServer.JustificationFlag justificationFlags = TextServer.JustificationFlag., TextServer.Direction direction = TextServer.Direction.Auto, TextServer.Orientation orientation = TextServer.Orientation.Horizontal)
```

引擎名：`get_multiline_string_size`

Returns the size of a bounding box of a string broken into the lines, taking kerning and advance into account.

See also `DrawMultilineString()`.

### GetOpentypeFeatures

```csharp
public Dictionary GetOpentypeFeatures()
```

引擎名：`get_opentype_features`

Returns a set of OpenType feature tags. More info: [OpenType feature tags](https://docs.microsoft.com/en-us/typography/opentype/spec/featuretags).

### GetOtNameStrings

```csharp
public Dictionary GetOtNameStrings()
```

引擎名：`get_ot_name_strings`

Returns [`Collections.Dictionary`](Collections.Dictionary.md) with OpenType font name strings (localized font names, version, description, license information, sample text, etc.).

### GetPaletteColors

```csharp
public Color[] GetPaletteColors(long index)
```

引擎名：`get_palette_colors`

Returns the array in the predefined color palette at `index`. Palette contains all colors used to render font glyphs. Each palette has the same number of colors. Colors can be overridden using [`FontVariation`](FontVariation.md).

### GetPaletteCount

```csharp
public long GetPaletteCount()
```

引擎名：`get_palette_count`

Returns the number of predefined color palettes. Palette contains all colors used to render font glyphs. Each palette has the same number of colors.

### GetPaletteName

```csharp
public string GetPaletteName(long index)
```

引擎名：`get_palette_name`

Returns the name of the predefined color palette at `index`. Palette contains all colors used to render font glyphs. Each palette has the same number of colors.

### GetRids

```csharp
public Array<Rid> GetRids()
```

引擎名：`get_rids`

Returns [`Collections.Array`](Collections.Array.md) of valid `Font` [`Rid`](Rid.md)s, which can be passed to the [`TextServer`](TextServer.md) methods.

### GetSpacing

```csharp
public int GetSpacing(TextServer.SpacingType spacing)
```

引擎名：`get_spacing`

Returns the amount of spacing for the given `spacing` type.

### GetStringSize

```csharp
public Vector2 GetStringSize(string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1f, int fontSize = 16, TextServer.JustificationFlag justificationFlags = TextServer.JustificationFlag., TextServer.Direction direction = TextServer.Direction.Auto, TextServer.Orientation orientation = TextServer.Orientation.Horizontal)
```

引擎名：`get_string_size`

Returns the size of a bounding box of a single-line string, taking kerning, advance and subpixel positioning into account. See also `GetMultilineStringSize()` and `DrawString()`.

For example, to get the string size as displayed by a single-line Label, use:

```csharp
Label label = GetNode<Label>("Label");
Vector2 stringSize = label.GetThemeFont("font").GetStringSize(label.Text, HorizontalAlignment.Left, -1, label.GetThemeFontSize("font_size"));

```

**Note:** Since kerning, advance and subpixel positioning are taken into account by `GetStringSize()`, using separate `GetStringSize()` calls on substrings of a string then adding the results together will return a different result compared to using a single `GetStringSize()` call on the full string.

**Note:** Real height of the string is context-dependent and can be significantly different from the value returned by `GetHeight()`.

### GetSupportedChars

```csharp
public string GetSupportedChars()
```

引擎名：`get_supported_chars`

Returns a string containing all the characters available in the font.

If a given character is included in more than one font data source, it appears only once in the returned string.

### GetSupportedFeatureList

```csharp
public Dictionary GetSupportedFeatureList()
```

引擎名：`get_supported_feature_list`

Returns list of OpenType features supported by font.

### GetSupportedVariationList

```csharp
public Dictionary GetSupportedVariationList()
```

引擎名：`get_supported_variation_list`

Returns list of supported [variation coordinates](https://docs.microsoft.com/en-us/typography/opentype/spec/dvaraxisreg), each coordinate is returned as `tag: Vector3i(min_value,max_value,default_value)`.

Font variations allow for continuous change of glyph characteristics along some given design axis, such as weight, width or slant.

To print available variation axes of a variable font:

```csharp
var fv = FontVariation.new()
fv.base_font = load("res://RobotoFlex.ttf")
var variation_list = fv.get_supported_variation_list()
for tag in variation_list:
var name = TextServerManager.get_primary_interface().tag_to_name(tag)
var values = variation_list[tag]
print("variation axis: %s (%d)\n\tmin, max, default: %s" % [name, tag, values])

```

**Note:** To set and get variation coordinates of a [`FontVariation`](FontVariation.md), use [`FontVariation.VariationOpentype`](FontVariation.md).

### GetUnderlinePosition

```csharp
public float GetUnderlinePosition(int fontSize = 16)
```

引擎名：`get_underline_position`

Returns average pixel offset of the underline below the baseline.

**Note:** Real underline position of the string is context-dependent and can be significantly different from the value returned by this function. Use it only as rough estimate.

### GetUnderlineThickness

```csharp
public float GetUnderlineThickness(int fontSize = 16)
```

引擎名：`get_underline_thickness`

Returns average thickness of the underline.

**Note:** Real underline thickness of the string is context-dependent and can be significantly different from the value returned by this function. Use it only as rough estimate.

### HasChar

```csharp
public bool HasChar(long char)
```

引擎名：`has_char`

Returns `true` if a Unicode `char` is available in the font.

### IsLanguageSupported

```csharp
public bool IsLanguageSupported(string language)
```

引擎名：`is_language_supported`

Returns `true` if the font supports the given language (as a [ISO 639](https://en.wikipedia.org/wiki/ISO_639-1) code).

### IsScriptSupported

```csharp
public bool IsScriptSupported(string script)
```

引擎名：`is_script_supported`

Returns `true` if the font supports the given script (as a [ISO 15924](https://en.wikipedia.org/wiki/ISO_15924) code).

### SetCacheCapacity

```csharp
public void SetCacheCapacity(int singleLine, int multiLine)
```

引擎名：`set_cache_capacity`

Sets LRU cache capacity for `draw_*` methods.

### SetFallbacks

```csharp
public void SetFallbacks(Array<Font> fallbacks)
```

引擎名：`set_fallbacks`
