<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RichTextLabel

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A control for displaying text that can contain custom fonts, images, and basic formatting. `RichTextLabel` manages these as an internal tag stack. It also adapts itself to given width/heights.

**Note:** `Newline()`, `PushParagraph()`, `"\n"`, `"\r\n"`, `p` tag, and alignment tags start a new paragraph. Each paragraph is processed independently, in its own BiDi context. If you want to force line wrapping within paragraph, any other line breaking character can be used, for example, Form Feed (U+000C), Next Line (U+0085), Line Separator (U+2028).

**Note:** Assignments to `Text` clear the tag stack and reconstruct it from the property's contents. Any edits made to `Text` will erase previous edits made from other manual sources such as `AppendText()` and the `push_*` / `Pop()` methods.

**Note:** RichTextLabel doesn't support entangled BBCode tags. For example, instead of using `[b]bold[i]bold italic[/b]italic[/i]`, use `[b]bold[i]bold italic[/i][/b][i]italic[/i]`.

**Note:** `push_*/pop_*` functions won't affect BBCode.

**Note:** While `BbcodeEnabled` is enabled, alignment tags such as `[center]` will take priority over the `HorizontalAlignment` setting which determines the default text alignment.

## 构造函数

### RichTextLabel

```csharp
public RichTextLabel()
```

## 属性

### AutowrapMode

```csharp
public TextServer.AutowrapMode AutowrapMode { get; set; }
```

引擎名：`autowrap_mode`

If set to something other than [`TextServer.AutowrapMode.Off`](TextServer.md), the text gets wrapped inside the node's bounding rectangle.

**Note:** RichTextLabels with autowrapping and `FitContent` enabled must have a custom maximum width configured to work correctly, either through the RichTextLabel's own [`Control.CustomMaximumSize`](Control.md) or as a result of a propagated maximum size from a parent Control with [`Control.PropagateMaximumSize`](Control.md) enabled.

### AutowrapTrimFlags

```csharp
public TextServer.LineBreakFlag AutowrapTrimFlags { get; set; }
```

引擎名：`autowrap_trim_flags`

Autowrap space trimming flags. See [`TextServer.LineBreakFlag.TrimStartEdgeSpaces`](TextServer.md) and [`TextServer.LineBreakFlag.TrimEndEdgeSpaces`](TextServer.md) for more info.

### BbcodeEnabled

```csharp
public bool BbcodeEnabled { get; set; }
```

引擎名：`bbcode_enabled`

If `true`, the label uses BBCode formatting.

**Note:** This only affects the contents of `Text`, not the tag stack.

### ContextMenuEnabled

```csharp
public bool ContextMenuEnabled { get; set; }
```

引擎名：`context_menu_enabled`

If `true`, a right-click displays the context menu.

### CustomEffects

```csharp
public Array CustomEffects { get; set; }
```

引擎名：`custom_effects`

The currently installed custom effects. This is an array of [`RichTextEffect`](RichTextEffect.md)s.

To add a custom effect, it's more convenient to use `InstallEffect()`.

### DeselectOnFocusLossEnabled

```csharp
public bool DeselectOnFocusLossEnabled { get; set; }
```

引擎名：`deselect_on_focus_loss_enabled`

If `true`, the selected text will be deselected when focus is lost.

### DragAndDropSelectionEnabled

```csharp
public bool DragAndDropSelectionEnabled { get; set; }
```

引擎名：`drag_and_drop_selection_enabled`

If `true`, allow drag and drop of selected text.

### FitContent

```csharp
public bool FitContent { get; set; }
```

引擎名：`fit_content`

If `true`, the label's minimum size will be automatically updated to fit its content, matching the behavior of [`Label`](Label.md).

**Note:** RichTextLabels with autowrapping and `FitContent` enabled must have a custom maximum width configured to work correctly, either through the RichTextLabel's own [`Control.CustomMaximumSize`](Control.md) or as a result of a propagated maximum size from a parent Control with [`Control.PropagateMaximumSize`](Control.md) enabled.

### HintUnderlined

```csharp
public bool HintUnderlined { get; set; }
```

引擎名：`hint_underlined`

If `true`, the label underlines hint tags such as `[hint=description]{text}[/hint]`.

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

### MetaUnderlined

```csharp
public bool MetaUnderlined { get; set; }
```

引擎名：`meta_underlined`

If `true`, the label underlines meta tags such as `[url]{text}[/url]`. These tags can call a function when clicked if `MetaClicked` is connected to a function.

### ProgressBarDelay

```csharp
public int ProgressBarDelay { get; set; }
```

引擎名：`progress_bar_delay`

The delay after which the loading progress bar is displayed, in milliseconds. Set to `-1` to disable progress bar entirely.

**Note:** Progress bar is displayed only if `Threaded` is enabled.

### ScrollActive

```csharp
public bool ScrollActive { get; set; }
```

引擎名：`scroll_active`

If `true`, the scrollbar is visible. Setting this to `false` does not block scrolling completely. See `ScrollToLine()`.

### ScrollFollowing

```csharp
public bool ScrollFollowing { get; set; }
```

引擎名：`scroll_following`

If `true`, the window scrolls down to display new content automatically.

### ScrollFollowingVisibleCharacters

```csharp
public bool ScrollFollowingVisibleCharacters { get; set; }
```

引擎名：`scroll_following_visible_characters`

If `true`, the window scrolls to display the last visible line when `VisibleCharacters` or `VisibleRatio` is changed.

### SelectionEnabled

```csharp
public bool SelectionEnabled { get; set; }
```

引擎名：`selection_enabled`

If `true`, the label allows text selection.

### ShortcutKeysEnabled

```csharp
public bool ShortcutKeysEnabled { get; set; }
```

引擎名：`shortcut_keys_enabled`

If `true`, shortcut keys for context menu items are enabled, even if the context menu is disabled.

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

### TabSize

```csharp
public int TabSize { get; set; }
```

引擎名：`tab_size`

The number of spaces associated with a single tab length. Does not affect `\t` in text tags, only indent tags.

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

