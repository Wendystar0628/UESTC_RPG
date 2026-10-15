<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CharFXTransform

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

By setting various properties on this object, you can control how individual characters will be displayed in a [`RichTextEffect`](RichTextEffect.md).

## 构造函数

### CharFXTransform

```csharp
public CharFXTransform()
```

## 属性

### Color

```csharp
public Color Color { get; set; }
```

引擎名：`color`

The color the character will be drawn with.

### ElapsedTime

```csharp
public double ElapsedTime { get; set; }
```

引擎名：`elapsed_time`

The time elapsed since the [`RichTextLabel`](RichTextLabel.md) was added to the scene tree (in seconds). Time stops when the [`RichTextLabel`](RichTextLabel.md) is paused (see [`Node.ProcessMode`](Node.md)). Resets when the text in the [`RichTextLabel`](RichTextLabel.md) is changed.

**Note:** Time still passes while the [`RichTextLabel`](RichTextLabel.md) is hidden.

### Env

```csharp
public Dictionary Env { get; set; }
```

引擎名：`env`

Contains the arguments passed in the opening BBCode tag. By default, arguments are strings; if their contents match a type such as `System.Boolean`, `System.Int32` or `System.Single`, they will be converted automatically. Color codes in the form `#rrggbb` or `#rgb` will be converted to an opaque [`Color`](Color.md). String arguments may not contain spaces, even if they're quoted. If present, quotes will also be present in the final string.

For example, the opening BBCode tag `[example foo=hello bar=true baz=42 color=#ffffff]` will map to the following [`Collections.Dictionary`](Collections.Dictionary.md):

```csharp
{"foo": "hello", "bar": true, "baz": 42, "color": Color(1, 1, 1, 1)}

```

### Font

```csharp
public Rid Font { get; set; }
```

引擎名：`font`

[`TextServer`](TextServer.md) RID of the font used to render glyph, this value can be used with `TextServer.font_*` methods to retrieve font information.

**Note:** Read-only. Setting this property won't affect drawing.

### GlyphCount

```csharp
public byte GlyphCount { get; set; }
```

引擎名：`glyph_count`

Number of glyphs in the grapheme cluster. This value is set in the first glyph of a cluster.

**Note:** Read-only. Setting this property won't affect drawing.

### GlyphFlags

```csharp
public ushort GlyphFlags { get; set; }
```

引擎名：`glyph_flags`

Glyph flags. See [`TextServer.GraphemeFlag`](TextServer.md) for more info.

**Note:** Read-only. Setting this property won't affect drawing.

### GlyphIndex

```csharp
public uint GlyphIndex { get; set; }
```

引擎名：`glyph_index`

Glyph index specific to the `Font`. If you want to replace this glyph, use [`TextServer.FontGetGlyphIndex`](TextServer.md) with `Font` to get a new glyph index for a single character.

### Offset

```csharp
public Vector2 Offset { get; set; }
```

引擎名：`offset`

The position offset the character will be drawn with (in pixels).

### Outline

```csharp
public bool Outline { get; set; }
```

引擎名：`outline`

If `true`, FX transform is called for outline drawing.

**Note:** Read-only. Setting this property won't affect drawing.

### Range

```csharp
public Vector2I Range { get; set; }
```

引擎名：`range`

Absolute character range in the string, corresponding to the glyph.

**Note:** Read-only. Setting this property won't affect drawing.

### RelativeIndex

```csharp
public int RelativeIndex { get; set; }
```

引擎名：`relative_index`

The character offset of the glyph, relative to the current [`RichTextEffect`](RichTextEffect.md) custom block.

**Note:** Read-only. Setting this property won't affect drawing.

### Transform

```csharp
public Transform2D Transform { get; set; }
```

引擎名：`transform`

The current transform of the current glyph. It can be overridden (for example, by driving the position and rotation from a curve). You can also alter the existing value to apply transforms on top of other effects.

### Visible

```csharp
public bool Visible { get; set; }
```

引擎名：`visible`

If `true`, the character will be drawn. If `false`, the character will be hidden. Characters around hidden characters will reflow to take the space of hidden characters. If this is not desired, set their `Color` to `Color(1, 1, 1, 0)` instead.

## 方法

### GetColor

```csharp
public Color GetColor()
```

引擎名：`get_color`

### GetElapsedTime

```csharp
public double GetElapsedTime()
```

引擎名：`get_elapsed_time`

### GetEnvironment

```csharp
public Dictionary GetEnvironment()
```

引擎名：`get_environment`

### GetFont

```csharp
public Rid GetFont()
```

引擎名：`get_font`

### GetGlyphCount

```csharp
public byte GetGlyphCount()
```

引擎名：`get_glyph_count`

### GetGlyphFlags

```csharp
public ushort GetGlyphFlags()
```

引擎名：`get_glyph_flags`

### GetGlyphIndex

```csharp
public uint GetGlyphIndex()
```

引擎名：`get_glyph_index`

### GetOffset

```csharp
public Vector2 GetOffset()
```

引擎名：`get_offset`

### GetRange

```csharp
public Vector2I GetRange()
```

引擎名：`get_range`

### GetRelativeIndex

```csharp
public int GetRelativeIndex()
```

引擎名：`get_relative_index`

### GetTransform

```csharp
public Transform2D GetTransform()
```

引擎名：`get_transform`

### IsOutline

```csharp
public bool IsOutline()
```

引擎名：`is_outline`

### IsVisible

```csharp
public bool IsVisible()
```

引擎名：`is_visible`

### SetColor

```csharp
public void SetColor(Color color)
```

引擎名：`set_color`

### SetElapsedTime

```csharp
public void SetElapsedTime(double time)
```

引擎名：`set_elapsed_time`

### SetEnvironment

```csharp
public void SetEnvironment(Dictionary environment)
```

引擎名：`set_environment`

### SetFont

```csharp
public void SetFont(Rid font)
```

引擎名：`set_font`

### SetGlyphCount

```csharp
public void SetGlyphCount(byte glyphCount)
```

引擎名：`set_glyph_count`

### SetGlyphFlags

```csharp
public void SetGlyphFlags(ushort glyphFlags)
```

引擎名：`set_glyph_flags`

### SetGlyphIndex

```csharp
public void SetGlyphIndex(uint glyphIndex)
```

引擎名：`set_glyph_index`

### SetOffset

```csharp
public void SetOffset(Vector2 offset)
```

引擎名：`set_offset`

### SetOutline

```csharp
public void SetOutline(bool outline)
```

引擎名：`set_outline`

### SetRange

```csharp
public void SetRange(Vector2I range)
```

引擎名：`set_range`

### SetRelativeIndex

```csharp
public void SetRelativeIndex(int relativeIndex)
```

引擎名：`set_relative_index`

### SetTransform

```csharp
public void SetTransform(Transform2D transform)
```

引擎名：`set_transform`

### SetVisibility

```csharp
public void SetVisibility(bool visibility)
```

引擎名：`set_visibility`
