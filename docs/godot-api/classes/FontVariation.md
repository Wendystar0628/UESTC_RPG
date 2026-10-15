<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# FontVariation

命名空间 `Godot`；class；继承 [`Font`](Font.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Provides OpenType variations, simulated bold / slant, and additional font settings like OpenType features and extra spacing.

To use simulated bold font variant:

```csharp
var fv = new FontVariation();
fv.SetBaseFont(ResourceLoader.Load<FontFile>("res://BarlowCondensed-Regular.ttf"));
fv.SetVariationEmbolden(1.2);
GetNode("Label").AddThemeFontOverride("font", fv);
GetNode("Label").AddThemeFontSizeOverride("font_size", 64);

```

To set the coordinate of multiple variation axes:

```csharp
var fv = FontVariation.new();
var ts = TextServerManager.get_primary_interface()
fv.base_font = load("res://BarlowCondensed-Regular.ttf")
fv.variation_opentype = { ts.name_to_tag("wght"): 900, ts.name_to_tag("custom_hght"): 900 }

```

## 构造函数

### FontVariation

```csharp
public FontVariation()
```

## 属性

### BaseFont

```csharp
public Font BaseFont { get; set; }
```

引擎名：`base_font`

Base font used to create a variation. If not set, default [`Theme`](Theme.md) font is used.

### BaselineOffset

```csharp
public float BaselineOffset { get; set; }
```

引擎名：`baseline_offset`

Extra baseline offset (as a fraction of font height).

### OpentypeFeatures

```csharp
public Dictionary OpentypeFeatures { get; set; }
```

引擎名：`opentype_features`

A set of OpenType feature tags. More info: [OpenType feature tags](https://docs.microsoft.com/en-us/typography/opentype/spec/featuretags).

### PaletteCustomColors

```csharp
public Color[] PaletteCustomColors { get; set; }
```

引擎名：`palette_custom_colors`

An array of colors to override predefined palette. Use `Color(0, 0, 0, 0)`, to keep predefined palette color at specific position.

### PaletteIndex

```csharp
public long PaletteIndex { get; set; }
```

引擎名：`palette_index`

A palette index.

### SpacingBottom

```csharp
public int SpacingBottom { get; set; }
```

引擎名：`spacing_bottom`

Extra spacing at the bottom of the line in pixels.

### SpacingGlyph

```csharp
public int SpacingGlyph { get; set; }
```

引擎名：`spacing_glyph`

Extra spacing between graphical glyphs.

### SpacingSpace

```csharp
public int SpacingSpace { get; set; }
```

引擎名：`spacing_space`

Extra width of the space glyphs.

### SpacingTop

```csharp
public int SpacingTop { get; set; }
```

引擎名：`spacing_top`

Extra spacing at the top of the line in pixels.

### VariationEmbolden

```csharp
public float VariationEmbolden { get; set; }
```

引擎名：`variation_embolden`

If is not equal to zero, emboldens the font outlines. Negative values reduce the outline thickness.

**Note:** Emboldened fonts might have self-intersecting outlines, which will prevent MSDF fonts and [`TextMesh`](TextMesh.md) from working correctly.

### VariationFaceIndex

```csharp
public int VariationFaceIndex { get; set; }
```

引擎名：`variation_face_index`

Active face index in the TrueType / OpenType collection file.

### VariationOpentype

```csharp
public Dictionary VariationOpentype { get; set; }
```

引擎名：`variation_opentype`

Font OpenType variation coordinates. More info: [OpenType variation tags](https://docs.microsoft.com/en-us/typography/opentype/spec/dvaraxisreg).

**Note:** This [`Collections.Dictionary`](Collections.Dictionary.md) uses OpenType tags as keys. Variation axes can be identified both by tags (`System.Int32`, e.g. `0x77678674`) and names (`System.String`, e.g. `wght`). Some axes might be accessible by multiple names. For example, `wght` refers to the same axis as `weight`. Tags on the other hand are unique. To convert between names and tags, use [`TextServer.NameToTag`](TextServer.md) and [`TextServer.TagToName`](TextServer.md).

**Note:** To get available variation axes of a font, use [`Font.GetSupportedVariationList`](Font.md).

### VariationTransform

```csharp
public Transform2D VariationTransform { get; set; }
```

引擎名：`variation_transform`

2D transform, applied to the font outlines, can be used for slanting, flipping and rotating glyphs.

For example, to simulate italic typeface by slanting, apply the following transform `Transform2D(1.0, slant, 0.0, 1.0, 0.0, 0.0)`.

## 方法

### GetBaseFont

```csharp
public Font GetBaseFont()
```

引擎名：`get_base_font`

### GetBaselineOffset

```csharp
public float GetBaselineOffset()
```

引擎名：`get_baseline_offset`

### GetPaletteCustomColors

```csharp
public Color[] GetPaletteCustomColors()
```

引擎名：`get_palette_custom_colors`

### GetPaletteIndex

```csharp
public long GetPaletteIndex()
```

引擎名：`get_palette_index`

### GetVariationEmbolden

```csharp
public float GetVariationEmbolden()
```

引擎名：`get_variation_embolden`

### GetVariationFaceIndex

```csharp
public int GetVariationFaceIndex()
```

引擎名：`get_variation_face_index`

### GetVariationOpentype

```csharp
public Dictionary GetVariationOpentype()
```

引擎名：`get_variation_opentype`

### GetVariationTransform

```csharp
public Transform2D GetVariationTransform()
```

引擎名：`get_variation_transform`

### SetBaseFont

```csharp
public void SetBaseFont(Font font)
```

引擎名：`set_base_font`

### SetBaselineOffset

```csharp
public void SetBaselineOffset(float baselineOffset)
```

引擎名：`set_baseline_offset`

### SetOpentypeFeatures

```csharp
public void SetOpentypeFeatures(Dictionary features)
```

引擎名：`set_opentype_features`

### SetPaletteCustomColors

```csharp
public void SetPaletteCustomColors(Color[] colors)
```

引擎名：`set_palette_custom_colors`

### SetPaletteCustomColors

```csharp
public void SetPaletteCustomColors(ReadOnlySpan<Color> colors)
```

引擎名：`set_palette_custom_colors`

### SetPaletteIndex

```csharp
public void SetPaletteIndex(long paletteIndex)
```

引擎名：`set_palette_index`

### SetSpacing

```csharp
public void SetSpacing(TextServer.SpacingType spacing, int value)
```

引擎名：`set_spacing`

Sets the spacing for `spacing` to `value` in pixels (not relative to the font size).

### SetVariationEmbolden

```csharp
public void SetVariationEmbolden(float strength)
```

引擎名：`set_variation_embolden`

### SetVariationFaceIndex

```csharp
public void SetVariationFaceIndex(int faceIndex)
```

引擎名：`set_variation_face_index`

### SetVariationOpentype

```csharp
public void SetVariationOpentype(Dictionary coords)
```

引擎名：`set_variation_opentype`

### SetVariationTransform

```csharp
public void SetVariationTransform(Transform2D transform)
```

引擎名：`set_variation_transform`