The label's text in BBCode format. Is not representative of manual modifications to the internal tag stack. Erases changes made by other methods when edited.

**Note:** If `BbcodeEnabled` is `true`, it is unadvised to use the `+=` operator with `Text` (e.g. `text += "some string"`) as it replaces the whole text and can cause slowdowns. It will also erase all BBCode that was added to stack using `push_*` methods. Use `AppendText()` for adding text instead, unless you absolutely need to close a tag that was opened in an earlier method call.

### TextDirection

```csharp
public Control.TextDirection TextDirection { get; set; }
```

引擎名：`text_direction`

Base text writing direction.

### Threaded

```csharp
public bool Threaded { get; set; }
```

引擎名：`threaded`

If `true`, text processing is done in a background thread.

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

### AddHr

```csharp
public void AddHr(int width = 90, int height = 2, Color? color = null, HorizontalAlignment alignment = HorizontalAlignment.Center, bool widthInPercent = true, bool heightInPercent = false)
```

引擎名：`add_hr`

Adds a horizontal rule that can be used to separate content.

If `widthInPercent` is set, `width` values are percentages of the control width instead of pixels.

If `heightInPercent` is set, `height` values are percentages of the control width instead of pixels.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### AddImage

```csharp
public void AddImage(Texture2D image, float width = 0f, float height = 0f, Color? color = null, InlineAlignment inlineAlign = InlineAlignment.Center, Rect2? region = null, Variant key = default, bool pad = false, string tooltip = "", RichTextLabel.ImageUnit widthUnit = RichTextLabel.ImageUnit.Pixel, RichTextLabel.ImageUnit heightUnit = RichTextLabel.ImageUnit.Pixel, string altText = "")
```

引擎名：`add_image`

Adds an image's opening and closing tags to the tag stack, optionally providing a `width` and `height` to resize the image, a `color` to tint the image and a `region` to only use parts of the image.

If `width` or `height` is set to 0, the image size will be adjusted in order to keep the original aspect ratio.

If `width` and `height` are not set, but `region` is, the region's rect will be used.

`key` is an optional identifier, that can be used to modify the image via `UpdateImage()`.

If `pad` is set, and the image is smaller than the size specified by `width` and `height`, the image padding is added to match the size instead of upscaling.

Parameters `widthUnit` and `heightUnit` determine the units used to calculate the image width and height, respectively.

`altText` is used as the image description for assistive apps.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### AddImage

```csharp
public void AddImage(Texture2D image, int width, int height, Color? color, InlineAlignment inlineAlign, Rect2? region)
```

引擎名：`add_image`

Adds an image's opening and closing tags to the tag stack, optionally providing a `width` and `height` to resize the image, a `color` to tint the image and a `region` to only use parts of the image.

If `width` or `height` is set to 0, the image size will be adjusted in order to keep the original aspect ratio.

If `width` and `height` are not set, but `region` is, the region's rect will be used.

`key` is an optional identifier, that can be used to modify the image via `UpdateImage()`.

If `pad` is set, and the image is smaller than the size specified by `width` and `height`, the image padding is added to match the size instead of upscaling.

Parameters `widthUnit` and `heightUnit` determine the units used to calculate the image width and height, respectively.

`altText` is used as the image description for assistive apps.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### AddImage

```csharp
public void AddImage(Texture2D image, int width, int height, Color? color, InlineAlignment inlineAlign, Rect2? region, Variant key, bool pad, string tooltip, bool sizeInPercent)
```

引擎名：`add_image`

Adds an image's opening and closing tags to the tag stack, optionally providing a `width` and `height` to resize the image, a `color` to tint the image and a `region` to only use parts of the image.

If `width` or `height` is set to 0, the image size will be adjusted in order to keep the original aspect ratio.

If `width` and `height` are not set, but `region` is, the region's rect will be used.

`key` is an optional identifier, that can be used to modify the image via `UpdateImage()`.

If `pad` is set, and the image is smaller than the size specified by `width` and `height`, the image padding is added to match the size instead of upscaling.

Parameters `widthUnit` and `heightUnit` determine the units used to calculate the image width and height, respectively.

`altText` is used as the image description for assistive apps.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### AddImage

```csharp
public void AddImage(Texture2D image, int width, int height, Color? color, InlineAlignment inlineAlign, Rect2? region, Variant key, bool pad, string tooltip, bool sizeInPercent, string altText)
```

引擎名：`add_image`

Adds an image's opening and closing tags to the tag stack, optionally providing a `width` and `height` to resize the image, a `color` to tint the image and a `region` to only use parts of the image.

If `width` or `height` is set to 0, the image size will be adjusted in order to keep the original aspect ratio.

If `width` and `height` are not set, but `region` is, the region's rect will be used.

`key` is an optional identifier, that can be used to modify the image via `UpdateImage()`.

If `pad` is set, and the image is smaller than the size specified by `width` and `height`, the image padding is added to match the size instead of upscaling.

Parameters `widthUnit` and `heightUnit` determine the units used to calculate the image width and height, respectively.

`altText` is used as the image description for assistive apps.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### AddImage

```csharp
public void AddImage(Texture2D image, int width, int height, Color? color, InlineAlignment inlineAlign, Rect2? region, Variant key, bool pad, string tooltip, bool widthInPercent, bool heightInPercent, string altText)
```

引擎名：`add_image`

Adds an image's opening and closing tags to the tag stack, optionally providing a `width` and `height` to resize the image, a `color` to tint the image and a `region` to only use parts of the image.

If `width` or `height` is set to 0, the image size will be adjusted in order to keep the original aspect ratio.

If `width` and `height` are not set, but `region` is, the region's rect will be used.

`key` is an optional identifier, that can be used to modify the image via `UpdateImage()`.

If `pad` is set, and the image is smaller than the size specified by `width` and `height`, the image padding is added to match the size instead of upscaling.

Parameters `widthUnit` and `heightUnit` determine the units used to calculate the image width and height, respectively.

`altText` is used as the image description for assistive apps.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### AddText

```csharp
public void AddText(string text)
```

引擎名：`add_text`

Adds raw non-BBCode-parsed text to the tag stack.

