<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextLine

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Abstraction over [`TextServer`](TextServer.md) for handling a single line of text.

## 构造函数

### TextLine

```csharp
public TextLine()
```

## 属性

### Alignment

```csharp
public HorizontalAlignment Alignment { get; set; }
```

引擎名：`alignment`

Sets text alignment within the line as if the line was horizontal.

### Direction

```csharp
public TextServer.Direction Direction { get; set; }
```

引擎名：`direction`

Text writing direction.

### EllipsisChar

```csharp
public string EllipsisChar { get; set; }
```

引擎名：`ellipsis_char`

Ellipsis character used for text clipping.

### Flags

```csharp
public TextServer.JustificationFlag Flags { get; set; }
```

引擎名：`flags`

Line alignment rules. For more info see [`TextServer`](TextServer.md).

### Orientation

```csharp
public TextServer.Orientation Orientation { get; set; }
```

引擎名：`orientation`

Text orientation.

### PreserveControl

```csharp
public bool PreserveControl { get; set; }
```

引擎名：`preserve_control`

If set to `true` text will display control characters.

### PreserveInvalid

```csharp
public bool PreserveInvalid { get; set; }
```

引擎名：`preserve_invalid`

If set to `true` text will display invalid characters.

### TextOverrunBehavior

```csharp
public TextServer.OverrunBehavior TextOverrunBehavior { get; set; }
```

引擎名：`text_overrun_behavior`

The clipping behavior when the text exceeds the text line's set width.

### Width

```csharp
public float Width { get; set; }
```

引擎名：`width`

Text line width.

## 方法

### AddObject

```csharp
public bool AddObject(Variant key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, int length = 1, float baseline = 0f)
```

引擎名：`add_object`

Adds inline object to the text buffer, `key` must be unique. In the text, object is represented as `length` object replacement characters.

### AddString

```csharp
public bool AddString(string text, Font font, int fontSize, string language = "", Variant meta = default)
```

引擎名：`add_string`

Adds text span and font to draw it.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears text line (removes text and inline objects).

### Draw

```csharp
public void Draw(Rid canvas, Vector2 pos, Color? color = null, float oversampling = 0f)
```

引擎名：`draw`

Draw text into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### Draw

```csharp
public void Draw(Rid canvas, Vector2 pos, Color? color)
```

引擎名：`draw`

Draw text into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawOutline

```csharp
public void DrawOutline(Rid canvas, Vector2 pos, int outlineSize = 1, Color? color = null, float oversampling = 0f)
```

引擎名：`draw_outline`

Draw text into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawOutline

```csharp
public void DrawOutline(Rid canvas, Vector2 pos, int outlineSize, Color? color)
```

引擎名：`draw_outline`

Draw text into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### Duplicate

```csharp
public TextLine Duplicate()
```

引擎名：`duplicate`

Duplicates this `TextLine`.

### GetDirection

```csharp
public TextServer.Direction GetDirection()
```

引擎名：`get_direction`

### GetEllipsisChar

```csharp
public string GetEllipsisChar()
```

引擎名：`get_ellipsis_char`

### GetFlags

```csharp
public TextServer.JustificationFlag GetFlags()
```

引擎名：`get_flags`

### GetHorizontalAlignment

```csharp
public HorizontalAlignment GetHorizontalAlignment()
```

引擎名：`get_horizontal_alignment`

### GetInferredDirection

```csharp
public TextServer.Direction GetInferredDirection()
```

引擎名：`get_inferred_direction`

Returns the text writing direction inferred by the BiDi algorithm.

### GetLineAscent

```csharp
public float GetLineAscent()
```

引擎名：`get_line_ascent`

Returns the text ascent (number of pixels above the baseline for horizontal layout or to the left of baseline for vertical).

### GetLineDescent

```csharp
public float GetLineDescent()
```

引擎名：`get_line_descent`

Returns the text descent (number of pixels below the baseline for horizontal layout or to the right of baseline for vertical).

