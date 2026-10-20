<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextParagraph

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Abstraction over [`TextServer`](TextServer.md) for handling a single paragraph of text.

## 构造函数

### TextParagraph

```csharp
public TextParagraph()
```

## 属性

### Alignment

```csharp
public HorizontalAlignment Alignment { get; set; }
```

引擎名：`alignment`

Paragraph horizontal alignment.

### BreakFlags

```csharp
public TextServer.LineBreakFlag BreakFlags { get; set; }
```

引擎名：`break_flags`

Line breaking rules. For more info see [`TextServer`](TextServer.md).

### CustomPunctuation

```csharp
public string CustomPunctuation { get; set; }
```

引擎名：`custom_punctuation`

Custom punctuation character list, used for word breaking. If set to empty string, server defaults are used.

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

### JustificationFlags

```csharp
public TextServer.JustificationFlag JustificationFlags { get; set; }
```

引擎名：`justification_flags`

Line fill alignment rules.

### LineSpacing

```csharp
public float LineSpacing { get; set; }
```

引擎名：`line_spacing`

Additional vertical spacing between lines (in pixels), spacing is added to line descent. This value can be negative.

### MaxLinesVisible

```csharp
public int MaxLinesVisible { get; set; }
```

引擎名：`max_lines_visible`

Limits the lines of text shown.

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

The clipping behavior when the text exceeds the paragraph's set width.

### Width

```csharp
public float Width { get; set; }
```

引擎名：`width`

Paragraph width.

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

Clears text paragraph (removes text and inline objects).

### ClearDropcap

```csharp
public void ClearDropcap()
```

引擎名：`clear_dropcap`

Removes dropcap.

### Draw

```csharp
public void Draw(Rid canvas, Vector2 pos, Color? color = null, Color? dcColor = null, float oversampling = 0f)
```

引擎名：`draw`

Draw all lines of the text and drop cap into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `dcColor`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### Draw

```csharp
public void Draw(Rid canvas, Vector2 pos, Color? color, Color? dcColor)
```

引擎名：`draw`

Draw all lines of the text and drop cap into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `dcColor`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawDropcap

```csharp
public void DrawDropcap(Rid canvas, Vector2 pos, Color? color = null, float oversampling = 0f)
```

引擎名：`draw_dropcap`

Draw drop cap into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawDropcap

```csharp
public void DrawDropcap(Rid canvas, Vector2 pos, Color? color)
```

引擎名：`draw_dropcap`

Draw drop cap into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawDropcapOutline

```csharp
public void DrawDropcapOutline(Rid canvas, Vector2 pos, int outlineSize = 1, Color? color = null, float oversampling = 0f)
```

引擎名：`draw_dropcap_outline`

Draw drop cap outline into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawDropcapOutline

```csharp
public void DrawDropcapOutline(Rid canvas, Vector2 pos, int outlineSize, Color? color)
```

引擎名：`draw_dropcap_outline`

Draw drop cap outline into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawLine

```csharp
public void DrawLine(Rid canvas, Vector2 pos, int line, Color? color = null, float oversampling = 0f)
```

引擎名：`draw_line`

Draw single line of text into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawLine

```csharp
public void DrawLine(Rid canvas, Vector2 pos, int line, Color? color)
```

引擎名：`draw_line`

Draw single line of text into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawLineOutline

```csharp
public void DrawLineOutline(Rid canvas, Vector2 pos, int line, int outlineSize = 1, Color? color = null, float oversampling = 0f)
```

引擎名：`draw_line_outline`

Draw outline of the single line of text into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawLineOutline

```csharp
public void DrawLineOutline(Rid canvas, Vector2 pos, int line, int outlineSize, Color? color)
```

引擎名：`draw_line_outline`

Draw outline of the single line of text into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawOutline

```csharp
public void DrawOutline(Rid canvas, Vector2 pos, int outlineSize = 1, Color? color = null, Color? dcColor = null, float oversampling = 0f)
```

引擎名：`draw_outline`

Draw outlines of all lines of the text and drop cap into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `dcColor`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawOutline

```csharp
public void DrawOutline(Rid canvas, Vector2 pos, int outlineSize, Color? color, Color? dcColor)
```

引擎名：`draw_outline`

Draw outlines of all lines of the text and drop cap into a canvas item at a given position, with `color`. `pos` specifies the top left corner of the bounding box. If `oversampling` is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `dcColor`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### Duplicate

```csharp
public TextParagraph Duplicate()
```

引擎名：`duplicate`

Duplicates this `TextParagraph`.

### GetAlignment

```csharp
public HorizontalAlignment GetAlignment()
```

引擎名：`get_alignment`

### GetBreakFlags

```csharp
public TextServer.LineBreakFlag GetBreakFlags()
```

引擎名：`get_break_flags`

### GetCustomPunctuation

```csharp
public string GetCustomPunctuation()
```

引擎名：`get_custom_punctuation`

### GetDirection

```csharp
public TextServer.Direction GetDirection()
```

引擎名：`get_direction`

### GetDropcapLines

```csharp
public int GetDropcapLines()
```

引擎名：`get_dropcap_lines`

Returns number of lines used by dropcap.

### GetDropcapRid

```csharp
public Rid GetDropcapRid()
```

引擎名：`get_dropcap_rid`

Returns drop cap text buffer RID.

### GetDropcapSize

```csharp
public Vector2 GetDropcapSize()
```

引擎名：`get_dropcap_size`

Returns drop cap bounding box size.

### GetEllipsisChar