### AppendText

```csharp
public void AppendText(string bbcode)
```

引擎名：`append_text`

Parses `bbcode` and adds tags to the tag stack as needed.

**Note:** Using this method, you can't close a tag that was opened in a previous `AppendText()` call. This is done to improve performance, especially when updating large RichTextLabels since rebuilding the whole BBCode every time would be slower. If you absolutely need to close a tag in a future method call, append the `Text` instead of using `AppendText()`.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the tag stack, causing the label to display nothing.

**Note:** This method does not affect `Text`, and its contents will show again if the label is redrawn. However, setting `Text` to an empty `System.String` also clears the stack.

### Deselect

```csharp
public void Deselect()
```

引擎名：`deselect`

Clears the current selection.

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

### GetCharacterLine

```csharp
public int GetCharacterLine(int character)
```

引擎名：`get_character_line`

Returns the line number of the character position provided. Line and character numbers are both zero-indexed.

**Note:** If `Threaded` is enabled, this method returns a value for the loaded part of the document. Use `IsFinished()` or `Finished` to determine whether document is fully loaded.

### GetCharacterParagraph

```csharp
public int GetCharacterParagraph(int character)
```

引擎名：`get_character_paragraph`

Returns the paragraph number of the character position provided. Paragraph and character numbers are both zero-indexed.

**Note:** If `Threaded` is enabled, this method returns a value for the loaded part of the document. Use `IsFinished()` or `Finished` to determine whether document is fully loaded.

### GetContentHeight

```csharp
public int GetContentHeight()
```

引擎名：`get_content_height`

Returns the height of the content.

**Note:** This method always returns the full content size, and is not affected by `VisibleRatio` and `VisibleCharacters`. To get the visible content size, use `GetVisibleContentRect()`.

**Note:** If `Threaded` is enabled, this method returns a value for the loaded part of the document. Use `IsFinished()` or `Finished` to determine whether document is fully loaded.

### GetContentWidth

```csharp
public int GetContentWidth()
```

引擎名：`get_content_width`

Returns the width of the content.

**Note:** This method always returns the full content size, and is not affected by `VisibleRatio` and `VisibleCharacters`. To get the visible content size, use `GetVisibleContentRect()`.

**Note:** If `Threaded` is enabled, this method returns a value for the loaded part of the document. Use `IsFinished()` or `Finished` to determine whether document is fully loaded.

### GetEffects

```csharp
public Array GetEffects()
```

引擎名：`get_effects`

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

### GetLineCount

```csharp
public int GetLineCount()
```

引擎名：`get_line_count`

Returns the total number of lines in the text. Wrapped text is counted as multiple lines.

**Note:** If `Threaded` is enabled, this method returns a value for the loaded part of the document. Use `IsFinished()` or `Finished` to determine whether document is fully loaded.

### GetLineHeight

```csharp
public int GetLineHeight(int line)
```

引擎名：`get_line_height`

Returns the height of the line found at the provided index.

**Note:** If `Threaded` is enabled, this method returns a value for the loaded part of the document. Use `IsFinished()` or `Finished` to determine whether the document is fully loaded.

### GetLineOffset

```csharp
public float GetLineOffset(int line)
```

引擎名：`get_line_offset`

Returns the vertical offset of the line found at the provided index.

**Note:** If `Threaded` is enabled, this method returns a value for the loaded part of the document. Use `IsFinished()` or `Finished` to determine whether document is fully loaded.

### GetLineRange

```csharp
public Vector2I GetLineRange(int line)
```

引擎名：`get_line_range`

Returns the indexes of the first and last visible characters for the given `line`, as a [`Vector2I`](Vector2I.md).

**Note:** If `VisibleCharactersBehavior` is set to [`TextServer.VisibleCharactersBehavior.CharsBeforeShaping`](TextServer.md) only visible wrapped lines are counted.

**Note:** If `Threaded` is enabled, this method returns a value for the loaded part of the document. Use `IsFinished()` or `Finished` to determine whether document is fully loaded.

### GetLineWidth

```csharp
public int GetLineWidth(int line)
```

引擎名：`get_line_width`

Returns the width of the line found at the provided index.

**Note:** If `Threaded` is enabled, this method returns a value for the loaded part of the document. Use `IsFinished()` or `Finished` to determine whether the document is fully loaded.

### GetMenu

```csharp
public PopupMenu GetMenu()
```

引擎名：`get_menu`

Returns the [`PopupMenu`](PopupMenu.md) of this `RichTextLabel`. By default, this menu is displayed when right-clicking on the `RichTextLabel`.

You can add custom menu items or remove standard ones. Make sure your IDs don't conflict with the standard ones (see [`RichTextLabel.MenuItems`](RichTextLabel.md)). For example:

```csharp
public override void _Ready()
{
var menu = GetMenu();
// Remove "Select All" item.
menu.RemoveItem(RichTextLabel.MenuItems.SelectAll);
// Add custom items.
menu.AddSeparator();
menu.AddItem("Duplicate Text", RichTextLabel.MenuItems.Max + 1);
// Add event handler.
menu.IdPressed += OnItemPressed;
}

public void OnItemPressed(int id)
{
if (id == TextEdit.MenuItems.Max + 1)
{
AddText("\n" + GetParsedText());
}
}

```

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`Window.Visible`](Window.md) property.

### GetParagraphCount

```csharp
public int GetParagraphCount()
```

引擎名：`get_paragraph_count`