### GetLineUnderlinePosition

```csharp
public float GetLineUnderlinePosition()
```

引擎名：`get_line_underline_position`

Returns pixel offset of the underline below the baseline.

### GetLineUnderlineThickness

```csharp
public float GetLineUnderlineThickness()
```

引擎名：`get_line_underline_thickness`

Returns thickness of the underline.

### GetLineWidth

```csharp
public float GetLineWidth()
```

引擎名：`get_line_width`

Returns width (for horizontal layout) or height (for vertical) of the text.

### GetObjectRect

```csharp
public Rect2 GetObjectRect(Variant key)
```

引擎名：`get_object_rect`

Returns bounding rectangle of the inline object.

### GetObjects

```csharp
public Array GetObjects()
```

引擎名：`get_objects`

Returns array of inline objects.

### GetOrientation

```csharp
public TextServer.Orientation GetOrientation()
```

引擎名：`get_orientation`

### GetPreserveControl

```csharp
public bool GetPreserveControl()
```

引擎名：`get_preserve_control`

### GetPreserveInvalid

```csharp
public bool GetPreserveInvalid()
```

引擎名：`get_preserve_invalid`

### GetRid

```csharp
public Rid GetRid()
```

引擎名：`get_rid`

Returns TextServer buffer RID.

### GetSize

```csharp
public Vector2 GetSize()
```

引擎名：`get_size`

Returns size of the bounding box of the text.

### GetTextOverrunBehavior

```csharp
public TextServer.OverrunBehavior GetTextOverrunBehavior()
```

引擎名：`get_text_overrun_behavior`

### GetWidth

```csharp
public float GetWidth()
```

引擎名：`get_width`

### HasObject

```csharp
public bool HasObject(Variant key)
```

引擎名：`has_object`

Returns `true` if an object with `key` is embedded in this line.

### HitTest

```csharp
public int HitTest(float coords)
```

引擎名：`hit_test`

Returns caret character offset at the specified pixel offset at the baseline. This function always returns a valid position.

### ResizeObject

```csharp
public bool ResizeObject(Variant key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, float baseline = 0f)
```

引擎名：`resize_object`

Sets new size and alignment of embedded object.

### SetBidiOverride

```csharp
public void SetBidiOverride(Array override)
```

引擎名：`set_bidi_override`

Overrides BiDi for the structured text.

Override ranges should cover full source text without overlaps. BiDi algorithm will be used on each range separately.

### SetDirection

```csharp
public void SetDirection(TextServer.Direction direction)
```

引擎名：`set_direction`

### SetEllipsisChar

```csharp
public void SetEllipsisChar(string char)
```

引擎名：`set_ellipsis_char`

### SetFlags

```csharp
public void SetFlags(TextServer.JustificationFlag flags)
```

引擎名：`set_flags`

### SetHorizontalAlignment

```csharp
public void SetHorizontalAlignment(HorizontalAlignment alignment)
```

引擎名：`set_horizontal_alignment`

### SetOrientation

```csharp
public void SetOrientation(TextServer.Orientation orientation)
```

引擎名：`set_orientation`

### SetPreserveControl

```csharp
public void SetPreserveControl(bool enabled)
```

引擎名：`set_preserve_control`

### SetPreserveInvalid

```csharp
public void SetPreserveInvalid(bool enabled)
```

引擎名：`set_preserve_invalid`

### SetTextOverrunBehavior

```csharp
public void SetTextOverrunBehavior(TextServer.OverrunBehavior overrunBehavior)
```

引擎名：`set_text_overrun_behavior`

### SetWidth

```csharp
public void SetWidth(float width)
```

引擎名：`set_width`

### TabAlign

```csharp
public void TabAlign(ReadOnlySpan<float> tabStops)
```

引擎名：`tab_align`

Aligns text to the given tab-stops.

### TabAlign

```csharp
public void TabAlign(float[] tabStops)
```

引擎名：`tab_align`

Aligns text to the given tab-stops.
