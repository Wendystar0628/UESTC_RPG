<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextEdit

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A multiline text editor. It also has limited facilities for editing code, such as syntax highlighting support. For more advanced facilities for editing code, see [`CodeEdit`](CodeEdit.md).

While entering text, it is possible to insert special characters using Unicode, OEM or Windows alt codes:

- To enter Unicode codepoints, hold Alt and type the codepoint on the numpad. For example, to enter the character `á` (U+00E1), hold Alt and type +E1 on the numpad (the leading zeroes can be omitted).

- To enter OEM codepoints, hold Alt and type the code on the numpad. For example, to enter the character `á` (OEM 160), hold Alt and type `160` on the numpad.

- To enter Windows codepoints, hold Alt and type the code on the numpad. For example, to enter the character `á` (Windows 0225), hold Alt and type 0, 2, 2, 5 on the numpad. The leading zero here must **not** be omitted, as this is how Windows codepoints are distinguished from OEM codepoints.

**Note:** Most viewport, caret, and edit methods contain a `caret_index` argument for `CaretMultiple` support. The argument should be one of the following: `-1` for all carets, `0` for the main caret, or greater than `0` for secondary carets in the order they were created.

**Note:** When holding down Alt, the vertical scroll wheel will scroll 5 times as fast as it would normally do. This also works in the Godot script editor.

## 构造函数

### TextEdit

```csharp
public TextEdit()
```

## 属性

### AutowrapMode

```csharp
public TextServer.AutowrapMode AutowrapMode { get; set; }
```

引擎名：`autowrap_mode`

If `WrapMode` is set to [`TextEdit.LineWrappingMode.Boundary`](TextEdit.md), sets text wrapping mode.

### BackspaceDeletesCompositeCharacterEnabled

```csharp
public bool BackspaceDeletesCompositeCharacterEnabled { get; set; }
```

引擎名：`backspace_deletes_composite_character_enabled`

If `true` and `CaretMidGrapheme` is `false`, backspace deletes an entire composite character such as ❤️‍🩹, instead of deleting part of the composite character.

### CaretBlink

```csharp
public bool CaretBlink { get; set; }
```

引擎名：`caret_blink`

If `true`, makes the caret blink.

### CaretBlinkInterval

```csharp
public float CaretBlinkInterval { get; set; }
```

引擎名：`caret_blink_interval`

The interval at which the caret blinks (in seconds).

### CaretDrawWhenEditableDisabled

```csharp
public bool CaretDrawWhenEditableDisabled { get; set; }
```

引擎名：`caret_draw_when_editable_disabled`

If `true`, caret will be visible when `Editable` is disabled.

### CaretMidGrapheme

```csharp
public bool CaretMidGrapheme { get; set; }
```

引擎名：`caret_mid_grapheme`

Allow moving caret, selecting and removing the individual composite character components.

**Note:** Backspace is always removing individual composite character components.

### CaretMoveOnRightClick

```csharp
public bool CaretMoveOnRightClick { get; set; }
```

引擎名：`caret_move_on_right_click`

If `true`, a right-click moves the caret at the mouse position before displaying the context menu.

If `false`, the context menu ignores mouse location.

### CaretMultiple

```csharp
public bool CaretMultiple { get; set; }
```

引擎名：`caret_multiple`

If `true`, multiple carets are allowed. Left-clicking with Alt adds a new caret. See `AddCaret()` and `GetCaretCount()`.

### CaretType

```csharp
public TextEdit.CaretTypeEnum CaretType { get; set; }
```

引擎名：`caret_type`

Set the type of caret to draw.

### ContextMenuEnabled

```csharp
public bool ContextMenuEnabled { get; set; }
```

引擎名：`context_menu_enabled`

If `true`, a right-click displays the context menu.

### CustomWordSeparators

```csharp
public string CustomWordSeparators { get; set; }
```

引擎名：`custom_word_separators`

The characters to consider as word delimiters if `UseCustomWordSeparators` is `true`. The characters should be defined without separation, for example `#_!`.

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

If `true`, allow drag and drop of selected text. Text can still be dropped from other sources.

### DrawControlChars

```csharp
public bool DrawControlChars { get; set; }
```

引擎名：`draw_control_chars`

If `true`, control characters are displayed.

### DrawSpaces

```csharp
public bool DrawSpaces { get; set; }
```

引擎名：`draw_spaces`

If `true`, the "space" character will have a visible representation.

### DrawTabs

```csharp
public bool DrawTabs { get; set; }
```

引擎名：`draw_tabs`

If `true`, the "tab" character will have a visible representation.

### Editable

```csharp
public bool Editable { get; set; }
```

引擎名：`editable`

If `false`, existing text cannot be modified and new text cannot be added.

### EmojiMenuEnabled

```csharp
public bool EmojiMenuEnabled { get; set; }
```

引擎名：`emoji_menu_enabled`

If `true`, "Emoji and Symbols" menu is enabled.

### EmptySelectionClipboardEnabled

```csharp
public bool EmptySelectionClipboardEnabled { get; set; }
```

引擎名：`empty_selection_clipboard_enabled`

If `true`, copying or cutting without a selection is performed on all lines with a caret. Otherwise, copy and cut require a selection.

### HighlightAllOccurrences

```csharp
public bool HighlightAllOccurrences { get; set; }
```

引擎名：`highlight_all_occurrences`

If `true`, all occurrences of the selected text will be highlighted.

### HighlightCurrentLine

```csharp
public bool HighlightCurrentLine { get; set; }
```

引擎名：`highlight_current_line`

If `true`, the line containing the cursor is highlighted.

### IndentWrappedLines

```csharp
public bool IndentWrappedLines { get; set; }
```

引擎名：`indent_wrapped_lines`

If `true`, all wrapped lines are indented to the same amount as the unwrapped line.

### Language

```csharp
public string Language { get; set; }
```

引擎名：`language`

Language code used for line-breaking and text shaping algorithms. If left empty, the current locale is used instead.

### MiddleMousePasteEnabled

```csharp
public bool MiddleMousePasteEnabled { get; set; }
```

引擎名：`middle_mouse_paste_enabled`

If `false`, using middle mouse button to paste clipboard will be disabled.

**Note:** This method is only implemented on Linux.

### MinimapDraw

```csharp
public bool MinimapDraw { get; set; }
```

引擎名：`minimap_draw`

If `true`, a minimap is shown, providing an outline of your source code. The minimap uses a fixed-width text size.

### MinimapWidth

```csharp
public int MinimapWidth { get; set; }
```

引擎名：`minimap_width`

The width, in pixels, of the minimap.

### PlaceholderText

```csharp
public string PlaceholderText { get; set; }
```

引擎名：`placeholder_text`

Text shown when the `TextEdit` is empty. It is **not** the `TextEdit`'s default value (see `Text`).

### ScrollFitContentHeight

```csharp
public bool ScrollFitContentHeight { get; set; }
```

引擎名：`scroll_fit_content_height`

If `true`, `TextEdit` fits its minimum height to the number of visible lines instead of scrolling vertically. If a maximum height is set (for example via [`Control.CustomMaximumSize`](Control.md)) and content exceeds it, a vertical scrollbar is shown.

### ScrollFitContentWidth

```csharp
public bool ScrollFitContentWidth { get; set; }
```

引擎名：`scroll_fit_content_width`

If `true`, `TextEdit` fits its minimum width to the widest line instead of scrolling horizontally. If a maximum width is set (for example via [`Control.CustomMaximumSize`](Control.md)) and content exceeds it, a horizontal scrollbar is shown.

### ScrollHorizontal

```csharp
public int ScrollHorizontal { get; set; }
```

引擎名：`scroll_horizontal`

If there is a horizontal scrollbar, this determines the current horizontal scroll value in pixels.

### ScrollPastEndOfFile

```csharp
public bool ScrollPastEndOfFile { get; set; }
```

引擎名：`scroll_past_end_of_file`

Allow scrolling past the last line into "virtual" space.

### ScrollSmooth

```csharp
public bool ScrollSmooth { get; set; }
```

引擎名：`scroll_smooth`

Scroll smoothly over the text rather than jumping to the next location.

### ScrollVScrollSpeed

```csharp
public float ScrollVScrollSpeed { get; set; }
```

引擎名：`scroll_v_scroll_speed`

Sets the scroll speed with the minimap or when `ScrollSmooth` is enabled.

### ScrollVertical

```csharp
public double ScrollVertical { get; set; }
```

引擎名：`scroll_vertical`

If there is a vertical scrollbar, this determines the current vertical scroll value in line numbers, starting at 0 for the top line.

### SelectingEnabled

```csharp
public bool SelectingEnabled { get; set; }
```

引擎名：`selecting_enabled`

If `true`, text can be selected.

If `false`, text can not be selected by the user or by the `Select()` or `SelectAll()` methods.

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

### SyntaxHighlighter

```csharp
public SyntaxHighlighter SyntaxHighlighter { get; set; }
```

引擎名：`syntax_highlighter`

The syntax highlighter to use.