Returns the total number of paragraphs (newlines or `p` tags in the tag stack's text tags). Considers wrapped text as one paragraph.

### GetParagraphOffset

```csharp
public float GetParagraphOffset(int paragraph)
```

引擎名：`get_paragraph_offset`

Returns the vertical offset of the paragraph found at the provided index.

**Note:** If `Threaded` is enabled, this method returns a value for the loaded part of the document. Use `IsFinished()` or `Finished` to determine whether document is fully loaded.

### GetParsedText

```csharp
public string GetParsedText()
```

引擎名：`get_parsed_text`

Returns the text without BBCode mark-up.

### GetProgressBarDelay

```csharp
public int GetProgressBarDelay()
```

引擎名：`get_progress_bar_delay`

### GetSelectedText

```csharp
public string GetSelectedText()
```

引擎名：`get_selected_text`

Returns the current selection text. Does not include BBCodes.

### GetSelectionFrom

```csharp
public int GetSelectionFrom()
```

引擎名：`get_selection_from`

Returns the current selection first character index if a selection is active, `-1` otherwise. Does not include BBCodes.

### GetSelectionLineOffset

```csharp
public float GetSelectionLineOffset()
```

引擎名：`get_selection_line_offset`

Returns the current selection vertical line offset if a selection is active, `-1.0` otherwise.

### GetSelectionTo

```csharp
public int GetSelectionTo()
```

引擎名：`get_selection_to`

Returns the current selection last character index if a selection is active, `-1` otherwise. Does not include BBCodes.

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

### GetTabSize

```csharp
public int GetTabSize()
```

引擎名：`get_tab_size`

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

### GetTotalCharacterCount

```csharp
public int GetTotalCharacterCount()
```

引擎名：`get_total_character_count`

Returns the total number of characters from text tags. Does not include BBCodes.

### GetVScrollBar

```csharp
public VScrollBar GetVScrollBar()
```

引擎名：`get_v_scroll_bar`

Returns the vertical scrollbar.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`CanvasItem.Visible`](CanvasItem.md) property.

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

### GetVisibleContentRect

```csharp
public Rect2I GetVisibleContentRect()
```

引擎名：`get_visible_content_rect`

Returns the bounding rectangle of the visible content.

**Note:** This method returns a correct value only after the label has been drawn.

```csharp
public partial class TestLabel : RichTextLabel
{
[Export]
public Panel BackgroundPanel { get; set; }

public override async void _Ready()
{
await ToSignal(this, Control.SignalName.Draw);
BackgroundGPanel.Position = GetVisibleContentRect().Position;
BackgroundPanel.Size = GetVisibleContentRect().Size;
}
}

```

### GetVisibleLineCount

```csharp
public int GetVisibleLineCount()
```

引擎名：`get_visible_line_count`

Returns the number of visible lines.

**Note:** This method returns a correct value only after the label has been drawn.

**Note:** If `Threaded` is enabled, this method returns a value for the loaded part of the document. Use `IsFinished()` or `Finished` to determine whether document is fully loaded.

### GetVisibleParagraphCount

```csharp
public int GetVisibleParagraphCount()
```

引擎名：`get_visible_paragraph_count`

Returns the number of visible paragraphs. A paragraph is considered visible if at least one of its lines is visible.

**Note:** This method returns a correct value only after the label has been drawn.

**Note:** If `Threaded` is enabled, this method returns a value for the loaded part of the document. Use `IsFinished()` or `Finished` to determine whether document is fully loaded.

### GetVisibleRatio

```csharp
public float GetVisibleRatio()
```

引擎名：`get_visible_ratio`

### InstallEffect

```csharp
public void InstallEffect(Variant effect)
```

引擎名：`install_effect`

Installs a custom effect. This can also be done in the Inspector through the `CustomEffects` property. `effect` should be a valid [`RichTextEffect`](RichTextEffect.md).

**Example:** With the following script extending from [`RichTextEffect`](RichTextEffect.md):

```csharp
# effect.gd
class_name MyCustomEffect
extends RichTextEffect

var bbcode = "my_custom_effect"

# ...

```

The above effect can be installed in `RichTextLabel` from a script:

```csharp
# rich_text_label.gd
extends RichTextLabel

func _ready():
install_effect(MyCustomEffect.new())

# Alternatively, if not using `class_name` in the script that extends RichTextEffect:
install_effect(preload("res://effect.gd").new())

```

### InvalidateParagraph

```csharp
public bool InvalidateParagraph(int paragraph)
```

引擎名：`invalidate_paragraph`

Invalidates `paragraph` and all subsequent paragraphs cache.

### IsContextMenuEnabled

```csharp
public bool IsContextMenuEnabled()
```

引擎名：`is_context_menu_enabled`

### IsDeselectOnFocusLossEnabled

```csharp
public bool IsDeselectOnFocusLossEnabled()
```

引擎名：`is_deselect_on_focus_loss_enabled`

### IsDragAndDropSelectionEnabled

```csharp
public bool IsDragAndDropSelectionEnabled()
```

引擎名：`is_drag_and_drop_selection_enabled`

### IsFinished

```csharp
public bool IsFinished()
```

引擎名：`is_finished`

If `Threaded` is enabled, returns `true` if the background thread has finished text processing, otherwise always return `true`.

### IsFitContentEnabled

```csharp
public bool IsFitContentEnabled()
```

引擎名：`is_fit_content_enabled`

### IsHintUnderlined

```csharp
public bool IsHintUnderlined()
```

引擎名：`is_hint_underlined`

### IsMenuVisible

```csharp
public bool IsMenuVisible()
```

引擎名：`is_menu_visible`

Returns whether the menu is visible. Use this instead of `get_menu().visible` to improve performance (so the creation of the menu is avoided).

### IsMetaUnderlined

```csharp
public bool IsMetaUnderlined()
```

引擎名：`is_meta_underlined`

### IsReady

```csharp
public bool IsReady()
```

引擎名：`is_ready`

If `Threaded` is enabled, returns `true` if the background thread has finished text processing, otherwise always return `true`.

### IsScrollActive

```csharp
public bool IsScrollActive()
```

引擎名：`is_scroll_active`

### IsScrollFollowing

```csharp
public bool IsScrollFollowing()
```

引擎名：`is_scroll_following`

### IsScrollFollowingVisibleCharacters

```csharp
public bool IsScrollFollowingVisibleCharacters()
```

引擎名：`is_scroll_following_visible_characters`

### IsSelectionEnabled

```csharp
public bool IsSelectionEnabled()
```

引擎名：`is_selection_enabled`

### IsShortcutKeysEnabled

```csharp
public bool IsShortcutKeysEnabled()
```

引擎名：`is_shortcut_keys_enabled`

### IsThreaded

