<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Label3D

命名空间 `Godot`；class；继承 [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A node for displaying plain text in 3D space. By adjusting various properties of this node, you can configure things such as the text's appearance and whether it always faces the camera.

## 构造函数

### Label3D

```csharp
public Label3D()
```

## 属性

### AlphaAntialiasingEdge

```csharp
public float AlphaAntialiasingEdge { get; set; }
```

引擎名：`alpha_antialiasing_edge`

Threshold at which antialiasing will be applied on the alpha channel.

### AlphaAntialiasingMode

```csharp
public BaseMaterial3D.AlphaAntiAliasing AlphaAntialiasingMode { get; set; }
```

引擎名：`alpha_antialiasing_mode`

The type of alpha antialiasing to apply.

### AlphaCut

```csharp
public Label3D.AlphaCutMode AlphaCut { get; set; }
```

引擎名：`alpha_cut`

The alpha cutting mode to use for the sprite.

### AlphaHashScale

```csharp
public float AlphaHashScale { get; set; }
```

引擎名：`alpha_hash_scale`

The hashing scale for Alpha Hash. Recommended values between `0` and `2`.

### AlphaScissorThreshold

```csharp
public float AlphaScissorThreshold { get; set; }
```

引擎名：`alpha_scissor_threshold`

Threshold at which the alpha scissor will discard values.

### AutowrapMode

```csharp
public TextServer.AutowrapMode AutowrapMode { get; set; }
```

引擎名：`autowrap_mode`

If set to something other than [`TextServer.AutowrapMode.Off`](TextServer.md), the text gets wrapped inside the node's bounding rectangle. If you resize the node, it will change its height automatically to show all the text.

### AutowrapTrimFlags

```csharp
public TextServer.LineBreakFlag AutowrapTrimFlags { get; set; }
```

引擎名：`autowrap_trim_flags`

Autowrap space trimming flags. See [`TextServer.LineBreakFlag.TrimStartEdgeSpaces`](TextServer.md) and [`TextServer.LineBreakFlag.TrimEndEdgeSpaces`](TextServer.md) for more info.

### Billboard

```csharp
public BaseMaterial3D.BillboardModeEnum Billboard { get; set; }
```

引擎名：`billboard`

The billboard mode to use for the label.

### DoubleSided

```csharp
public bool DoubleSided { get; set; }
```

引擎名：`double_sided`

If `true`, text can be seen from the back as well, if `false`, it is invisible when looking at it from behind.

### FixedSize

```csharp
public bool FixedSize { get; set; }
```

引擎名：`fixed_size`

If `true`, the label is rendered at the same size regardless of distance. The label's size on screen is the same as if the camera was `1.0` units away from the label's origin, regardless of the actual distance from the camera. The [`Camera3D`](Camera3D.md)'s field of view (or [`Camera3D.Size`](Camera3D.md) when in orthogonal/frustum mode) still affects the size the label is drawn at.

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

Font size of the `Label3D`'s text. To make the font look more detailed when up close, increase `FontSize` while decreasing `PixelSize` at the same time.

Higher font sizes require more time to render new characters, which can cause stuttering during gameplay.

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

### Modulate

```csharp
public Color Modulate { get; set; }
```

引擎名：`modulate`

Text [`Color`](Color.md) of the `Label3D`.

### NoDepthTest

```csharp
public bool NoDepthTest { get; set; }
```

引擎名：`no_depth_test`

If `true`, depth testing is disabled and the object will be drawn in render order.

### Offset

```csharp
public Vector2 Offset { get; set; }
```

引擎名：`offset`

The text drawing offset (in pixels).

### OutlineModulate

```csharp
public Color OutlineModulate { get; set; }
```

引擎名：`outline_modulate`

The tint of text outline.

### OutlineRenderPriority

```csharp
public int OutlineRenderPriority { get; set; }
```

引擎名：`outline_render_priority`

Sets the render priority for the text outline. Higher priority objects will be sorted in front of lower priority objects.

**Note:** This only applies if `AlphaCut` is set to [`Label3D.AlphaCutMode.Disabled`](Label3D.md) (default value).

**Note:** This only applies to sorting of transparent objects. This will not impact how transparent objects are sorted relative to opaque objects. This is because opaque objects are not sorted, while transparent objects are sorted from back to front (subject to priority).

### OutlineSize

```csharp
public int OutlineSize { get; set; }
```

引擎名：`outline_size`

Text outline size.

### PixelSize

```csharp
public float PixelSize { get; set; }
```

引擎名：`pixel_size`

The size of one pixel's width on the label to scale it in 3D. To make the font look more detailed when up close, increase `FontSize` while decreasing `PixelSize` at the same time.

### RenderPriority

```csharp
public int RenderPriority { get; set; }
```

引擎名：`render_priority`

Sets the render priority for the text. Higher priority objects will be sorted in front of lower priority objects.

**Note:** This only applies if `AlphaCut` is set to [`Label3D.AlphaCutMode.Disabled`](Label3D.md) (default value).

**Note:** This only applies to sorting of transparent objects. This will not impact how transparent objects are sorted relative to opaque objects. This is because opaque objects are not sorted, while transparent objects are sorted from back to front (subject to priority).

### Shaded

```csharp
public bool Shaded { get; set; }
```

引擎名：`shaded`

If `true`, the [`Light3D`](Light3D.md) in the [`Environment`](Environment.md) has effects on the label.

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

The text to display on screen.

### TextDirection

```csharp
public TextServer.Direction TextDirection { get; set; }
```

引擎名：`text_direction`

Base text writing direction.

### TextureFilter

```csharp
public BaseMaterial3D.TextureFilterEnum TextureFilter { get; set; }
```

引擎名：`texture_filter`

Filter flags for the texture.

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

Text width (in pixels), used for autowrap and fill alignment.

## 方法

### GenerateTriangleMesh

```csharp
public TriangleMesh GenerateTriangleMesh()
```

引擎名：`generate_triangle_mesh`

Returns a [`TriangleMesh`](TriangleMesh.md) with the label's vertices following its current configuration (such as its `PixelSize`).

### GetAlphaAntialiasing

```csharp
public BaseMaterial3D.AlphaAntiAliasing GetAlphaAntialiasing()
```

引擎名：`get_alpha_antialiasing`

### GetAlphaAntialiasingEdge

```csharp
public float GetAlphaAntialiasingEdge()
```

引擎名：`get_alpha_antialiasing_edge`

### GetAlphaCutMode

```csharp
public Label3D.AlphaCutMode GetAlphaCutMode()
```

引擎名：`get_alpha_cut_mode`

### GetAlphaHashScale

```csharp
public float GetAlphaHashScale()
```

引擎名：`get_alpha_hash_scale`

### GetAlphaScissorThreshold

```csharp
public float GetAlphaScissorThreshold()
```

引擎名：`get_alpha_scissor_threshold`

### GetAutowrapMode

```csharp
public TextServer.AutowrapMode GetAutowrapMode()
```

引擎名：`get_autowrap_mode`

### GetAutowrapTrimFlags

```csharp
public TextServer.LineBreakFlag GetAutowrapTrimFlags()
```

引擎名：`get_autowrap_trim_flags`

### GetBillboardMode

```csharp
public BaseMaterial3D.BillboardModeEnum GetBillboardMode()
```

引擎名：`get_billboard_mode`

### GetDrawFlag

```csharp
public bool GetDrawFlag(Label3D.DrawFlags flag)
```

引擎名：`get_draw_flag`

Returns the value of the specified flag.

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

### GetModulate

```csharp
public Color GetModulate()
```

引擎名：`get_modulate`

### GetOffset

```csharp
public Vector2 GetOffset()
```

引擎名：`get_offset`

### GetOutlineModulate

```csharp
public Color GetOutlineModulate()
```

引擎名：`get_outline_modulate`

### GetOutlineRenderPriority

```csharp
public int GetOutlineRenderPriority()
```

引擎名：`get_outline_render_priority`

### GetOutlineSize

```csharp
public int GetOutlineSize()
```

引擎名：`get_outline_size`

### GetPixelSize

```csharp
public float GetPixelSize()
```

引擎名：`get_pixel_size`

### GetRenderPriority

```csharp
public int GetRenderPriority()
```

引擎名：`get_render_priority`

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

### GetTextureFilter

```csharp
public BaseMaterial3D.TextureFilterEnum GetTextureFilter()
```

引擎名：`get_texture_filter`

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

### SetAlphaAntialiasing

```csharp
public void SetAlphaAntialiasing(BaseMaterial3D.AlphaAntiAliasing alphaAA)
```

引擎名：`set_alpha_antialiasing`

### SetAlphaAntialiasingEdge

```csharp
public void SetAlphaAntialiasingEdge(float edge)
```

引擎名：`set_alpha_antialiasing_edge`

### SetAlphaCutMode

```csharp
public void SetAlphaCutMode(Label3D.AlphaCutMode mode)
```

引擎名：`set_alpha_cut_mode`

### SetAlphaHashScale

```csharp
public void SetAlphaHashScale(float threshold)
```

引擎名：`set_alpha_hash_scale`

### SetAlphaScissorThreshold

```csharp
public void SetAlphaScissorThreshold(float threshold)
```

引擎名：`set_alpha_scissor_threshold`

### SetAutowrapMode

```csharp
public void SetAutowrapMode(TextServer.AutowrapMode autowrapMode)
```

引擎名：`set_autowrap_mode`

### SetAutowrapTrimFlags

```csharp
public void SetAutowrapTrimFlags(TextServer.LineBreakFlag autowrapTrimFlags)
```

引擎名：`set_autowrap_trim_flags`

### SetBillboardMode

```csharp
public void SetBillboardMode(BaseMaterial3D.BillboardModeEnum mode)
```

引擎名：`set_billboard_mode`

### SetDrawFlag

```csharp
public void SetDrawFlag(Label3D.DrawFlags flag, bool enabled)
```

引擎名：`set_draw_flag`

If `true`, the specified `flag` will be enabled.

### SetFont

```csharp
public void SetFont(Font font)
```

引擎名：`set_font`

### SetFontSize

```csharp
public void SetFontSize(int size)
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

### SetModulate

```csharp
public void SetModulate(Color modulate)
```

引擎名：`set_modulate`

### SetOffset

```csharp
public void SetOffset(Vector2 offset)
```

引擎名：`set_offset`

### SetOutlineModulate

```csharp
public void SetOutlineModulate(Color modulate)
```

引擎名：`set_outline_modulate`

### SetOutlineRenderPriority

```csharp
public void SetOutlineRenderPriority(int priority)
```

引擎名：`set_outline_render_priority`

### SetOutlineSize

```csharp
public void SetOutlineSize(int outlineSize)
```

引擎名：`set_outline_size`

### SetPixelSize

```csharp
public void SetPixelSize(float pixelSize)
```

引擎名：`set_pixel_size`

### SetRenderPriority

```csharp
public void SetRenderPriority(int priority)
```

引擎名：`set_render_priority`

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

### SetTextureFilter

```csharp
public void SetTextureFilter(BaseMaterial3D.TextureFilterEnum mode)
```

引擎名：`set_texture_filter`

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

## AlphaCutMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：This mode performs standard alpha blending.
- `Discard = 1`：This mode only allows fully transparent or fully opaque pixels.
- `OpaquePrepass = 2`：This mode draws fully opaque pixels in the depth prepass.
- `Hash = 3`：This mode draws cuts off all values below a spatially-deterministic threshold, the rest will remain opaque.

## DrawFlags

命名空间 `Godot`；enum

### 枚举值

- `Shaded = 0`：If set, lights in the environment affect the label.
- `DoubleSided = 1`：If set, text can be seen from the back as well.
- `DisableDepthTest = 2`：Disables the depth test, so this object is drawn on top of all others.
- `FixedSize = 3`：Label is scaled by depth so that it always appears the same size on screen.
- `Max = 4`：Represents the size of the `Label3D.DrawFlags` enum.
