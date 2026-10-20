<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Label

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A control for displaying plain text. It gives you control over the horizontal and vertical alignment and can wrap the text inside the node's bounding rectangle. It doesn't support bold, italics, or other rich text formatting. For that, use [`RichTextLabel`](RichTextLabel.md) instead.

**Note:** A single Label node is not designed to display huge amounts of text. To display large amounts of text in a single node, consider using [`RichTextLabel`](RichTextLabel.md) instead as it supports features like an integrated scroll bar and threading. [`RichTextLabel`](RichTextLabel.md) generally performs better when displaying large amounts of text (several pages or more).

## 构造函数

### Label

```csharp
public Label()
```

## 属性

### AutowrapMode

```csharp
public TextServer.AutowrapMode AutowrapMode { get; set; }
```

引擎名：`autowrap_mode`

If set to something other than [`TextServer.AutowrapMode.Off`](TextServer.md), the text gets wrapped inside the node's bounding rectangle. If you resize the node, it will change its height automatically to show all the text.

**Note:** Labels with autowrapping enabled must have a custom maximum width configured to work correctly, either through the Label's own [`Control.CustomMaximumSize`](Control.md) or as a result of a propagated maximum size from a parent Control with [`Control.PropagateMaximumSize`](Control.md) enabled.

### AutowrapTrimFlags

```csharp
public TextServer.LineBreakFlag AutowrapTrimFlags { get; set; }
```

引擎名：`autowrap_trim_flags`

Autowrap space trimming flags. See [`TextServer.LineBreakFlag.TrimStartEdgeSpaces`](TextServer.md) and [`TextServer.LineBreakFlag.TrimEndEdgeSpaces`](TextServer.md) for more info.

### ClipText

```csharp
public bool ClipText { get; set; }
```

引擎名：`clip_text`

If `true`, the Label only shows the text that fits inside its bounding rectangle and will clip text horizontally.

### EllipsisChar

```csharp
public string EllipsisChar { get; set; }
```

引擎名：`ellipsis_char`

Ellipsis character used for text clipping.

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

### LabelSettings

```csharp
public LabelSettings LabelSettings { get; set; }
```

引擎名：`label_settings`

A [`LabelSettings`](LabelSettings.md) resource that can be shared between multiple `Label` nodes. Takes priority over theme properties.

### Language

```csharp
public string Language { get; set; }
```

引擎名：`language`

Language code used for line-breaking and text shaping algorithms. If left empty, the current locale is used instead.

### LinesSkipped

```csharp
public int LinesSkipped { get; set; }
```

引擎名：`lines_skipped`

The number of the lines ignored and not displayed from the start of the `Text` value.

### MaxLinesVisible

```csharp
public int MaxLinesVisible { get; set; }
```

引擎名：`max_lines_visible`

Limits the lines of text the node shows on screen.

### ParagraphSeparator

```csharp
public string ParagraphSeparator { get; set; }
```

引擎名：`paragraph_separator`

String used as a paragraph separator. Each paragraph is processed independently, in its own BiDi context.

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

### TabStops

```csharp
public float[] TabStops { get; set; }
```

引擎名：`tab_stops`

Aligns text to the given tab-stops.

### Text

```csharp
public string Text { get; set; }
```

引擎名：`text`

The text to display on screen.

### TextDirection

```csharp
public Control.TextDirection TextDirection { get; set; }
```

引擎名：`text_direction`

Base text writing direction.

### TextOverrunBehavior

```csharp
public TextServer.OverrunBehavior TextOverrunBehavior { get; set; }
```

引擎名：`text_overrun_behavior`

The clipping behavior when the text exceeds the node's bounding rectangle.

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

Controls the text's vertical alignment. Supports top, center, bottom, and fill.

### VisibleCharacters

```csharp
public int VisibleCharacters { get; set; }
```

引擎名：`visible_characters`

The number of characters to display. If set to `-1`, all characters are displayed. This can be useful when animating the text appearing in a dialog box.

**Note:** Setting this property updates `VisibleRatio` accordingly.

**Note:** Characters are counted as Unicode codepoints. A single visible grapheme may contain multiple codepoints (e.g. certain emoji use three codepoints). A single codepoint may contain two UTF-16 characters, which are used in C# strings.

### VisibleCharactersBehavior

```csharp
public TextServer.VisibleCharactersBehavior VisibleCharactersBehavior { get; set; }
```

引擎名：`visible_characters_behavior`

The clipping behavior when `VisibleCharacters` or `VisibleRatio` is set.

### VisibleRatio

```csharp
public float VisibleRatio { get; set; }
```

引擎名：`visible_ratio`

The fraction of characters to display, relative to the total number of characters (see `GetTotalCharacterCount()`). If set to `1.0`, all characters are displayed. If set to `0.5`, only half of the characters will be displayed. This can be useful when animating the text appearing in a dialog box.

**Note:** Setting this property updates `VisibleCharacters` accordingly.

## 方法

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

### GetCharacterBounds

```csharp
public Rect2 GetCharacterBounds(int pos)
```

引擎名：`get_character_bounds`