```csharp
public bool IsThreaded()
```

引擎名：`is_threaded`

### IsUsingBbcode

```csharp
public bool IsUsingBbcode()
```

引擎名：`is_using_bbcode`

### MenuOption

```csharp
public void MenuOption(int option)
```

引擎名：`menu_option`

Executes a given action as defined in the [`RichTextLabel.MenuItems`](RichTextLabel.md) enum.

### Newline

```csharp
public void Newline()
```

引擎名：`newline`

Adds a newline tag to the tag stack.

### ParseBbcode

```csharp
public void ParseBbcode(string bbcode)
```

引擎名：`parse_bbcode`

The assignment version of `AppendText()`. Clears the tag stack and inserts the new content.

### ParseExpressionsForValues

```csharp
public Dictionary ParseExpressionsForValues(ReadOnlySpan<string> expressions)
```

引擎名：`parse_expressions_for_values`

Parses BBCode parameter `expressions` into a dictionary.

### ParseExpressionsForValues

```csharp
public Dictionary ParseExpressionsForValues(string[] expressions)
```

引擎名：`parse_expressions_for_values`

Parses BBCode parameter `expressions` into a dictionary.

### Pop

```csharp
public void Pop()
```

引擎名：`pop`

Terminates the current tag. Use after `push_*` methods to close BBCodes manually. Does not need to follow `add_*` methods.

### PopAll

```csharp
public void PopAll()
```

引擎名：`pop_all`

Terminates all tags opened by `push_*` methods.

### PopContext

```csharp
public void PopContext()
```

引擎名：`pop_context`

Terminates tags opened after the last `PushContext()` call (including context marker), or all tags if there's no context marker on the stack.

### PushBgcolor

```csharp
public void PushBgcolor(Color bgcolor)
```

引擎名：`push_bgcolor`

Adds a `[bgcolor]` tag to the tag stack.

**Note:** The background color has padding applied by default, which is controlled using `text_highlight_h_padding` and `text_highlight_v_padding`. This can lead to overlapping highlights if background colors are placed on neighboring lines/columns, so consider setting those theme items to `0` if you want to avoid this.

### PushBold

```csharp
public void PushBold()
```

引擎名：`push_bold`

Adds a `[font]` tag with a bold font to the tag stack. This is the same as adding a `[b]` tag if not currently in a `[i]` tag.

### PushBoldItalics

```csharp
public void PushBoldItalics()
```

引擎名：`push_bold_italics`

Adds a `[font]` tag with a bold italics font to the tag stack.

### PushCell

```csharp
public void PushCell()
```

引擎名：`push_cell`

Adds a `[cell]` tag to the tag stack. Must be inside a `[table]` tag. See `PushTable()` for details. Use `SetTableColumnExpand()` to set column expansion ratio, `SetCellBorderColor()` to set cell border, `SetCellRowBackgroundColor()` to set cell background, `SetCellSizeOverride()` to override cell size, and `SetCellPadding()` to set padding.

### PushColor

```csharp
public void PushColor(Color color)
```

引擎名：`push_color`

Adds a `[color]` tag to the tag stack.

### PushContext

```csharp
public void PushContext()
```

引擎名：`push_context`

Adds a context marker to the tag stack. See `PopContext()`.

### PushCustomfx

```csharp
public void PushCustomfx(RichTextEffect effect, Dictionary env)
```

引擎名：`push_customfx`

Adds a custom effect tag to the tag stack. The effect does not need to be in `CustomEffects`. The environment is directly passed to the effect.

### PushDropcap

```csharp
public void PushDropcap(string string, Font font, int size, Rect2? dropcapMargins = null, Color? color = null, int outlineSize = 0, Color? outlineColor = null)
```

引擎名：`push_dropcap`

Adds a `[dropcap]` tag to the tag stack. Drop cap (dropped capital) is a decorative element at the beginning of a paragraph that is larger than the rest of the text.

参数：
- `dropcapMargins`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `outlineColor`：If the parameter is null, then the default value is `new Color(0.0f, 0.0f, 0.0f, 0.0f)`.

### PushFgcolor

```csharp
public void PushFgcolor(Color fgcolor)
```

引擎名：`push_fgcolor`

Adds a `[fgcolor]` tag to the tag stack.

**Note:** The foreground color has padding applied by default, which is controlled using `text_highlight_h_padding` and `text_highlight_v_padding`. This can lead to overlapping highlights if foreground colors are placed on neighboring lines/columns, so consider setting those theme items to `0` if you want to avoid this.

### PushFont

```csharp
public void PushFont(Font font, int fontSize = 0)
```

引擎名：`push_font`

Adds a `[font]` tag to the tag stack. Overrides default fonts for its duration.

Passing `0` to `fontSize` will use the existing default font size.

### PushFontSize

```csharp
public void PushFontSize(int fontSize)
```

引擎名：`push_font_size`

Adds a `[font_size]` tag to the tag stack. Overrides default font size for its duration.

### PushHint

```csharp
public void PushHint(string description)
```

引擎名：`push_hint`

Adds a `[hint]` tag to the tag stack. Same as BBCode `[hint=something]{text}[/hint]`.

### PushIndent

```csharp
public void PushIndent(int level)
```

引擎名：`push_indent`

Adds an `[indent]` tag to the tag stack. Multiplies `level` by current `TabSize` to determine new margin length.

### PushItalics

```csharp
public void PushItalics()
```

引擎名：`push_italics`

Adds a `[font]` tag with an italics font to the tag stack. This is the same as adding an `[i]` tag if not currently in a `[b]` tag.

### PushLanguage

```csharp
public void PushLanguage(string language)
```

引擎名：`push_language`

Adds language code used for text shaping algorithm and Open-Type font features.

### PushList

```csharp
public void PushList(int level, RichTextLabel.ListType type, bool capitalize)
```

引擎名：`push_list`

Adds `[ol]` or `[ul]` tag to the tag stack. Multiplies `level` by current `TabSize` to determine new margin length.

### PushList

```csharp
public void PushList(int level, RichTextLabel.ListType type, bool capitalize, string bullet = "•")
```