**Note:** A [`SyntaxHighlighter`](SyntaxHighlighter.md) instance should not be used across multiple `TextEdit` nodes.

### TabInputMode

```csharp
public bool TabInputMode { get; set; }
```

引擎名：`tab_input_mode`

If `true`, `ProjectSettings.input/ui_text_indent` input `Tab` character, otherwise it moves keyboard focus to the next [`Control`](Control.md) in the scene.

### Text

```csharp
public string Text { get; set; }
```

引擎名：`text`

String value of the `TextEdit`.

### TextDirection

```csharp
public Control.TextDirection TextDirection { get; set; }
```

引擎名：`text_direction`

Base text writing direction.

### UseCustomWordSeparators

```csharp
public bool UseCustomWordSeparators { get; set; }
```

引擎名：`use_custom_word_separators`

If `false`, using Ctrl + Left or Ctrl + Right (Cmd + Left or Cmd + Right on macOS) bindings will use the behavior of `UseDefaultWordSeparators`. If `true`, it will also stop the caret if a character within `CustomWordSeparators` is detected. Useful for subword moving. This behavior also will be applied to the behavior of text selection.

### UseDefaultWordSeparators

```csharp
public bool UseDefaultWordSeparators { get; set; }
```

引擎名：`use_default_word_separators`

If `false`, using Ctrl + Left or Ctrl + Right (Cmd + Left or Cmd + Right on macOS) bindings will stop moving caret only if a space or punctuation is detected. If `true`, it will also stop the caret if a character is part of `!"#$%&'()*+,-./:;<=>?@[\]^`{|}~`, the Unicode General Punctuation table, or the Unicode CJK Punctuation table. Useful for subword moving. This behavior also will be applied to the behavior of text selection.

### VirtualKeyboardEnabled

```csharp
public bool VirtualKeyboardEnabled { get; set; }
```

引擎名：`virtual_keyboard_enabled`

If `true`, the native virtual keyboard is enabled on platforms that support it.

### VirtualKeyboardShowOnFocus

```csharp
public bool VirtualKeyboardShowOnFocus { get; set; }
```

引擎名：`virtual_keyboard_show_on_focus`

If `true`, the native virtual keyboard is shown on focus events on platforms that support it.

### WrapMode

```csharp
public TextEdit.LineWrappingMode WrapMode { get; set; }
```

引擎名：`wrap_mode`

Sets the line wrapping mode to use.

## 方法

### AddCaret

```csharp
public int AddCaret(int line, int column)
```

引擎名：`add_caret`

Adds a new caret at the given location. Returns the index of the new caret, or `-1` if the location is invalid.

### AddCaretAtCarets

```csharp
public void AddCaretAtCarets(bool below)
```

引擎名：`add_caret_at_carets`

Adds an additional caret above or below every caret. If `below` is `true` the new caret will be added below and above otherwise.

### AddGutter

```csharp
public void AddGutter(int at = -1)
```

引擎名：`add_gutter`

Register a new gutter to this `TextEdit`. Use `at` to have a specific gutter order. A value of `-1` appends the gutter to the right.

### AddSelectionForNextOccurrence

```csharp
public void AddSelectionForNextOccurrence()
```

引擎名：`add_selection_for_next_occurrence`

Adds a selection and a caret for the next occurrence of the current selection. If there is no active selection, selects word under caret.

### AdjustCaretsAfterEdit

```csharp
public void AdjustCaretsAfterEdit(int caret, int fromLine, int fromCol, int toLine, int toCol)
```

引擎名：`adjust_carets_after_edit`

This method does nothing.

### AdjustViewportToCaret

```csharp
public void AdjustViewportToCaret(int caretIndex = 0)
```

引擎名：`adjust_viewport_to_caret`

Adjust the viewport so the caret is visible.

### ApplyIme

```csharp
public void ApplyIme()
```

引擎名：`apply_ime`

Applies text from the [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) (IME) to each caret and closes the IME if it is open.

### Backspace

```csharp
public void Backspace(int caretIndex = -1)
```

引擎名：`backspace`

Called when the user presses the backspace key. Can be overridden with `_Backspace()`.

### BeginComplexOperation

```csharp
public void BeginComplexOperation()
```

引擎名：`begin_complex_operation`

Starts a multipart edit. All edits will be treated as one action until `EndComplexOperation()` is called.

### BeginMulticaretEdit

```csharp
public void BeginMulticaretEdit()
```

引擎名：`begin_multicaret_edit`

Starts an edit for multiple carets. The edit must be ended with `EndMulticaretEdit()`. Multicaret edits can be used to edit text at multiple carets and delay merging the carets until the end, so the caret indexes aren't affected immediately. `BeginMulticaretEdit()` and `EndMulticaretEdit()` can be nested, and the merge will happen at the last `EndMulticaretEdit()`.

```csharp
begin_complex_operation()
begin_multicaret_edit()
for i in range(get_caret_count()):
if multicaret_edit_ignore_caret(i):
continue
# Logic here.
end_multicaret_edit()
end_complex_operation()

