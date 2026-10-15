<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextMesh

命名空间 `Godot`；class；继承 [`PrimitiveMesh`](PrimitiveMesh.md) → [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Generate a [`PrimitiveMesh`](PrimitiveMesh.md) from the text.

TextMesh can be generated only when using dynamic fonts with vector glyph contours. Bitmap fonts (including bitmap data in the TrueType/OpenType containers, like color emoji fonts) are not supported.

The UV layout is arranged in 4 horizontal strips, top to bottom: 40% of the height for the front face, 40% for the back face, 10% for the outer edges and 10% for the inner edges.

## 构造函数

### TextMesh

```csharp
public TextMesh()
```

## 属性

### AutowrapMode

```csharp
public TextServer.AutowrapMode AutowrapMode { get; set; }
```

引擎名：`autowrap_mode`

If set to something other than [`TextServer.AutowrapMode.Off`](TextServer.md), the text gets wrapped inside the node's bounding rectangle. If you resize the node, it will change its height automatically to show all the text.

### CurveStep

```csharp
public float CurveStep { get; set; }
```

引擎名：`curve_step`

Step (in pixels) used to approximate Bézier curves. Lower values result in smoother curves, but is slower to generate and render. Consider adjusting this according to the font size and the typical viewing distance.

**Note:** Changing this property will regenerate the mesh, which is a slow operation, especially with large font sizes and long texts.

### Depth

```csharp
public float Depth { get; set; }
```

引擎名：`depth`

Depths of the mesh, if set to `0.0` only front surface, is generated, and UV layout is changed to use full texture for the front face only.

### Font

```csharp
public Font Font { get; set; }
```

引擎名：`font`

Font configuration used to display text.

### FontSize

```csharp
public int FontSize { get; set; }
```

引擎名：`font_size`

Font size of the `TextMesh`'s text. This property works in tandem with `PixelSize`. Higher values will result in a more detailed font, regardless of `CurveStep` and `PixelSize`. Consider keeping this value below 63 (inclusive) for good performance, and adjust `PixelSize` as needed to enlarge text.

**Note:** Changing this property will regenerate the mesh, which is a slow operation, especially with large font sizes and long texts. To change the text's size in real-time efficiently, change the node's [`Node3D.Scale`](Node3D.md) instead.

### HorizontalAlignment

```csharp
public HorizontalAlignment HorizontalAlignment { get; set; }
```

引擎名：`horizontal_alignment`

Controls the text's horizontal alignment. Supports left, center, right, and fill (also known as justify).

### JustificationFlags

```csharp
public TextServer.JustificationFlag JustificationFlags { get; set; }
```

引擎名：`justification_flags`

Line fill alignment rules.

### Language

```csharp
public string Language { get; set; }
```

引擎名：`language`

Language code used for line-breaking and text shaping algorithms. If left empty, the current locale is used instead.

### LineSpacing

```csharp
public float LineSpacing { get; set; }
```

引擎名：`line_spacing`

Additional vertical spacing between lines (in pixels), spacing is added to line descent. This value can be negative.

### Offset

```csharp
public Vector2 Offset { get; set; }
```

引擎名：`offset`

The text drawing offset (in pixels).

**Note:** Changing this property will regenerate the mesh, which is a slow operation. To change the text's position in real-time efficiently, change the node's [`Node3D.Position`](Node3D.md) instead.

### PixelSize

```csharp
public float PixelSize { get; set; }
```

引擎名：`pixel_size`

The size of one pixel's width on the text to scale it in 3D. This property works in tandem with `FontSize`.

**Note:** Changing this property will regenerate the mesh, which is a slow operation, especially with large font sizes and long texts. To change the text's size in real-time efficiently, change the node's [`Node3D.Scale`](Node3D.md) instead.

### StructuredTextBidiOverride

```csharp
public TextServer.StructuredTextParser StructuredTextBidiOverride { get; set; }
```

引擎名：`structured_text_bidi_override`

Set BiDi algorithm override for the structured text.

### StructuredTextBidiOverrideOptions

```csharp
public Array StructuredTextBidiOverrideOptions { get; set; }
```

引擎名：`structured_text_bidi_override_options`

Set additional options for BiDi override.

### Text

```csharp
public string Text { get; set; }
```

引擎名：`text`

The text to generate mesh from.

**Note:** Due to being a [`Resource`](Resource.md), it doesn't follow the rules of [`Node.AutoTranslateMode`](Node.md). If disabling translation is desired, it should be done manually with [`GodotObject.SetMessageTranslation`](GodotObject.md).

### TextDirection

```csharp
public TextServer.Direction TextDirection { get; set; }
```

引擎名：`text_direction`

Base text writing direction.

### Uppercase

```csharp
public bool Uppercase { get; set; }
```

引擎名：`uppercase`

If `true`, all the text displays as UPPERCASE.

### VerticalAlignment

```csharp
public VerticalAlignment VerticalAlignment { get; set; }
```

引擎名：`vertical_alignment`

Controls the text's vertical alignment. Supports top, center, and bottom.

### Width

```csharp
public float Width { get; set; }
```

引擎名：`width`

Text width (in pixels), used for fill alignment.

## 方法

### GetAutowrapMode

```csharp
public TextServer.AutowrapMode GetAutowrapMode()
```

引擎名：`get_autowrap_mode`

### GetCurveStep

```csharp
public float GetCurveStep()
```

引擎名：`get_curve_step`

### GetDepth

```csharp
public float GetDepth()
```

引擎名：`get_depth`

### GetFont

```csharp
public Font GetFont()
```

引擎名：`get_font`

### GetFontSize

```csharp
public int GetFontSize()
```

引擎名：`get_font_size`

### GetHorizontalAlignment

```csharp
public HorizontalAlignment GetHorizontalAlignment()
```

引擎名：`get_horizontal_alignment`

### GetJustificationFlags

```csharp
public TextServer.JustificationFlag GetJustificationFlags()
```

引擎名：`get_justification_flags`

### GetLanguage

```csharp
public string GetLanguage()
```

引擎名：`get_language`

### GetLineSpacing

```csharp
public float GetLineSpacing()
```

引擎名：`get_line_spacing`

### GetOffset

```csharp
public Vector2 GetOffset()
```

引擎名：`get_offset`

### GetPixelSize

```csharp
public float GetPixelSize()
```

引擎名：`get_pixel_size`

### GetStructuredTextBidiOverride

```csharp
public TextServer.StructuredTextParser GetStructuredTextBidiOverride()
```

引擎名：`get_structured_text_bidi_override`

### GetStructuredTextBidiOverrideOptions

```csharp
public Array GetStructuredTextBidiOverrideOptions()
```

引擎名：`get_structured_text_bidi_override_options`

### GetText

```csharp
public string GetText()
```

引擎名：`get_text`

### GetTextDirection

```csharp
public TextServer.Direction GetTextDirection()
```

引擎名：`get_text_direction`

### GetVerticalAlignment

```csharp
public VerticalAlignment GetVerticalAlignment()
```

引擎名：`get_vertical_alignment`

### GetWidth

```csharp
public float GetWidth()
```

引擎名：`get_width`

### IsUppercase

```csharp
public bool IsUppercase()
```

引擎名：`is_uppercase`

### SetAutowrapMode

```csharp
public void SetAutowrapMode(TextServer.AutowrapMode autowrapMode)
```

引擎名：`set_autowrap_mode`

### SetCurveStep

```csharp
public void SetCurveStep(float curveStep)
```

引擎名：`set_curve_step`

### SetDepth

```csharp
public void SetDepth(float depth)
```

引擎名：`set_depth`

### SetFont

```csharp
public void SetFont(Font font)
```

引擎名：`set_font`

### SetFontSize

```csharp
public void SetFontSize(int fontSize)
```

引擎名：`set_font_size`

### SetHorizontalAlignment

```csharp
public void SetHorizontalAlignment(HorizontalAlignment alignment)
```

引擎名：`set_horizontal_alignment`

### SetJustificationFlags

```csharp
public void SetJustificationFlags(TextServer.JustificationFlag justificationFlags)
```

引擎名：`set_justification_flags`

### SetLanguage

```csharp
public void SetLanguage(string language)
```

引擎名：`set_language`

### SetLineSpacing

```csharp
public void SetLineSpacing(float lineSpacing)
```

引擎名：`set_line_spacing`

### SetOffset

```csharp
public void SetOffset(Vector2 offset)
```

引擎名：`set_offset`

### SetPixelSize

```csharp
public void SetPixelSize(float pixelSize)
```

引擎名：`set_pixel_size`

### SetStructuredTextBidiOverride

```csharp
public void SetStructuredTextBidiOverride(TextServer.StructuredTextParser parser)
```

引擎名：`set_structured_text_bidi_override`

### SetStructuredTextBidiOverrideOptions

```csharp
public void SetStructuredTextBidiOverrideOptions(Array args)
```

引擎名：`set_structured_text_bidi_override_options`

### SetText

```csharp
public void SetText(string text)
```

引擎名：`set_text`

### SetTextDirection

```csharp
public void SetTextDirection(TextServer.Direction direction)
```

引擎名：`set_text_direction`

### SetUppercase

```csharp
public void SetUppercase(bool enable)
```

引擎名：`set_uppercase`

### SetVerticalAlignment

```csharp
public void SetVerticalAlignment(VerticalAlignment alignment)
```

引擎名：`set_vertical_alignment`

### SetWidth

```csharp
public void SetWidth(float width)
```

引擎名：`set_width`