Returns the bounding rectangle of the character at position `pos` in the label's local coordinate system. If the character is a non-visual character or `pos` is outside the valid range, an empty [`Rect2`](Rect2.md) is returned. If the character is a part of a composite grapheme, the bounding rectangle of the whole grapheme is returned.

### GetEllipsisChar

```csharp
public string GetEllipsisChar()
```

引擎名：`get_ellipsis_char`

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

### GetLabelSettings

```csharp
public LabelSettings GetLabelSettings()
```

引擎名：`get_label_settings`

### GetLanguage

```csharp
public string GetLanguage()
```

引擎名：`get_language`

### GetLineCount

```csharp
public int GetLineCount()
```

引擎名：`get_line_count`

Returns the number of lines of text the Label has.

### GetLineHeight

```csharp
public int GetLineHeight(int line = -1)
```

引擎名：`get_line_height`

Returns the height of the line `line`.

If `line` is set to `-1`, returns the biggest line height.

If there are no lines, returns font size in pixels.

### GetLinesSkipped

```csharp
public int GetLinesSkipped()
```

引擎名：`get_lines_skipped`

### GetMaxLinesVisible

```csharp
public int GetMaxLinesVisible()
```

引擎名：`get_max_lines_visible`

### GetParagraphSeparator

```csharp
public string GetParagraphSeparator()
```

引擎名：`get_paragraph_separator`

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

### GetTabStops

```csharp
public float[] GetTabStops()
```

引擎名：`get_tab_stops`

### GetText

```csharp
public string GetText()
```

引擎名：`get_text`

### GetTextDirection

```csharp
public Control.TextDirection GetTextDirection()
```

引擎名：`get_text_direction`

### GetTextOverrunBehavior

```csharp
public TextServer.OverrunBehavior GetTextOverrunBehavior()
```

引擎名：`get_text_overrun_behavior`

### GetTotalCharacterCount

```csharp
public int GetTotalCharacterCount()
```

引擎名：`get_total_character_count`

Returns the total number of printable characters in the text (excluding spaces and newlines).

### GetVerticalAlignment

```csharp
public VerticalAlignment GetVerticalAlignment()
```

引擎名：`get_vertical_alignment`

### GetVisibleCharacters

```csharp
public int GetVisibleCharacters()
```

引擎名：`get_visible_characters`

### GetVisibleCharactersBehavior

```csharp
public TextServer.VisibleCharactersBehavior GetVisibleCharactersBehavior()
```

引擎名：`get_visible_characters_behavior`

### GetVisibleLineCount

```csharp
public int GetVisibleLineCount()
```

引擎名：`get_visible_line_count`

Returns the number of lines shown. Useful if the `Label`'s height cannot currently display all lines.

### GetVisibleRatio

```csharp
public float GetVisibleRatio()
```

引擎名：`get_visible_ratio`

### IsClippingText

```csharp
public bool IsClippingText()
```

引擎名：`is_clipping_text`

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

### SetAutowrapTrimFlags

```csharp
public void SetAutowrapTrimFlags(TextServer.LineBreakFlag autowrapTrimFlags)
```

引擎名：`set_autowrap_trim_flags`

### SetClipText

```csharp
public void SetClipText(bool enable)
```

引擎名：`set_clip_text`

### SetEllipsisChar

```csharp
public void SetEllipsisChar(string char)
```

引擎名：`set_ellipsis_char`

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

### SetLabelSettings

```csharp
public void SetLabelSettings(LabelSettings settings)
```

引擎名：`set_label_settings`

### SetLanguage

```csharp
public void SetLanguage(string language)
```

引擎名：`set_language`

### SetLinesSkipped

```csharp
public void SetLinesSkipped(int linesSkipped)
```

引擎名：`set_lines_skipped`

### SetMaxLinesVisible

```csharp
public void SetMaxLinesVisible(int linesVisible)
```

引擎名：`set_max_lines_visible`

### SetParagraphSeparator

```csharp
public void SetParagraphSeparator(string paragraphSeparator)
```

引擎名：`set_paragraph_separator`

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

### SetTabStops

```csharp
public void SetTabStops(ReadOnlySpan<float> tabStops)
```

引擎名：`set_tab_stops`

### SetTabStops

```csharp
public void SetTabStops(float[] tabStops)
```

引擎名：`set_tab_stops`

### SetText

```csharp
public void SetText(string text)
```

引擎名：`set_text`

### SetTextDirection

```csharp
public void SetTextDirection(Control.TextDirection direction)
```

引擎名：`set_text_direction`

### SetTextOverrunBehavior

```csharp
public void SetTextOverrunBehavior(TextServer.OverrunBehavior overrunBehavior)
```

引擎名：`set_text_overrun_behavior`

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

### SetVisibleCharacters

```csharp
public void SetVisibleCharacters(int amount)
```

引擎名：`set_visible_characters`

### SetVisibleCharactersBehavior

```csharp
public void SetVisibleCharactersBehavior(TextServer.VisibleCharactersBehavior behavior)
```

引擎名：`set_visible_characters_behavior`

### SetVisibleRatio

```csharp
public void SetVisibleRatio(float ratio)
```

引擎名：`set_visible_ratio`