```

### CancelIme

```csharp
public void CancelIme()
```

引擎名：`cancel_ime`

Closes the [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) (IME) if it is open. Any text in the IME will be lost.

### CenterViewportToCaret

```csharp
public void CenterViewportToCaret(int caretIndex = 0)
```

引擎名：`center_viewport_to_caret`

Centers the viewport on the line the editing caret is at. This also resets the `ScrollHorizontal` value to `0`.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Performs a full reset of `TextEdit`, including undo history.

### ClearUndoHistory

```csharp
public void ClearUndoHistory()
```

引擎名：`clear_undo_history`

Clears the undo history.

### CollapseCarets

```csharp
public void CollapseCarets(int fromLine, int fromColumn, int toLine, int toColumn, bool inclusive = false)
```

引擎名：`collapse_carets`

Collapse all carets in the given range to the `fromLine` and `fromColumn` position.

`inclusive` applies to both ends.

If `IsInMulitcaretEdit()` is `true`, carets that are collapsed will be `true` for `MulticaretEditIgnoreCaret()`.

`MergeOverlappingCarets()` will be called if any carets were collapsed.

### Copy

```csharp
public void Copy(int caretIndex = -1)
```

引擎名：`copy`

Copies the current text selection. Can be overridden with `_Copy()`.

### Cut

```csharp
public void Cut(int caretIndex = -1)
```

引擎名：`cut`

Cut's the current selection. Can be overridden with `_Cut()`.

### DeleteSelection

```csharp
public void DeleteSelection(int caretIndex = -1)
```

引擎名：`delete_selection`

Deletes the selected text.

### Deselect

```csharp
public void Deselect(int caretIndex = -1)
```

引擎名：`deselect`

Deselects the current selection.

### EndAction

```csharp
public void EndAction()
```

引擎名：`end_action`

Marks the end of steps in the current action started with `StartAction()`.

### EndComplexOperation

```csharp
public void EndComplexOperation()
```

引擎名：`end_complex_operation`

Ends a multipart edit, started with `BeginComplexOperation()`. If called outside a complex operation, the current operation is pushed onto the undo/redo stack.

### EndMulticaretEdit

```csharp
public void EndMulticaretEdit()
```

引擎名：`end_multicaret_edit`

Ends an edit for multiple carets, that was started with `BeginMulticaretEdit()`. If this was the last `EndMulticaretEdit()` and `MergeOverlappingCarets()` was called, carets will be merged.

### GetAutowrapMode

```csharp
public TextServer.AutowrapMode GetAutowrapMode()
```

引擎名：`get_autowrap_mode`

### GetCaretBlinkInterval

```csharp
public float GetCaretBlinkInterval()
```

引擎名：`get_caret_blink_interval`

### GetCaretColumn

```csharp
public int GetCaretColumn(int caretIndex = 0)
```

引擎名：`get_caret_column`

Returns the column the editing caret is at.

### GetCaretCount

```csharp
public int GetCaretCount()
```

引擎名：`get_caret_count`

Returns the number of carets in this `TextEdit`.

### GetCaretDrawPos

```csharp
public Vector2 GetCaretDrawPos(int caretIndex = 0)
```

引擎名：`get_caret_draw_pos`

Returns the caret pixel draw position.

### GetCaretIndexEditOrder

```csharp
public int[] GetCaretIndexEditOrder()
```

引擎名：`get_caret_index_edit_order`

Returns a list of caret indexes in their edit order, this done from bottom to top. Edit order refers to the way actions such as `InsertTextAtCaret()` are applied.

### GetCaretLine

```csharp
public int GetCaretLine(int caretIndex = 0)
```

引擎名：`get_caret_line`

Returns the line the editing caret is on.

### GetCaretType

```csharp
public TextEdit.CaretTypeEnum GetCaretType()
```

引擎名：`get_caret_type`

### GetCaretWrapIndex

```csharp
public int GetCaretWrapIndex(int caretIndex = 0)
```

引擎名：`get_caret_wrap_index`

Returns the wrap index the editing caret is on.

### GetCustomWordSeparators

```csharp
public string GetCustomWordSeparators()
```

引擎名：`get_custom_word_separators`

### GetDrawControlChars

```csharp
public bool GetDrawControlChars()
```

引擎名：`get_draw_control_chars`

### GetFirstNonWhiteSpaceColumn

```csharp
public int GetFirstNonWhiteSpaceColumn(int line)
```

Returns the first column containing a non-whitespace character on the given line. If there is only whitespace, returns the number of characters.

### GetFirstVisibleLine

```csharp
public int GetFirstVisibleLine()
```

引擎名：`get_first_visible_line`

Returns the first visible line.

### GetGutterCount

```csharp
public int GetGutterCount()
```

引擎名：`get_gutter_count`

Returns the number of gutters registered.

### GetGutterName

```csharp
public string GetGutterName(int gutter)
```

引擎名：`get_gutter_name`

Returns the name of the gutter at the given index.

### GetGutterType

```csharp
public TextEdit.GutterType GetGutterType(int gutter)
```

引擎名：`get_gutter_type`

Returns the type of the gutter at the given index. Gutters can contain icons, text, or custom visuals.

### GetGutterWidth

```csharp
public int GetGutterWidth(int gutter)
```

引擎名：`get_gutter_width`

Returns the width of the gutter at the given index.

### GetHScroll

```csharp
public int GetHScroll()
```

引擎名：`get_h_scroll`

### GetHScrollBar

```csharp
public HScrollBar GetHScrollBar()
```

引擎名：`get_h_scroll_bar`

Returns the [`HScrollBar`](HScrollBar.md) used by `TextEdit`.

### GetIndentLevel

```csharp
public int GetIndentLevel(int line)
```

引擎名：`get_indent_level`

Returns the indent level of the given line. This is the number of spaces and tabs at the beginning of the line, with the tabs taking the tab size into account (see `GetTabSize()`).

### GetLanguage

```csharp
public string GetLanguage()
```

引擎名：`get_language`

### GetLastFullVisibleLine

```csharp
public int GetLastFullVisibleLine()
```

引擎名：`get_last_full_visible_line`

Returns the last visible line. Use `GetLastFullVisibleLineWrapIndex()` for the wrap index.

### GetLastFullVisibleLineWrapIndex

```csharp
public int GetLastFullVisibleLineWrapIndex()
```

引擎名：`get_last_full_visible_line_wrap_index`

Returns the last visible wrap index of the last visible line.

### GetLastUnhiddenLine

```csharp
public int GetLastUnhiddenLine()
```

引擎名：`get_last_unhidden_line`

Returns the last unhidden line in the entire `TextEdit`.

### GetLine

```csharp
public string GetLine(int line)
```

引擎名：`get_line`

Returns the text of a specific line.

### GetLineBackgroundColor

```csharp
public Color GetLineBackgroundColor(int line)
```

引擎名：`get_line_background_color`

Returns the custom background color of the given line. If no color is set, returns `Color(0, 0, 0, 0)`.

### GetLineColumnAtPos

```csharp
public Vector2I GetLineColumnAtPos(Vector2I position, bool allowOutOfBounds)
```

引擎名：`get_line_column_at_pos`

Returns the line and column at the given position. In the returned vector, `x` is the column and `y` is the line.

If `clampLine` is `false` and `position` is below the last line, `Vector2i(-1, -1)` is returned.

If `clampColumn` is `false` and `position` is outside the column range of the line, `Vector2i(-1, -1)` is returned.

### GetLineColumnAtPos

```csharp
public Vector2I GetLineColumnAtPos(Vector2I position, bool clampLine = true, bool clampColumn = true)
```

引擎名：`get_line_column_at_pos`

Returns the line and column at the given position. In the returned vector, `x` is the column and `y` is the line.

If `clampLine` is `false` and `position` is below the last line, `Vector2i(-1, -1)` is returned.

If `clampColumn` is `false` and `position` is outside the column range of the line, `Vector2i(-1, -1)` is returned.

### GetLineCount

```csharp
public int GetLineCount()
```

引擎名：`get_line_count`

Returns the number of lines in the text.

### GetLineGutterIcon

```csharp
public Texture2D GetLineGutterIcon(int line, int gutter)
```

引擎名：`get_line_gutter_icon`

Returns the icon currently in `gutter` at `line`. This only works when the gutter type is [`TextEdit.GutterType.Icon`](TextEdit.md) (see `SetGutterType()`).

### GetLineGutterItemColor

```csharp
public Color GetLineGutterItemColor(int line, int gutter)
```

引擎名：`get_line_gutter_item_color`

Returns the color currently in `gutter` at `line`.

### GetLineGutterMetadata

```csharp
public Variant GetLineGutterMetadata(int line, int gutter)
```

引擎名：`get_line_gutter_metadata`

Returns the metadata currently in `gutter` at `line`.

### GetLineGutterText

```csharp
public string GetLineGutterText(int line, int gutter)
```

引擎名：`get_line_gutter_text`

Returns the text currently in `gutter` at `line`. This only works when the gutter type is [`TextEdit.GutterType.String`](TextEdit.md) (see `SetGutterType()`).

### GetLineHeight

```csharp
public int GetLineHeight()
```

引擎名：`get_line_height`

Returns the maximum value of the line height among all lines.

**Note:** The return value is influenced by `line_spacing` and `font_size`. And it will not be less than `1`.

### GetLineRangesFromCarets

```csharp
public Array<Vector2I> GetLineRangesFromCarets(bool onlySelections = false, bool mergeAdjacent = true)
```

引擎名：`get_line_ranges_from_carets`

Returns an [`Collections.Array`](Collections.Array.md) of line ranges where `x` is the first line and `y` is the last line. All lines within these ranges will have a caret on them or be part of a selection. Each line will only be part of one line range, even if it has multiple carets on it.

If a selection's end column (`GetSelectionToColumn()`) is at column `0`, that line will not be included. If a selection begins on the line after another selection ends and `mergeAdjacent` is `true`, or they begin and end on the same line, one line range will include both selections.

### GetLineWidth

```csharp
public int GetLineWidth(int line, int wrapIndex = -1)
```

引擎名：`get_line_width`

Returns the width in pixels of the `wrapIndex` on `line`.

### GetLineWithIme

```csharp
public string GetLineWithIme(int line)
```

引擎名：`get_line_with_ime`

Returns line text as it is currently displayed, including IME composition string.

### GetLineWrapCount

```csharp
public int GetLineWrapCount(int line)
```

引擎名：`get_line_wrap_count`

Returns the number of times the given line is wrapped.

### GetLineWrapIndexAtColumn

```csharp
public int GetLineWrapIndexAtColumn(int line, int column)
```

引擎名：`get_line_wrap_index_at_column`

Returns the wrap index of the given column on the given line. This ranges from `0` to `GetLineWrapCount()`.

### GetLineWrappedText

```csharp
public string[] GetLineWrappedText(int line)
```

引擎名：`get_line_wrapped_text`

Returns an array of `System.String`s representing each wrapped index.

### GetLineWrappingMode

```csharp
public TextEdit.LineWrappingMode GetLineWrappingMode()
```

引擎名：`get_line_wrapping_mode`

### GetLocalMousePos

```csharp
public Vector2 GetLocalMousePos()
```

引擎名：`get_local_mouse_pos`

Returns the local mouse position adjusted for the text direction.

### GetMenu

```csharp
public PopupMenu GetMenu()
```

引擎名：`get_menu`

Returns the [`PopupMenu`](PopupMenu.md) of this `TextEdit`. By default, this menu is displayed when right-clicking on the `TextEdit`.

You can add custom menu items or remove standard ones. Make sure your IDs don't conflict with the standard ones (see [`TextEdit.MenuItems`](TextEdit.md)). For example:

```csharp
public override void _Ready()
{
var menu = GetMenu();
// Remove all items after "Redo".
menu.ItemCount = menu.GetItemIndex(TextEdit.MenuItems.Redo) + 1;
// Add custom items.
menu.AddSeparator();
menu.AddItem("Insert Date", TextEdit.MenuItems.Max + 1);
// Add event handler.
menu.IdPressed += OnItemPressed;
}

public void OnItemPressed(int id)
{
if (id == TextEdit.MenuItems.Max + 1)
{
InsertTextAtCaret(Time.GetDateStringFromSystem());
}
}