```csharp
public string GetEllipsisChar()
```

引擎名：`get_ellipsis_char`

### GetInferredDirection

```csharp
public TextServer.Direction GetInferredDirection()
```

引擎名：`get_inferred_direction`

Returns the text writing direction inferred by the BiDi algorithm.

### GetJustificationFlags

```csharp
public TextServer.JustificationFlag GetJustificationFlags()
```

引擎名：`get_justification_flags`

### GetLineAscent

```csharp
public float GetLineAscent(int line)
```

引擎名：`get_line_ascent`

Returns the text line ascent (number of pixels above the baseline for horizontal layout or to the left of baseline for vertical).

### GetLineCount

```csharp
public int GetLineCount()
```

引擎名：`get_line_count`

Returns number of lines in the paragraph.

### GetLineDescent

```csharp
public float GetLineDescent(int line)
```

引擎名：`get_line_descent`

Returns the text line descent (number of pixels below the baseline for horizontal layout or to the right of baseline for vertical).

### GetLineObjectRect

```csharp
public Rect2 GetLineObjectRect(int line, Variant key)
```

引擎名：`get_line_object_rect`

Returns bounding rectangle of the inline object.

### GetLineObjects

```csharp
public Array GetLineObjects(int line)
```

引擎名：`get_line_objects`

Returns array of inline objects in the line.

### GetLineRange

```csharp
public Vector2I GetLineRange(int line)
```

引擎名：`get_line_range`

Returns character range of the line.

### GetLineRid

```csharp
public Rid GetLineRid(int line)
```

引擎名：`get_line_rid`

Returns TextServer line buffer RID.

### GetLineSize

```csharp
public Vector2 GetLineSize(int line)
```

引擎名：`get_line_size`

Returns size of the bounding box of the line of text. Returned size is rounded up.

### GetLineSpacing

```csharp
public float GetLineSpacing()
```

引擎名：`get_line_spacing`

### GetLineUnderlinePosition

```csharp
public float GetLineUnderlinePosition(int line)
```

引擎名：`get_line_underline_position`

Returns pixel offset of the underline below the baseline.

### GetLineUnderlineThickness

```csharp
public float GetLineUnderlineThickness(int line)
```

引擎名：`get_line_underline_thickness`

Returns thickness of the underline.

### GetLineWidth

```csharp
public float GetLineWidth(int line)
```

引擎名：`get_line_width`

Returns width (for horizontal layout) or height (for vertical) of the line of text.

### GetMaxLinesVisible

```csharp
public int GetMaxLinesVisible()
```

引擎名：`get_max_lines_visible`

### GetNonWrappedSize

```csharp
public Vector2 GetNonWrappedSize()
```

引擎名：`get_non_wrapped_size`

Returns the size of the bounding box of the paragraph, without line breaks.

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

### GetRange

```csharp
public Vector2I GetRange()
```

引擎名：`get_range`

Returns the character range of the paragraph.

### GetRid

```csharp
public Rid GetRid()
```

引擎名：`get_rid`

Returns TextServer full string buffer RID.

### GetSize

```csharp
public Vector2 GetSize()
```

引擎名：`get_size`

Returns the size of the bounding box of the paragraph.

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

Returns `true` if an object with `key` is embedded in this shaped text buffer.

### HitTest

```csharp
public int HitTest(Vector2 coords)
```

引擎名：`hit_test`

Returns caret character offset at the specified coordinates. This function always returns a valid position.

### ResizeObject

```csharp
public bool ResizeObject(Variant key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, float baseline = 0f)
```

引擎名：`resize_object`

Sets new size and alignment of embedded object.

### SetAlignment

```csharp
public void SetAlignment(HorizontalAlignment alignment)
```

引擎名：`set_alignment`

### SetBidiOverride

```csharp
public void SetBidiOverride(Array override)
```

引擎名：`set_bidi_override`

Overrides BiDi for the structured text.

Override ranges should cover full source text without overlaps. BiDi algorithm will be used on each range separately.

### SetBreakFlags

```csharp
public void SetBreakFlags(TextServer.LineBreakFlag flags)
```

引擎名：`set_break_flags`

### SetCustomPunctuation

```csharp
public void SetCustomPunctuation(string customPunctuation)
```

引擎名：`set_custom_punctuation`

### SetDirection

```csharp
public void SetDirection(TextServer.Direction direction)
```

引擎名：`set_direction`

### SetDropcap

```csharp
public bool SetDropcap(string text, Font font, int fontSize, Rect2? dropcapMargins = null, string language = "")
```

引擎名：`set_dropcap`

Sets drop cap, overrides previously set drop cap. Drop cap (dropped capital) is a decorative element at the beginning of a paragraph that is larger than the rest of the text.

参数：
- `dropcapMargins`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### SetEllipsisChar

```csharp
public void SetEllipsisChar(string char)
```

引擎名：`set_ellipsis_char`

### SetJustificationFlags

```csharp
public void SetJustificationFlags(TextServer.JustificationFlag flags)
```

引擎名：`set_justification_flags`

### SetLineSpacing

```csharp
public void SetLineSpacing(float lineSpacing)
```

引擎名：`set_line_spacing`

### SetMaxLinesVisible

```csharp
public void SetMaxLinesVisible(int maxLinesVisible)
```

引擎名：`set_max_lines_visible`

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

Aligns paragraph to the given tab-stops.

### TabAlign

```csharp
public void TabAlign(float[] tabStops)
```

引擎名：`tab_align`

Aligns paragraph to the given tab-stops.