引擎名：`push_list`

Adds `[ol]` or `[ul]` tag to the tag stack. Multiplies `level` by current `TabSize` to determine new margin length.

### PushMeta

```csharp
public void PushMeta(Variant data)
```

引擎名：`push_meta`

Adds a meta tag to the tag stack. Similar to the BBCode `[url=something]{text}[/url]`, but supports non-`System.String` metadata types.

If `MetaUnderlined` is `true`, meta tags display an underline. This behavior can be customized with `underlineMode`.

**Note:** Meta tags do nothing by default when clicked. To assign behavior when clicked, connect `MetaClicked` to a function that is called when the meta tag is clicked.

### PushMeta

```csharp
public void PushMeta(Variant data, RichTextLabel.MetaUnderline underlineMode = RichTextLabel.MetaUnderline.Always, string tooltip = "")
```

引擎名：`push_meta`

Adds a meta tag to the tag stack. Similar to the BBCode `[url=something]{text}[/url]`, but supports non-`System.String` metadata types.

If `MetaUnderlined` is `true`, meta tags display an underline. This behavior can be customized with `underlineMode`.

**Note:** Meta tags do nothing by default when clicked. To assign behavior when clicked, connect `MetaClicked` to a function that is called when the meta tag is clicked.

### PushMeta

```csharp
public void PushMeta(Variant data, RichTextLabel.MetaUnderline underlineMode)
```

引擎名：`push_meta`

Adds a meta tag to the tag stack. Similar to the BBCode `[url=something]{text}[/url]`, but supports non-`System.String` metadata types.

If `MetaUnderlined` is `true`, meta tags display an underline. This behavior can be customized with `underlineMode`.

**Note:** Meta tags do nothing by default when clicked. To assign behavior when clicked, connect `MetaClicked` to a function that is called when the meta tag is clicked.

### PushMono

```csharp
public void PushMono()
```

引擎名：`push_mono`

Adds a `[font]` tag with a monospace font to the tag stack.

### PushNormal

```csharp
public void PushNormal()
```

引擎名：`push_normal`

Adds a `[font]` tag with a normal font to the tag stack.

### PushOutlineColor

```csharp
public void PushOutlineColor(Color color)
```

引擎名：`push_outline_color`

Adds a `[outline_color]` tag to the tag stack. Adds text outline for its duration.

### PushOutlineSize

```csharp
public void PushOutlineSize(int outlineSize)
```

引擎名：`push_outline_size`

Adds a `[outline_size]` tag to the tag stack. Overrides default text outline size for its duration.

### PushParagraph

```csharp
public void PushParagraph(HorizontalAlignment alignment, Control.TextDirection baseDirection = Control.TextDirection.Auto, string language = "", TextServer.StructuredTextParser stParser = TextServer.StructuredTextParser.Default, TextServer.JustificationFlag justificationFlags = TextServer.JustificationFlag., float[] tabStops = null)
```

引擎名：`push_paragraph`

Adds a `[p]` tag to the tag stack.

参数：
- `tabStops`：If the parameter is null, then the default value is `Array.Empty<float>()`.

### PushParagraph

```csharp
public void PushParagraph(HorizontalAlignment alignment, Control.TextDirection baseDirection, string language, TextServer.StructuredTextParser stParser)
```

引擎名：`push_paragraph`

Adds a `[p]` tag to the tag stack.

参数：
- `tabStops`：If the parameter is null, then the default value is `Array.Empty<float>()`.

### PushParagraph

```csharp
public void PushParagraph(HorizontalAlignment alignment, Control.TextDirection baseDirection, string language, TextServer.StructuredTextParser stParser, TextServer.JustificationFlag justificationFlags, ReadOnlySpan<float> tabStops)
```

引擎名：`push_paragraph`

Adds a `[p]` tag to the tag stack.

### PushStrikethrough

```csharp
public void PushStrikethrough()
```

引擎名：`push_strikethrough`

Adds a `[s]` tag to the tag stack. If `color`'s alpha value is `0.0`, the current font's color with its alpha multiplied by `strikethrough_alpha` is used.

### PushStrikethrough

```csharp
public void PushStrikethrough(Color? color = null)
```

引擎名：`push_strikethrough`