```

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`Window.Visible`](Window.md) property.

### GetMinimapLineAtPos

```csharp
public int GetMinimapLineAtPos(Vector2I position)
```

引擎名：`get_minimap_line_at_pos`

Returns the equivalent minimap line at `position`.

### GetMinimapVisibleLines

```csharp
public int GetMinimapVisibleLines()
```

引擎名：`get_minimap_visible_lines`

Returns the number of lines that may be drawn on the minimap.

### GetMinimapWidth

```csharp
public int GetMinimapWidth()
```

引擎名：`get_minimap_width`

### GetNextCompositeCharacterColumn

```csharp
public int GetNextCompositeCharacterColumn(int line, int column)
```

引擎名：`get_next_composite_character_column`

Returns the correct column at the end of a composite character like ❤️‍🩹 (mending heart; Unicode: `U+2764 U+FE0F U+200D U+1FA79`) which is comprised of more than one Unicode code point, if the caret is at the start of the composite character. Also returns the correct column with the caret at mid grapheme and for non-composite characters.

**Note:** To check at caret location use `get_next_composite_character_column(get_caret_line(), get_caret_column())`

### GetNextVisibleLineIndexOffsetFrom

```csharp
public Vector2I GetNextVisibleLineIndexOffsetFrom(int line, int wrapIndex, int visibleAmount)
```

引擎名：`get_next_visible_line_index_offset_from`

Similar to `GetNextVisibleLineOffsetFrom()`, but takes into account the line wrap indexes. In the returned vector, `x` is the line, `y` is the wrap index.

### GetNextVisibleLineOffsetFrom

```csharp
public int GetNextVisibleLineOffsetFrom(int line, int visibleAmount)
```

引擎名：`get_next_visible_line_offset_from`

Returns the count to the next visible line from `line` to `line + visible_amount`. Can also count backwards. For example if a `TextEdit` has 5 lines with lines 2 and 3 hidden, calling this with `line = 1, visible_amount = 1` would return 3.

### GetPlaceholder

```csharp
public string GetPlaceholder()
```

引擎名：`get_placeholder`

### GetPosAtLineColumn

```csharp
public Vector2I GetPosAtLineColumn(int line, int column)
```

引擎名：`get_pos_at_line_column`

Returns the local position for the given `line` and `column`. If `x` or `y` of the returned vector equal `-1`, the position is outside of the viewable area of the control.

**Note:** The Y position corresponds to the bottom side of the line. Use `GetRectAtLineColumn()` to get the top side position.

### GetPreviousCompositeCharacterColumn

```csharp
public int GetPreviousCompositeCharacterColumn(int line, int column)
```

引擎名：`get_previous_composite_character_column`

Returns the correct column at the start of a composite character like ❤️‍🩹 (mending heart; Unicode: `U+2764 U+FE0F U+200D U+1FA79`) which is comprised of more than one Unicode code point, if the caret is at the end of the composite character. Also returns the correct column with the caret at mid grapheme and for non-composite characters.

**Note:** To check at caret location use `get_previous_composite_character_column(get_caret_line(), get_caret_column())`

### GetRectAtLineColumn

```csharp
public Rect2I GetRectAtLineColumn(int line, int column)
```

引擎名：`get_rect_at_line_column`

Returns the local position and size for the grapheme at the given `line` and `column`. If `x` or `y` position of the returned rect equal `-1`, the position is outside of the viewable area of the control.

**Note:** The Y position of the returned rect corresponds to the top side of the line, unlike `GetPosAtLineColumn()` which returns the bottom side.

### GetSavedVersion

```csharp
public uint GetSavedVersion()
```

引擎名：`get_saved_version`

Returns the last tagged saved version from `TagSavedVersion()`.

### GetScrollPosForLine

```csharp
public double GetScrollPosForLine(int line, int wrapIndex = 0)
```

引擎名：`get_scroll_pos_for_line`

Returns the scroll position for `wrapIndex` of `line`.

### GetSelectedText

```csharp
public string GetSelectedText(int caretIndex = -1)
```

引擎名：`get_selected_text`

Returns the text inside the selection of a caret, or all the carets if `caretIndex` is its default value `-1`.

### GetSelectionAtLineColumn

```csharp
public int GetSelectionAtLineColumn(int line, int column, bool includeEdges = true, bool onlySelections = true)
```

引擎名：`get_selection_at_line_column`

Returns the caret index of the selection at the given `line` and `column`, or `-1` if there is none.

If `includeEdges` is `false`, the position must be inside the selection and not at either end. If `onlySelections` is `false`, carets without a selection will also be considered.

### GetSelectionColumn

```csharp
public int GetSelectionColumn(int caretIndex = 0)
```

引擎名：`get_selection_column`

Returns the original start column of the selection.

### GetSelectionFromColumn

```csharp
public int GetSelectionFromColumn(int caretIndex = 0)
```

引擎名：`get_selection_from_column`

Returns the selection begin column. Returns the caret column if there is no selection.

### GetSelectionFromLine

```csharp
public int GetSelectionFromLine(int caretIndex = 0)
```

引擎名：`get_selection_from_line`

Returns the selection begin line. Returns the caret line if there is no selection.

### GetSelectionLine

```csharp
public int GetSelectionLine(int caretIndex = 0)
```

引擎名：`get_selection_line`

Returns the original start line of the selection.

### GetSelectionMode

```csharp
public TextEdit.SelectionMode GetSelectionMode()
```

引擎名：`get_selection_mode`

Returns the current selection mode.

### GetSelectionOriginColumn

```csharp
public int GetSelectionOriginColumn(int caretIndex = 0)
```

引擎名：`get_selection_origin_column`

Returns the origin column of the selection. This is the opposite end from the caret.

### GetSelectionOriginLine

```csharp
public int GetSelectionOriginLine(int caretIndex = 0)
```

引擎名：`get_selection_origin_line`

Returns the origin line of the selection. This is the opposite end from the caret.

### GetSelectionToColumn

```csharp
public int GetSelectionToColumn(int caretIndex = 0)
```

引擎名：`get_selection_to_column`

Returns the selection end column. Returns the caret column if there is no selection.

### GetSelectionToLine

```csharp
public int GetSelectionToLine(int caretIndex = 0)
```

引擎名：`get_selection_to_line`

Returns the selection end line. Returns the caret line if there is no selection.

### GetSortedCarets

```csharp
public int[] GetSortedCarets(bool includeIgnoredCarets = false)
```

引擎名：`get_sorted_carets`

Returns the carets sorted by selection beginning from lowest line and column to highest (from top to bottom of text).

If `includeIgnoredCarets` is `false`, carets from `MulticaretEditIgnoreCaret()` will be ignored.

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

### GetSyntaxHighlighter

```csharp
public SyntaxHighlighter GetSyntaxHighlighter()
```

引擎名：`get_syntax_highlighter`

### GetTabInputMode

```csharp
public bool GetTabInputMode()
```

引擎名：`get_tab_input_mode`

### GetTabSize

```csharp
public int GetTabSize()
```

引擎名：`get_tab_size`

Returns the `TextEdit`'s' tab size.

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

### GetTotalGutterWidth

```csharp
public int GetTotalGutterWidth()
```

引擎名：`get_total_gutter_width`

Returns the total width of all gutters and internal padding.

### GetTotalVisibleLineCount

```csharp
public int GetTotalVisibleLineCount()
```

引擎名：`get_total_visible_line_count`

Returns the total number of lines in the text. This includes wrapped lines and excludes folded lines. If `WrapMode` is set to [`TextEdit.LineWrappingMode.None`](TextEdit.md) and no lines are folded (see [`CodeEdit.IsLineFolded`](CodeEdit.md)) then this is equivalent to `GetLineCount()`. See `GetVisibleLineCountInRange()` for a limited range of lines.

### GetVScroll

```csharp
public double GetVScroll()
```

引擎名：`get_v_scroll`

### GetVScrollBar

```csharp
public VScrollBar GetVScrollBar()
```

引擎名：`get_v_scroll_bar`

Returns the [`VScrollBar`](VScrollBar.md) of the `TextEdit`.

### GetVScrollSpeed

```csharp
public float GetVScrollSpeed()
```

引擎名：`get_v_scroll_speed`

### GetVersion

```csharp
public uint GetVersion()
```

引擎名：`get_version`

Returns the current version of the `TextEdit`. The version is a count of recorded operations by the undo/redo history.

### GetVirtualKeyboardShowOnFocus

```csharp
public bool GetVirtualKeyboardShowOnFocus()
```

引擎名：`get_virtual_keyboard_show_on_focus`

### GetVisibleLineCount

```csharp
public int GetVisibleLineCount()
```

引擎名：`get_visible_line_count`

Returns the number of lines that can visually fit, rounded down, based on this control's height.

### GetVisibleLineCountInRange

```csharp
public int GetVisibleLineCountInRange(int fromLine, int toLine)
```

引擎名：`get_visible_line_count_in_range`

Returns the total number of lines between `fromLine` and `toLine` (inclusive) in the text. This includes wrapped lines and excludes folded lines. If the range covers all lines it is equivalent to `GetTotalVisibleLineCount()`.

### GetWordAtPos

```csharp
public string GetWordAtPos(Vector2 position)
```

引擎名：`get_word_at_pos`

Returns the word at `position`.

### GetWordUnderCaret

```csharp
public string GetWordUnderCaret(int caretIndex = -1)
```

引擎名：`get_word_under_caret`

Returns a `System.String` text with the word under the caret's location.

### HasImeText

```csharp
public bool HasImeText()
```

引擎名：`has_ime_text`

Returns `true` if the user has text in the [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) (IME).

### HasRedo

```csharp
public bool HasRedo()
```

引擎名：`has_redo`

Returns `true` if a "redo" action is available.

### HasSelection

```csharp
public bool HasSelection(int caretIndex = -1)
```

引擎名：`has_selection`

Returns `true` if the user has selected text.

### HasUndo

```csharp
public bool HasUndo()
```

引擎名：`has_undo`

Returns `true` if an "undo" action is available.

### InsertLineAt

```csharp
public void InsertLineAt(int line, string text)
```

引擎名：`insert_line_at`

Inserts a new line with `text` at `line`.

### InsertText

```csharp
public void InsertText(string text, int line, int column, bool beforeSelectionBegin = true, bool beforeSelectionEnd = false)
```

引擎名：`insert_text`

Inserts the `text` at `line` and `column`.

If `beforeSelectionBegin` is `true`, carets and selections that begin at `line` and `column` will moved to the end of the inserted text, along with all carets after it.

If `beforeSelectionEnd` is `true`, selections that end at `line` and `column` will be extended to the end of the inserted text. These parameters can be used to insert text inside of or outside of selections.

### InsertTextAtCaret

```csharp
public void InsertTextAtCaret(string text, int caretIndex = -1)
```

引擎名：`insert_text_at_caret`

Insert the specified text at the caret position.

### IsBackspaceDeletesCompositeCharacterEnabled

```csharp
public bool IsBackspaceDeletesCompositeCharacterEnabled()
```

引擎名：`is_backspace_deletes_composite_character_enabled`

### IsCaretAfterSelectionOrigin

```csharp
public bool IsCaretAfterSelectionOrigin(int caretIndex = 0)
```

引擎名：`is_caret_after_selection_origin`

Returns `true` if the caret of the selection is after the selection origin. This can be used to determine the direction of the selection.

### IsCaretBlinkEnabled

```csharp
public bool IsCaretBlinkEnabled()
```

引擎名：`is_caret_blink_enabled`

### IsCaretMidGraphemeEnabled

```csharp
public bool IsCaretMidGraphemeEnabled()
```

引擎名：`is_caret_mid_grapheme_enabled`

### IsCaretVisible

```csharp
public bool IsCaretVisible(int caretIndex = 0)
```

引擎名：`is_caret_visible`

Returns `true` if the caret is visible, `false` otherwise. A caret will be considered hidden if it is outside the scrollable area when scrolling is enabled.

**Note:** `IsCaretVisible()` does not account for a caret being off-screen if it is still within the scrollable area. It will return `true` even if the caret is off-screen as long as it meets `TextEdit`'s own conditions for being visible. This includes uses of `ScrollFitContentWidth` and `ScrollFitContentHeight` that cause the `TextEdit` to expand beyond the viewport's bounds.

**Note:** This method does *not* guarantee an accurate visibility check immediately after setting the caret position. The correct value may only be available in the next frame after the `TextEdit` has finished drawing. This also applies to any operation that causes the `TextEdit` to change in size.

### IsContextMenuEnabled

```csharp
public bool IsContextMenuEnabled()
```

引擎名：`is_context_menu_enabled`

### IsCustomWordSeparatorsEnabled

```csharp
public bool IsCustomWordSeparatorsEnabled()
```

引擎名：`is_custom_word_separators_enabled`

### IsDefaultWordSeparatorsEnabled

```csharp
public bool IsDefaultWordSeparatorsEnabled()
```

引擎名：`is_default_word_separators_enabled`

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

### IsDraggingCursor

```csharp
public bool IsDraggingCursor()
```

引擎名：`is_dragging_cursor`

Returns `true` if the user is dragging their mouse for scrolling, selecting, or text dragging.

### IsDrawingCaretWhenEditableDisabled

```csharp
public bool IsDrawingCaretWhenEditableDisabled()
```

引擎名：`is_drawing_caret_when_editable_disabled`

### IsDrawingMinimap

```csharp
public bool IsDrawingMinimap()
```

引擎名：`is_drawing_minimap`

### IsDrawingSpaces

```csharp
public bool IsDrawingSpaces()
```

引擎名：`is_drawing_spaces`

### IsDrawingTabs

```csharp
public bool IsDrawingTabs()
```

引擎名：`is_drawing_tabs`

### IsEditable

```csharp
public bool IsEditable()
```

引擎名：`is_editable`

### IsEmojiMenuEnabled

```csharp
public bool IsEmojiMenuEnabled()
```

引擎名：`is_emoji_menu_enabled`

### IsEmptySelectionClipboardEnabled

```csharp
public bool IsEmptySelectionClipboardEnabled()
```

引擎名：`is_empty_selection_clipboard_enabled`

### IsFitContentHeightEnabled

```csharp
public bool IsFitContentHeightEnabled()
```

引擎名：`is_fit_content_height_enabled`

### IsFitContentWidthEnabled

```csharp
public bool IsFitContentWidthEnabled()
```

引擎名：`is_fit_content_width_enabled`

### IsGutterClickable

```csharp
public bool IsGutterClickable(int gutter)
```

引擎名：`is_gutter_clickable`

Returns `true` if the gutter at the given index is clickable. See `SetGutterClickable()`.

### IsGutterDrawn

```csharp
public bool IsGutterDrawn(int gutter)
```

引擎名：`is_gutter_drawn`

Returns `true` if the gutter at the given index is currently drawn. See `SetGutterDraw()`.

### IsGutterOverwritable

```csharp
public bool IsGutterOverwritable(int gutter)
```

引擎名：`is_gutter_overwritable`

Returns `true` if the gutter at the given index is overwritable. See `SetGutterOverwritable()`.

### IsHighlightAllOccurrencesEnabled

```csharp
public bool IsHighlightAllOccurrencesEnabled()
```

引擎名：`is_highlight_all_occurrences_enabled`

### IsHighlightCurrentLineEnabled

```csharp
public bool IsHighlightCurrentLineEnabled()
```

引擎名：`is_highlight_current_line_enabled`

### IsInMulitcaretEdit

```csharp
public bool IsInMulitcaretEdit()
```

引擎名：`is_in_mulitcaret_edit`

Returns `true` if a `BeginMulticaretEdit()` has been called and `EndMulticaretEdit()` has not yet been called.

### IsIndentWrappedLines

```csharp
public bool IsIndentWrappedLines()
```

引擎名：`is_indent_wrapped_lines`

### IsLineGutterClickable

```csharp
public bool IsLineGutterClickable(int line, int gutter)
```

引擎名：`is_line_gutter_clickable`

Returns `true` if the gutter at the given index on the given line is clickable. See `SetLineGutterClickable()`.

### IsLineInViewport

```csharp
public bool IsLineInViewport(int line)
```

引擎名：`is_line_in_viewport`

Returns `true` if the given line is within the scope of the scrollable area of the viewport.

### IsLineWrapped

```csharp
public bool IsLineWrapped(int line)
```

引擎名：`is_line_wrapped`

Returns if the given line is wrapped.

### IsMenuVisible

```csharp
public bool IsMenuVisible()
```

引擎名：`is_menu_visible`

Returns `true` if the menu is visible. Use this instead of `get_menu().visible` to improve performance (so the creation of the menu is avoided). See `GetMenu()`.

### IsMiddleMousePasteEnabled

```csharp
public bool IsMiddleMousePasteEnabled()
```

引擎名：`is_middle_mouse_paste_enabled`

### IsMouseOverSelection

```csharp
public bool IsMouseOverSelection(bool edges, int caretIndex = -1)
```

引擎名：`is_mouse_over_selection`

Returns `true` if the mouse is over a selection. If `edges` is `true`, the edges are considered part of the selection.

### IsMoveCaretOnRightClickEnabled

```csharp
public bool IsMoveCaretOnRightClickEnabled()
```

引擎名：`is_move_caret_on_right_click_enabled`

### IsMultipleCaretsEnabled

```csharp
public bool IsMultipleCaretsEnabled()
```

引擎名：`is_multiple_carets_enabled`

### IsOvertypeModeEnabled

```csharp
public bool IsOvertypeModeEnabled()
```

引擎名：`is_overtype_mode_enabled`

Returns `true` if overtype mode is enabled. See `SetOvertypeModeEnabled()`.

### IsScrollPastEndOfFileEnabled

```csharp
public bool IsScrollPastEndOfFileEnabled()
```

引擎名：`is_scroll_past_end_of_file_enabled`

### IsSelectingEnabled

```csharp
public bool IsSelectingEnabled()
```

引擎名：`is_selecting_enabled`

### IsShortcutKeysEnabled

```csharp
public bool IsShortcutKeysEnabled()
```

引擎名：`is_shortcut_keys_enabled`

### IsSmoothScrollEnabled

```csharp
public bool IsSmoothScrollEnabled()
```

引擎名：`is_smooth_scroll_enabled`

### IsVirtualKeyboardEnabled

```csharp
public bool IsVirtualKeyboardEnabled()
```

引擎名：`is_virtual_keyboard_enabled`

### MenuOption

```csharp
public void MenuOption(int option)
```

引擎名：`menu_option`

Executes a given action as defined in the [`TextEdit.MenuItems`](TextEdit.md) enum.

### MergeGutters

```csharp
public void MergeGutters(int fromLine, int toLine)
```

引擎名：`merge_gutters`

Merge the gutters from `fromLine` into `toLine`. Only overwritable gutters will be copied. See `SetGutterOverwritable()`.

### MergeOverlappingCarets

```csharp
public void MergeOverlappingCarets()
```

引擎名：`merge_overlapping_carets`

Merges any overlapping carets. Will favor the newest caret, or the caret with a selection.

If `IsInMulitcaretEdit()` is `true`, the merge will be queued to happen at the end of the multicaret edit. See `BeginMulticaretEdit()` and `EndMulticaretEdit()`.

**Note:** This is not called when a caret changes position but after certain actions, so it is possible to get into a state where carets overlap.

### MulticaretEditIgnoreCaret

```csharp
public bool MulticaretEditIgnoreCaret(int caretIndex)
```

引擎名：`multicaret_edit_ignore_caret`

Returns `true` if the given `caretIndex` should be ignored as part of a multicaret edit. See `BeginMulticaretEdit()` and `EndMulticaretEdit()`. Carets that should be ignored are ones that were part of removed text and will likely be merged at the end of the edit, or carets that were added during the edit.

It is recommended to `continue` within a loop iterating on multiple carets if a caret should be ignored.

### Paste

```csharp
public void Paste(int caretIndex = -1)
```

引擎名：`paste`

Paste at the current location. Can be overridden with `_Paste()`.

### PastePrimaryClipboard

```csharp
public void PastePrimaryClipboard(int caretIndex = -1)
```

引擎名：`paste_primary_clipboard`

Pastes the primary clipboard.

### Redo

```csharp
public void Redo()
```

引擎名：`redo`

Perform redo operation.

### RemoveCaret

```csharp
public void RemoveCaret(int caret)
```

引擎名：`remove_caret`

Removes the given caret index.

**Note:** This can result in adjustment of all other caret indices.

### RemoveGutter

```csharp
public void RemoveGutter(int gutter)
```

引擎名：`remove_gutter`

Removes the gutter at the given index.

### RemoveLineAt

```csharp
public void RemoveLineAt(int line, bool moveCaretsDown = true)
```

引擎名：`remove_line_at`

Removes the line of text at `line`. Carets on this line will attempt to match their previous visual x position.

If `moveCaretsDown` is `true` carets will move to the next line down, otherwise carets will move up.

### RemoveSecondaryCarets

```csharp
public void RemoveSecondaryCarets()
```

引擎名：`remove_secondary_carets`

Removes all additional carets.

### RemoveText

```csharp
public void RemoveText(int fromLine, int fromColumn, int toLine, int toColumn)
```

引擎名：`remove_text`

Removes text between the given positions.

### Search

```csharp
public Vector2I Search(string text, uint flags, int fromLine, int fromColumn)
```

引擎名：`search`

Perform a search inside the text. Search flags can be specified in the [`TextEdit.SearchFlags`](TextEdit.md) enum.

In the returned vector, `x` is the column, `y` is the line. If no results are found, both are equal to `-1`.

```csharp
Vector2I result = Search("print", (uint)TextEdit.SearchFlags.WholeWords, 0, 0);
if (result.X != -1)
{
// Result found.
int lineNumber = result.Y;
int columnNumber = result.X;
}

```

### Select

```csharp
public void Select(int originLine, int originColumn, int caretLine, int caretColumn, int caretIndex = 0)
```

引擎名：`select`

Selects text from `originLine` and `originColumn` to `caretLine` and `caretColumn` for the given `caretIndex`. This moves the selection origin and the caret. If the positions are the same, the selection will be deselected.

If `SelectingEnabled` is `false`, no selection will occur.

**Note:** If supporting multiple carets this will not check for any overlap. See `MergeOverlappingCarets()`.

### SelectAll

```csharp
public void SelectAll()
```

引擎名：`select_all`

Select all the text.

If `SelectingEnabled` is `false`, no selection will occur.

### SelectWordUnderCaret

```csharp
public void SelectWordUnderCaret(int caretIndex = -1)
```

引擎名：`select_word_under_caret`

Selects the word under the caret.

### SetAutowrapMode

```csharp
public void SetAutowrapMode(TextServer.AutowrapMode autowrapMode)
```

引擎名：`set_autowrap_mode`

### SetBackspaceDeletesCompositeCharacterEnabled

```csharp
public void SetBackspaceDeletesCompositeCharacterEnabled(bool enable)
```

引擎名：`set_backspace_deletes_composite_character_enabled`

### SetCaretBlinkEnabled

```csharp
public void SetCaretBlinkEnabled(bool enable)
```

引擎名：`set_caret_blink_enabled`

### SetCaretBlinkInterval

```csharp
public void SetCaretBlinkInterval(float interval)
```

引擎名：`set_caret_blink_interval`

### SetCaretColumn

```csharp
public void SetCaretColumn(int column, bool adjustViewport = true, int caretIndex = 0)
```

引擎名：`set_caret_column`

Moves the caret to the specified `column` index.

If `adjustViewport` is `true`, the viewport will center at the caret position after the move occurs.

**Note:** If supporting multiple carets this will not check for any overlap. See `MergeOverlappingCarets()`.

### SetCaretLine

```csharp
public void SetCaretLine(int line, bool adjustViewport = true, bool canBeHidden = true, int wrapIndex = 0, int caretIndex = 0)
```

引擎名：`set_caret_line`

Moves the caret to the specified `line` index. The caret column will be moved to the same visual position it was at the last time `SetCaretColumn()` was called, or clamped to the end of the line.

If `adjustViewport` is `true`, the viewport will center at the caret position after the move occurs.

If `canBeHidden` is `true`, the specified `line` can be hidden.

If `wrapIndex` is `-1`, the caret column will be clamped to the `line`'s length. If `wrapIndex` is greater than `-1`, the column will be moved to attempt to match the visual x position on the line's `wrapIndex` to the position from the last time `SetCaretColumn()` was called.

**Note:** If supporting multiple carets this will not check for any overlap. See `MergeOverlappingCarets()`.

### SetCaretMidGraphemeEnabled

```csharp
public void SetCaretMidGraphemeEnabled(bool enabled)
```

引擎名：`set_caret_mid_grapheme_enabled`

### SetCaretType

```csharp
public void SetCaretType(TextEdit.CaretTypeEnum type)
```

引擎名：`set_caret_type`

### SetContextMenuEnabled

```csharp
public void SetContextMenuEnabled(bool enabled)
```

引擎名：`set_context_menu_enabled`

### SetCustomWordSeparators

```csharp
public void SetCustomWordSeparators(string customWordSeparators)
```

引擎名：`set_custom_word_separators`

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

### SetDrawCaretWhenEditableDisabled

```csharp
public void SetDrawCaretWhenEditableDisabled(bool enable)
```

引擎名：`set_draw_caret_when_editable_disabled`

### SetDrawControlChars

```csharp
public void SetDrawControlChars(bool enabled)
```

引擎名：`set_draw_control_chars`

### SetDrawMinimap

```csharp
public void SetDrawMinimap(bool enabled)
```

引擎名：`set_draw_minimap`

### SetDrawSpaces

```csharp
public void SetDrawSpaces(bool enabled)
```

引擎名：`set_draw_spaces`

### SetDrawTabs

```csharp
public void SetDrawTabs(bool enabled)
```

引擎名：`set_draw_tabs`

### SetEditable

```csharp
public void SetEditable(bool enabled)
```

引擎名：`set_editable`

### SetEmojiMenuEnabled

```csharp
public void SetEmojiMenuEnabled(bool enable)
```

引擎名：`set_emoji_menu_enabled`

### SetEmptySelectionClipboardEnabled

```csharp
public void SetEmptySelectionClipboardEnabled(bool enabled)
```

引擎名：`set_empty_selection_clipboard_enabled`

### SetFitContentHeightEnabled

```csharp
public void SetFitContentHeightEnabled(bool enabled)
```

引擎名：`set_fit_content_height_enabled`

### SetFitContentWidthEnabled

```csharp
public void SetFitContentWidthEnabled(bool enabled)
```

引擎名：`set_fit_content_width_enabled`

### SetGutterClickable

```csharp
public void SetGutterClickable(int gutter, bool clickable)
```

引擎名：`set_gutter_clickable`

If `true`, the mouse cursor will change to a pointing hand ([`Control.CursorShape.PointingHand`](Control.md)) when hovering over the gutter at the given index. See `IsGutterClickable()` and `SetLineGutterClickable()`.

### SetGutterCustomDraw

```csharp
public void SetGutterCustomDraw(int column, Callable drawCallback)
```

引擎名：`set_gutter_custom_draw`

Set a custom draw callback for the gutter at the given index. `drawCallback` must take the following arguments: A line index `System.Int32`, a gutter index `System.Int32`, and an area [`Rect2`](Rect2.md). This callback only works when the gutter type is [`TextEdit.GutterType.Custom`](TextEdit.md) (see `SetGutterType()`).

### SetGutterDraw

```csharp
public void SetGutterDraw(int gutter, bool draw)
```

引擎名：`set_gutter_draw`

If `true`, the gutter at the given index is drawn. The gutter type (`SetGutterType()`) determines how it is drawn. See `IsGutterDrawn()`.

### SetGutterName

```csharp
public void SetGutterName(int gutter, string name)
```

引擎名：`set_gutter_name`

Sets the name of the gutter at the given index.

### SetGutterOverwritable

```csharp
public void SetGutterOverwritable(int gutter, bool overwritable)
```

引擎名：`set_gutter_overwritable`

If `true`, the line data of the gutter at the given index can be overridden when using `MergeGutters()`. See `IsGutterOverwritable()`.

### SetGutterType

```csharp
public void SetGutterType(int gutter, TextEdit.GutterType type)
```

引擎名：`set_gutter_type`

Sets the type of gutter at the given index. Gutters can contain icons, text, or custom visuals.

### SetGutterWidth

```csharp
public void SetGutterWidth(int gutter, int width)
```

引擎名：`set_gutter_width`

Set the width of the gutter at the given index.

### SetHScroll

```csharp
public void SetHScroll(int value)
```

引擎名：`set_h_scroll`

### SetHighlightAllOccurrences

```csharp
public void SetHighlightAllOccurrences(bool enabled)
```

引擎名：`set_highlight_all_occurrences`

### SetHighlightCurrentLine

```csharp
public void SetHighlightCurrentLine(bool enabled)
```

引擎名：`set_highlight_current_line`

### SetIndentWrappedLines

```csharp
public void SetIndentWrappedLines(bool enabled)
```

引擎名：`set_indent_wrapped_lines`

### SetLanguage

```csharp
public void SetLanguage(string language)
```

引擎名：`set_language`

### SetLine

```csharp
public void SetLine(int line, string newText)
```

引擎名：`set_line`

Sets the text for a specific `line`.

Carets on the line will attempt to keep their visual x position.

### SetLineAsCenterVisible

```csharp
public void SetLineAsCenterVisible(int line, int wrapIndex = 0)
```

引擎名：`set_line_as_center_visible`

Positions the `wrapIndex` of `line` at the center of the viewport.

### SetLineAsFirstVisible

```csharp
public void SetLineAsFirstVisible(int line, int wrapIndex = 0)
```

引擎名：`set_line_as_first_visible`

Positions the `wrapIndex` of `line` at the top of the viewport.

### SetLineAsLastVisible

```csharp
public void SetLineAsLastVisible(int line, int wrapIndex = 0)
```

引擎名：`set_line_as_last_visible`

Positions the `wrapIndex` of `line` at the bottom of the viewport.

### SetLineBackgroundColor

```csharp
public void SetLineBackgroundColor(int line, Color color)
```

引擎名：`set_line_background_color`

Sets the custom background color of the given line. If transparent, this color is applied on top of the default background color (See `background_color`). If set to `Color(0, 0, 0, 0)`, no additional color is applied.

### SetLineGutterClickable

```csharp
public void SetLineGutterClickable(int line, int gutter, bool clickable)
```

引擎名：`set_line_gutter_clickable`

If `clickable` is `true`, makes the `gutter` on the given `line` clickable. This is like `SetGutterClickable()`, but for a single line. If `IsGutterClickable()` is `true`, this will not have any effect. See `IsLineGutterClickable()` and `GutterClicked`.

### SetLineGutterIcon

```csharp
public void SetLineGutterIcon(int line, int gutter, Texture2D icon)
```

引擎名：`set_line_gutter_icon`

Sets the icon for `gutter` on `line` to `icon`. This only works when the gutter type is [`TextEdit.GutterType.Icon`](TextEdit.md) (see `SetGutterType()`).

### SetLineGutterItemColor

```csharp
public void SetLineGutterItemColor(int line, int gutter, Color color)
```

引擎名：`set_line_gutter_item_color`

Sets the color for `gutter` on `line` to `color`.

### SetLineGutterMetadata

```csharp
public void SetLineGutterMetadata(int line, int gutter, Variant metadata)
```

引擎名：`set_line_gutter_metadata`

Sets the metadata for `gutter` on `line` to `metadata`.

### SetLineGutterText

```csharp
public void SetLineGutterText(int line, int gutter, string text)
```

引擎名：`set_line_gutter_text`

Sets the text for `gutter` on `line` to `text`. This only works when the gutter type is [`TextEdit.GutterType.String`](TextEdit.md) (see `SetGutterType()`).

### SetLineWrappingMode

```csharp
public void SetLineWrappingMode(TextEdit.LineWrappingMode mode)
```

引擎名：`set_line_wrapping_mode`

### SetMiddleMousePasteEnabled

```csharp
public void SetMiddleMousePasteEnabled(bool enabled)
```

引擎名：`set_middle_mouse_paste_enabled`

### SetMinimapWidth

```csharp
public void SetMinimapWidth(int width)
```

引擎名：`set_minimap_width`

### SetMoveCaretOnRightClickEnabled

```csharp
public void SetMoveCaretOnRightClickEnabled(bool enable)
```

引擎名：`set_move_caret_on_right_click_enabled`

### SetMultipleCaretsEnabled

```csharp
public void SetMultipleCaretsEnabled(bool enabled)
```

引擎名：`set_multiple_carets_enabled`

### SetOvertypeModeEnabled

```csharp
public void SetOvertypeModeEnabled(bool enabled)
```

引擎名：`set_overtype_mode_enabled`

If `true`, enables overtype mode. In this mode, typing overrides existing text instead of inserting text. The `ProjectSettings.input/ui_text_toggle_insert_mode` action toggles overtype mode. See `IsOvertypeModeEnabled()`.

### SetPlaceholder

```csharp
public void SetPlaceholder(string text)
```

引擎名：`set_placeholder`

### SetScrollPastEndOfFileEnabled

```csharp
public void SetScrollPastEndOfFileEnabled(bool enable)
```

引擎名：`set_scroll_past_end_of_file_enabled`

### SetSearchFlags

```csharp
public void SetSearchFlags(uint flags)
```

引擎名：`set_search_flags`

Sets the search `flags`. This is used with `SetSearchText()` to highlight occurrences of the searched text. Search flags can be specified from the [`TextEdit.SearchFlags`](TextEdit.md) enum.

### SetSearchText

```csharp
public void SetSearchText(string searchText)
```

引擎名：`set_search_text`

Sets the search text. See `SetSearchFlags()`.

### SetSelectingEnabled

```csharp
public void SetSelectingEnabled(bool enable)
```

引擎名：`set_selecting_enabled`

### SetSelectionMode

```csharp
public void SetSelectionMode(TextEdit.SelectionMode mode)
```

引擎名：`set_selection_mode`

Sets the current selection mode.

### SetSelectionMode

```csharp
public void SetSelectionMode(TextEdit.SelectionMode mode, int line, int column, int caretIndex)
```

引擎名：`set_selection_mode`

Sets the current selection mode.

### SetSelectionOriginColumn

```csharp
public void SetSelectionOriginColumn(int column, int caretIndex = 0)
```

引擎名：`set_selection_origin_column`

Sets the selection origin column to the `column` for the given `caretIndex`. If the selection origin is moved to the caret position, the selection will deselect.

### SetSelectionOriginLine

```csharp
public void SetSelectionOriginLine(int line, bool canBeHidden = true, int wrapIndex = -1, int caretIndex = 0)
```

引擎名：`set_selection_origin_line`

Sets the selection origin line to the `line` for the given `caretIndex`. If the selection origin is moved to the caret position, the selection will deselect.

If `canBeHidden` is `false`, The line will be set to the nearest unhidden line below or above.

If `wrapIndex` is `-1`, the selection origin column will be clamped to the `line`'s length. If `wrapIndex` is greater than `-1`, the column will be moved to attempt to match the visual x position on the line's `wrapIndex` to the position from the last time `SetSelectionOriginColumn()` or `Select()` was called.

### SetShortcutKeysEnabled

```csharp
public void SetShortcutKeysEnabled(bool enabled)
```

引擎名：`set_shortcut_keys_enabled`

### SetSmoothScrollEnabled

```csharp
public void SetSmoothScrollEnabled(bool enable)
```

引擎名：`set_smooth_scroll_enabled`

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

### SetSyntaxHighlighter

```csharp
public void SetSyntaxHighlighter(SyntaxHighlighter syntaxHighlighter)
```

引擎名：`set_syntax_highlighter`

### SetTabInputMode

```csharp
public void SetTabInputMode(bool enabled)
```

引擎名：`set_tab_input_mode`

### SetTabSize

```csharp
public void SetTabSize(int size)
```

引擎名：`set_tab_size`

Sets the tab size for the `TextEdit` to use.

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

### SetTooltipRequestFunc

```csharp
public void SetTooltipRequestFunc(Callable callback)
```

引擎名：`set_tooltip_request_func`

Provide custom tooltip text. The callback method must take the following args: `hovered_word: String`.

### SetUseCustomWordSeparators

```csharp
public void SetUseCustomWordSeparators(bool enabled)
```

引擎名：`set_use_custom_word_separators`

### SetUseDefaultWordSeparators

```csharp
public void SetUseDefaultWordSeparators(bool enabled)
```

引擎名：`set_use_default_word_separators`

### SetVScroll

```csharp
public void SetVScroll(double value)
```

引擎名：`set_v_scroll`

### SetVScrollSpeed

```csharp
public void SetVScrollSpeed(float speed)
```

引擎名：`set_v_scroll_speed`

### SetVirtualKeyboardEnabled

```csharp
public void SetVirtualKeyboardEnabled(bool enabled)
```

引擎名：`set_virtual_keyboard_enabled`

### SetVirtualKeyboardShowOnFocus

```csharp
public void SetVirtualKeyboardShowOnFocus(bool showOnFocus)
```

引擎名：`set_virtual_keyboard_show_on_focus`

### SkipSelectionForNextOccurrence

```csharp
public void SkipSelectionForNextOccurrence()
```

引擎名：`skip_selection_for_next_occurrence`

Moves a selection and a caret for the next occurrence of the current selection. If there is no active selection, moves to the next occurrence of the word under caret.

### StartAction

```csharp
public void StartAction(TextEdit.EditAction action)
```

引擎名：`start_action`

Starts an action, will end the current action if `action` is different.

An action will also end after a call to `EndAction()`, after `ProjectSettings.gui/timers/text_edit_idle_detect_sec` is triggered or a new undoable step outside the `StartAction()` and `EndAction()` calls.

### SwapLines

```csharp
public void SwapLines(int fromLine, int toLine)
```

引擎名：`swap_lines`

Swaps the two lines. Carets will be swapped with the lines.

### TagSavedVersion

```csharp
public void TagSavedVersion()
```

引擎名：`tag_saved_version`

Tag the current version as saved.

### Undo

```csharp
public void Undo()
```

引擎名：`undo`

Perform undo operation.

### _Backspace

```csharp
public virtual void _Backspace(int caretIndex)
```

引擎名：`_backspace`

Override this method to define what happens when the user presses the backspace key.

### _Copy

```csharp
public virtual void _Copy(int caretIndex)
```

引擎名：`_copy`

Override this method to define what happens when the user performs a copy operation.

### _Cut

```csharp
public virtual void _Cut(int caretIndex)
```

引擎名：`_cut`

Override this method to define what happens when the user performs a cut operation.

### _HandleUnicodeInput

```csharp
public virtual void _HandleUnicodeInput(int unicodeChar, int caretIndex)
```

引擎名：`_handle_unicode_input`

Override this method to define what happens when the user types in the provided key `unicodeChar`.

### _Paste

```csharp
public virtual void _Paste(int caretIndex)
```

引擎名：`_paste`

Override this method to define what happens when the user performs a paste operation.

### _PastePrimaryClipboard

```csharp
public virtual void _PastePrimaryClipboard(int caretIndex)
```

引擎名：`_paste_primary_clipboard`

Override this method to define what happens when the user performs a paste operation with middle mouse button.

**Note:** This method is only implemented on Linux.

## 事件（信号）

### CaretChanged

```csharp
public event Action CaretChanged
```

引擎名：`caret_changed`

Emitted when any caret changes position.

### GutterAdded

```csharp
public event Action GutterAdded
```

引擎名：`gutter_added`

Emitted when a gutter is added.

### GutterClicked

```csharp
public event TextEdit.GutterClickedEventHandler GutterClicked
```

引擎名：`gutter_clicked`

Emitted when a gutter is clicked.

### GutterRemoved

```csharp
public event Action GutterRemoved
```

引擎名：`gutter_removed`

Emitted when a gutter is removed.

### LinesEditedFrom

```csharp
public event TextEdit.LinesEditedFromEventHandler LinesEditedFrom
```

引擎名：`lines_edited_from`

Emitted immediately when the text changes.

When text is added `fromLine` will be less than `toLine`. On a remove `toLine` will be less than `fromLine`.

### TextChanged

```csharp
public event Action TextChanged
```

引擎名：`text_changed`

Emitted when the text changes.

### TextSet

```csharp
public event Action TextSet
```

引擎名：`text_set`

Emitted when `Clear()` is called or `Text` is set.

## CaretTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Line = 0`：Vertical line caret.
- `Block = 1`：Block caret.