Adds a `[s]` tag to the tag stack. If `color`'s alpha value is `0.0`, the current font's color with its alpha multiplied by `strikethrough_alpha` is used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(0.0f, 0.0f, 0.0f, 0.0f)`.

### PushTable

```csharp
public void PushTable(int columns, InlineAlignment inlineAlign = InlineAlignment.TopTo, int alignToRow = -1, string name = "")
```

引擎名：`push_table`

Adds a `[table=columns,inline_align]` tag to the tag stack. Use `SetTableColumnExpand()` to set column expansion ratio. Use `PushCell()` to add cells. `name` is used as the table name for assistive apps.

### PushTable

```csharp
public void PushTable(int columns, InlineAlignment inlineAlign, int alignToRow)
```

引擎名：`push_table`

Adds a `[table=columns,inline_align]` tag to the tag stack. Use `SetTableColumnExpand()` to set column expansion ratio. Use `PushCell()` to add cells. `name` is used as the table name for assistive apps.

### PushUnderline

```csharp
public void PushUnderline()
```

引擎名：`push_underline`

Adds a `[u]` tag to the tag stack. If `color`'s alpha value is `0.0`, the current font's color with its alpha multiplied by `underline_alpha` is used.

### PushUnderline

```csharp
public void PushUnderline(Color? color = null)
```

引擎名：`push_underline`

Adds a `[u]` tag to the tag stack. If `color`'s alpha value is `0.0`, the current font's color with its alpha multiplied by `underline_alpha` is used.

参数：
- `color`：If the parameter is null, then the default value is `new Color(0.0f, 0.0f, 0.0f, 0.0f)`.

### ReloadEffects

```csharp
public void ReloadEffects()
```

引擎名：`reload_effects`

Reloads custom effects. Useful when `CustomEffects` is modified manually.

### RemoveParagraph

```csharp
public bool RemoveParagraph(int paragraph)
```

引擎名：`remove_paragraph`

Removes a paragraph of content from the label. Returns `true` if the paragraph exists.

The `paragraph` argument is the index of the paragraph to remove, it can take values in the interval `[0, get_paragraph_count() - 1]`.

If `noInvalidate` is set to `true`, cache for the subsequent paragraphs is not invalidated. Use it for faster updates if deleted paragraph is fully self-contained (have no unclosed tags), or this call is part of the complex edit operation and `InvalidateParagraph()` will be called at the end of operation.

### RemoveParagraph

```csharp
public bool RemoveParagraph(int paragraph, bool noInvalidate = false)
```

引擎名：`remove_paragraph`

Removes a paragraph of content from the label. Returns `true` if the paragraph exists.

The `paragraph` argument is the index of the paragraph to remove, it can take values in the interval `[0, get_paragraph_count() - 1]`.

If `noInvalidate` is set to `true`, cache for the subsequent paragraphs is not invalidated. Use it for faster updates if deleted paragraph is fully self-contained (have no unclosed tags), or this call is part of the complex edit operation and `InvalidateParagraph()` will be called at the end of operation.

### ScrollToLine

```csharp
public void ScrollToLine(int line)
```

引擎名：`scroll_to_line`

Scrolls the window's top line to match `line`.

### ScrollToParagraph

```csharp
public void ScrollToParagraph(int paragraph)
```

引擎名：`scroll_to_paragraph`

Scrolls the window's top line to match first line of the `paragraph`.

### ScrollToSelection

```csharp
public void ScrollToSelection()
```

引擎名：`scroll_to_selection`

Scrolls to the beginning of the current selection.

### SelectAll

```csharp
public void SelectAll()
```

引擎名：`select_all`

Select all the text.

If `SelectionEnabled` is `false`, no selection will occur.

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

### SetCellBorderColor

```csharp
public void SetCellBorderColor(Color color)
```

引擎名：`set_cell_border_color`

Sets color of a table cell border.

### SetCellPadding

```csharp
public void SetCellPadding(Rect2 padding)
```

引擎名：`set_cell_padding`

Sets inner padding of a table cell.

### SetCellRowBackgroundColor

```csharp
public void SetCellRowBackgroundColor(Color oddRowBg, Color evenRowBg)
```

引擎名：`set_cell_row_background_color`

Sets color of a table cell. Separate colors for alternating rows can be specified.

### SetCellSizeOverride

```csharp
public void SetCellSizeOverride(Vector2 minSize, Vector2 maxSize)
```

引擎名：`set_cell_size_override`

Sets minimum and maximum size overrides for a table cell.

### SetContextMenuEnabled

```csharp
public void SetContextMenuEnabled(bool enabled)
```

引擎名：`set_context_menu_enabled`

### SetDeselectOnFocusLossEnabled

```csharp
public void SetDeselectOnFocusLossEnabled(bool enable)
```

引擎名：`set_deselect_on_focus_loss_enabled`

### SetDragAndDropSelectionEnabled

```csharp
public void SetDragAndDropSelectionEnabled(bool enable)
```

引擎名：`set_drag_and_drop_selection_enabled`

### SetEffects

```csharp
public void SetEffects(Array effects)
```

引擎名：`set_effects`

### SetFitContent

```csharp
public void SetFitContent(bool enabled)
```

引擎名：`set_fit_content`

### SetHintUnderline

```csharp
public void SetHintUnderline(bool enable)
```

引擎名：`set_hint_underline`

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

### SetMetaUnderline

```csharp
public void SetMetaUnderline(bool enable)
```

引擎名：`set_meta_underline`

### SetProgressBarDelay

```csharp
public void SetProgressBarDelay(int delayMs)
```

引擎名：`set_progress_bar_delay`

### SetScrollActive

```csharp
public void SetScrollActive(bool active)
```

引擎名：`set_scroll_active`

### SetScrollFollow

```csharp
public void SetScrollFollow(bool follow)
```

引擎名：`set_scroll_follow`

### SetScrollFollowVisibleCharacters

```csharp
public void SetScrollFollowVisibleCharacters(bool follow)
```

引擎名：`set_scroll_follow_visible_characters`

### SetSelectionEnabled

```csharp
public void SetSelectionEnabled(bool enabled)
```

引擎名：`set_selection_enabled`

### SetShortcutKeysEnabled

```csharp
public void SetShortcutKeysEnabled(bool enabled)
```

引擎名：`set_shortcut_keys_enabled`

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

### SetTabSize

```csharp
public void SetTabSize(int spaces)
```

引擎名：`set_tab_size`

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

### SetTableColumnExpand

```csharp
public void SetTableColumnExpand(int column, bool expand, int ratio = 1, bool shrink = true)
```

引擎名：`set_table_column_expand`

Edits the selected column's expansion options. If `expand` is `true`, the column expands in proportion to its expansion ratio versus the other columns' ratios.

For example, 2 columns with ratios 3 and 4 plus 70 pixels in available width would expand 30 and 40 pixels, respectively.

If `expand` is `false`, the column will not contribute to the total ratio.

### SetTableColumnExpand

```csharp
public void SetTableColumnExpand(int column, bool expand, int ratio)
```

引擎名：`set_table_column_expand`

Edits the selected column's expansion options. If `expand` is `true`, the column expands in proportion to its expansion ratio versus the other columns' ratios.

For example, 2 columns with ratios 3 and 4 plus 70 pixels in available width would expand 30 and 40 pixels, respectively.

If `expand` is `false`, the column will not contribute to the total ratio.

### SetTableColumnName

```csharp
public void SetTableColumnName(int column, string name)
```

引擎名：`set_table_column_name`

Sets table column name for assistive apps.

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

### SetThreaded

```csharp
public void SetThreaded(bool threaded)
```

引擎名：`set_threaded`

### SetUseBbcode

```csharp
public void SetUseBbcode(bool enable)
```

引擎名：`set_use_bbcode`

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

### UpdateImage

```csharp
public void UpdateImage(Variant key, RichTextLabel.ImageUpdateMask mask, Texture2D image, float width = 0f, float height = 0f, Color? color = null, InlineAlignment inlineAlign = InlineAlignment.Center, Rect2? region = null, bool pad = false, string tooltip = "", RichTextLabel.ImageUnit widthUnit = RichTextLabel.ImageUnit.Pixel, RichTextLabel.ImageUnit heightUnit = RichTextLabel.ImageUnit.Pixel)
```

引擎名：`update_image`

Updates the existing images with the key `key`. Only properties specified by `mask` bits are updated. See `AddImage()`.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### UpdateImage

```csharp
public void UpdateImage(Variant key, RichTextLabel.ImageUpdateMask mask, Texture2D image, int width, int height, Color? color, InlineAlignment inlineAlign, Rect2? region, bool pad, string tooltip, bool sizeInPercent)
```

引擎名：`update_image`

Updates the existing images with the key `key`. Only properties specified by `mask` bits are updated. See `AddImage()`.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### UpdateImage

```csharp
public void UpdateImage(Variant key, RichTextLabel.ImageUpdateMask mask, Texture2D image, int width, int height, Color? color, InlineAlignment inlineAlign, Rect2? region, bool pad, string tooltip, bool widthInPercent, bool heightInPercent)
```

引擎名：`update_image`

Updates the existing images with the key `key`. Only properties specified by `mask` bits are updated. See `AddImage()`.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

## 事件（信号）

### Finished

```csharp
public event Action Finished
```

引擎名：`finished`

Triggered when the document is fully loaded.

**Note:** This can happen before the text is processed for drawing. Scrolling values may not be valid until the document is drawn for the first time after this signal.

### MetaClicked

```csharp
public event RichTextLabel.MetaClickedEventHandler MetaClicked
```

引擎名：`meta_clicked`

Triggered when the user clicks on content between meta (URL) tags. If the meta is defined in BBCode, e.g. `[url={"key": "value"}]Text[/url]`, then the parameter for this signal will always be a `System.String` type. If a particular type or an object is desired, the `PushMeta()` method must be used to manually insert the data into the tag stack. Alternatively, you can convert the `System.String` input to the desired type based on its contents (such as calling [`Json.Parse`](Json.md) on it).

For example, the following method can be connected to `MetaClicked` to open clicked URLs using the user's default web browser:

### MetaHoverEnded

```csharp
public event RichTextLabel.MetaHoverEndedEventHandler MetaHoverEnded
```

引擎名：`meta_hover_ended`

Triggers when the mouse exits a meta tag.

### MetaHoverStarted

```csharp
public event RichTextLabel.MetaHoverStartedEventHandler MetaHoverStarted
```

引擎名：`meta_hover_started`

Triggers when the mouse enters a meta tag.

## ImageUnit

命名空间 `Godot`；enum

### 枚举值

- `Pixel = 0`：Images drawn with this unit will be in pixels.
- `Percent = 1`：Images drawn with this unit will be in percentages of the control width.
- `Em = 2`：Images drawn with this unit will be in percentages of the surrounding font size.

## ImageUpdateMask

命名空间 `Godot`；enum

### 枚举值

- `Texture = 1`：If this bit is set, [`RichTextLabel.UpdateImage`](RichTextLabel.md) changes image texture.
- `Size = 2`：If this bit is set, [`RichTextLabel.UpdateImage`](RichTextLabel.md) changes image size.
- `Color = 4`：If this bit is set, [`RichTextLabel.UpdateImage`](RichTextLabel.md) changes image color.
- `Alignment = 8`：If this bit is set, [`RichTextLabel.UpdateImage`](RichTextLabel.md) changes image inline alignment.
- `Region = 16`：If this bit is set, [`RichTextLabel.UpdateImage`](RichTextLabel.md) changes image texture region.
- `Pad = 32`：If this bit is set, [`RichTextLabel.UpdateImage`](RichTextLabel.md) changes image padding.
- `Tooltip = 64`：If this bit is set, [`RichTextLabel.UpdateImage`](RichTextLabel.md) changes image tooltip.
- `WidthUnit = 128`：If this bit is set, [`RichTextLabel.UpdateImage`](RichTextLabel.md) changes the units used to calculate image size.

## ListType

命名空间 `Godot`；enum

### 枚举值

- `Numbers = 0`：Each list item has a number marker.
- `Letters = 1`：Each list item has a letter marker.
- `Roman = 2`：Each list item has a roman number marker.
- `Dots = 3`：Each list item has a filled circle marker.

## MenuItems

命名空间 `Godot`；enum

### 枚举值

- `Copy = 0`：Copies the selected text.
- `SelectAll = 1`：Selects the whole [`RichTextLabel`](RichTextLabel.md) text.
- `Max = 2`：Represents the size of the `RichTextLabel.MenuItems` enum.

## MetaClickedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`RichTextLabel.MetaClicked`](RichTextLabel.md) event of a [`RichTextLabel`](RichTextLabel.md) class.

### 签名

#### Invoke

```csharp
delegate void MetaClickedEventHandler(Variant meta)
```

## MetaHoverEndedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`RichTextLabel.MetaHoverEnded`](RichTextLabel.md) event of a [`RichTextLabel`](RichTextLabel.md) class.

### 签名

#### Invoke

```csharp
delegate void MetaHoverEndedEventHandler(Variant meta)
```

## MetaHoverStartedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`RichTextLabel.MetaHoverStarted`](RichTextLabel.md) event of a [`RichTextLabel`](RichTextLabel.md) class.

### 签名

#### Invoke

```csharp
delegate void MetaHoverStartedEventHandler(Variant meta)
```

## MetaUnderline

命名空间 `Godot`；enum

### 枚举值

- `Never = 0`：Meta tag does not display an underline, even if [`RichTextLabel.MetaUnderlined`](RichTextLabel.md) is `true`.
- `Always = 1`：If [`RichTextLabel.MetaUnderlined`](RichTextLabel.md) is `true`, meta tag always display an underline.
- `OnHover = 2`：If [`RichTextLabel.MetaUnderlined`](RichTextLabel.md) is `true`, meta tag display an underline when the mouse cursor is over it.