## EditAction

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No current action.
- `Typing = 1`：A typing action.
- `Backspace = 2`：A backwards delete action.
- `Delete = 3`：A forward delete action.

## GutterClickedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TextEdit.GutterClicked`](TextEdit.md) event of a [`TextEdit`](TextEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void GutterClickedEventHandler(long line, long gutter)
```

## GutterType

命名空间 `Godot`；enum

### 枚举值

- `String = 0`：When a gutter is set to string using [`TextEdit.SetGutterType`](TextEdit.md), it is used to contain text set via the [`TextEdit.SetLineGutterText`](TextEdit.md) method.
- `Icon = 1`：When a gutter is set to icon using [`TextEdit.SetGutterType`](TextEdit.md), it is used to contain an icon set via the [`TextEdit.SetLineGutterIcon`](TextEdit.md) method.
- `Custom = 2`：When a gutter is set to custom using [`TextEdit.SetGutterType`](TextEdit.md), it is used to contain custom visuals controlled by a callback method set via the [`TextEdit.SetGutterCustomDraw`](TextEdit.md) method.

## LineWrappingMode

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Line wrapping is disabled.
- `Boundary = 1`：Line wrapping occurs at the control boundary, beyond what would normally be visible.

## LinesEditedFromEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TextEdit.LinesEditedFrom`](TextEdit.md) event of a [`TextEdit`](TextEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void LinesEditedFromEventHandler(long fromLine, long toLine)
```

## MenuItems

命名空间 `Godot`；enum

### 枚举值

- `Cut = 0`：Cuts (copies and clears) the selected text.
- `Copy = 1`：Copies the selected text.
- `Paste = 2`：Pastes the clipboard text over the selected text (or at the cursor's position).
- `Clear = 3`：Erases the whole [`TextEdit`](TextEdit.md) text.
- `SelectAll = 4`：Selects the whole [`TextEdit`](TextEdit.md) text.
- `Undo = 5`：Undoes the previous action.
- `Redo = 6`：Redoes the previous action.
- `SubmenuTextDir = 7`：ID of "Text Writing Direction" submenu.
- `DirInherited = 8`：Sets text direction to inherited.
- `DirAuto = 9`：Sets text direction to automatic.
- `DirLtr = 10`：Sets text direction to left-to-right.
- `DirRtl = 11`：Sets text direction to right-to-left.
- `DisplayUcc = 12`：Toggles control character display.
- `SubmenuInsertUcc = 13`：ID of "Insert Control Character" submenu.
- `InsertLrm = 14`：Inserts left-to-right mark (LRM) character.
- `InsertRlm = 15`：Inserts right-to-left mark (RLM) character.
- `InsertLre = 16`：Inserts start of left-to-right embedding (LRE) character.
- `InsertRle = 17`：Inserts start of right-to-left embedding (RLE) character.
- `InsertLro = 18`：Inserts start of left-to-right override (LRO) character.
- `InsertRlo = 19`：Inserts start of right-to-left override (RLO) character.
- `InsertPdf = 20`：Inserts pop direction formatting (PDF) character.
- `InsertAlm = 21`：Inserts Arabic letter mark (ALM) character.
- `InsertLri = 22`：Inserts left-to-right isolate (LRI) character.
- `InsertRli = 23`：Inserts right-to-left isolate (RLI) character.
- `InsertFsi = 24`：Inserts first strong isolate (FSI) character.
- `InsertPdi = 25`：Inserts pop direction isolate (PDI) character.
- `InsertZwj = 26`：Inserts zero width joiner (ZWJ) character.
- `InsertZwnj = 27`：Inserts zero width non-joiner (ZWNJ) character.
- `InsertWj = 28`：Inserts word joiner (WJ) character.
- `InsertShy = 29`：Inserts soft hyphen (SHY) character.
- `EmojiAndSymbol = 30`：Opens system emoji and symbol picker.
- `Max = 31`：Represents the size of the `TextEdit.MenuItems` enum.

## SearchFlags

命名空间 `Godot`；enum

### 枚举值

- `MatchCase = 1`：Match case when searching.
- `WholeWords = 2`：Match whole words when searching.
- `Backwards = 4`：Search from end to beginning.

## SelectionMode

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Not selecting.
- `Shift = 1`：Select as if `shift` is pressed.
- `Pointer = 2`：Select single characters as if the user single clicked.
- `Word = 3`：Select whole words as if the user double clicked.
- `Line = 4`：Select whole lines as if the user triple clicked.
