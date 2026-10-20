<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CodeEdit

命名空间 `Godot`；class；继承 [`TextEdit`](TextEdit.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

CodeEdit is a specialized [`TextEdit`](TextEdit.md) designed for editing plain text code files. It has many features commonly found in code editors such as line numbers, line folding, code completion, indent management, and string/comment management.

**Note:** Regardless of locale, `CodeEdit` will by default always use left-to-right text direction to correctly display source code.

## 构造函数

### CodeEdit

```csharp
public CodeEdit()
```

## 属性

### AutoBraceCompletionEnabled

```csharp
public bool AutoBraceCompletionEnabled { get; set; }
```

引擎名：`auto_brace_completion_enabled`

If `true`, uses `AutoBraceCompletionPairs` to automatically insert the closing brace when the opening brace is inserted by typing or autocompletion. Also automatically removes the closing brace when using backspace on the opening brace.

### AutoBraceCompletionHighlightMatching

```csharp
public bool AutoBraceCompletionHighlightMatching { get; set; }
```

引擎名：`auto_brace_completion_highlight_matching`

If `true`, highlights brace pairs when the caret is on either one, using `AutoBraceCompletionPairs`. If matching, the pairs will be underlined. If a brace is unmatched, it is colored with `brace_mismatch_color`.

### AutoBraceCompletionPairs

```csharp
public Dictionary AutoBraceCompletionPairs { get; set; }
```

引擎名：`auto_brace_completion_pairs`

Sets the brace pairs to be autocompleted. For each entry in the dictionary, the key is the opening brace and the value is the closing brace that matches it. A brace is a `System.String` made of symbols. See `AutoBraceCompletionEnabled` and `AutoBraceCompletionHighlightMatching`.

### CodeCompletionEnabled

```csharp
public bool CodeCompletionEnabled { get; set; }
```

引擎名：`code_completion_enabled`

If `true`, the `ProjectSettings.input/ui_text_completion_query` action requests code completion. To handle it, see `_RequestCodeCompletion()` or `CodeCompletionRequested`.

### CodeCompletionPrefixes

```csharp
public Array<string> CodeCompletionPrefixes { get; set; }
```

引擎名：`code_completion_prefixes`

Sets prefixes that will trigger code completion.

### DelimiterComments

```csharp
public Array<string> DelimiterComments { get; set; }
```

引擎名：`delimiter_comments`

Sets the comment delimiters. All existing comment delimiters will be removed.

### DelimiterStrings

```csharp
public Array<string> DelimiterStrings { get; set; }
```

引擎名：`delimiter_strings`

Sets the string delimiters. All existing string delimiters will be removed.

### GuttersDrawBookmarks

```csharp
public bool GuttersDrawBookmarks { get; set; }
```

引擎名：`gutters_draw_bookmarks`

If `true`, bookmarks are drawn in the gutter. This gutter is shared with breakpoints and executing lines. See `SetLineAsBookmarked()`.

### GuttersDrawBreakpointsGutter

```csharp
public bool GuttersDrawBreakpointsGutter { get; set; }
```

引擎名：`gutters_draw_breakpoints_gutter`

If `true`, breakpoints are drawn in the gutter. This gutter is shared with bookmarks and executing lines. Clicking the gutter will toggle the breakpoint for the line, see `SetLineAsBreakpoint()`.

### GuttersDrawExecutingLines

```csharp
public bool GuttersDrawExecutingLines { get; set; }
```

引擎名：`gutters_draw_executing_lines`

If `true`, executing lines are marked in the gutter. This gutter is shared with breakpoints and bookmarks. See `SetLineAsExecuting()`.

### GuttersDrawFoldGutter

```csharp
public bool GuttersDrawFoldGutter { get; set; }
```

引擎名：`gutters_draw_fold_gutter`

If `true`, the fold gutter is drawn. In this gutter, the `can_fold_code_region` icon is drawn for each foldable line (see `CanFoldLine()`) and the `folded_code_region` icon is drawn for each folded line (see `IsLineFolded()`). These icons can be clicked to toggle the fold state, see `ToggleFoldableLine()`. `LineFolding` must be `true` to show icons.

### GuttersDrawLineNumbers

```csharp
public bool GuttersDrawLineNumbers { get; set; }
```

引擎名：`gutters_draw_line_numbers`

If `true`, the line number gutter is drawn. Line numbers start at `1` and are incremented for each line of text. Clicking and dragging in the line number gutter will select entire lines of text.

### GuttersLineNumbersMinDigits

```csharp
public int GuttersLineNumbersMinDigits { get; set; }
```

引擎名：`gutters_line_numbers_min_digits`

The minimum width in digits reserved for the line number gutter.

### GuttersZeroPadLineNumbers

```csharp
public bool GuttersZeroPadLineNumbers { get; set; }
```

引擎名：`gutters_zero_pad_line_numbers`

If `true`, line numbers drawn in the gutter are zero padded based on the total line count. Requires `GuttersDrawLineNumbers` to be set to `true`.

### IndentAutomatic

```csharp
public bool IndentAutomatic { get; set; }
```

引擎名：`indent_automatic`

If `true`, an extra indent is automatically inserted when a new line is added and a prefix in `IndentAutomaticPrefixes` is found. If a brace pair opening key is found, the matching closing brace will be moved to another new line (see `AutoBraceCompletionPairs`).

### IndentAutomaticPrefixes

```csharp
public Array<string> IndentAutomaticPrefixes { get; set; }
```

引擎名：`indent_automatic_prefixes`

Prefixes to trigger an automatic indent. Used when `IndentAutomatic` is set to `true`.

### IndentSize

```csharp
public int IndentSize { get; set; }
```

引擎名：`indent_size`

Size of the tabulation indent (one Tab press) in characters. If `IndentUseSpaces` is enabled the number of spaces to use.

### IndentUseSpaces

```csharp
public bool IndentUseSpaces { get; set; }
```

引擎名：`indent_use_spaces`

Use spaces instead of tabs for indentation.

### LineFolding

```csharp
public bool LineFolding { get; set; }
```

引擎名：`line_folding`

If `true`, lines can be folded. Otherwise, line folding methods like `FoldLine()` will not work and `CanFoldLine()` will always return `false`. See `GuttersDrawFoldGutter`.

### LineLengthGuidelines

```csharp
public Array<int> LineLengthGuidelines { get; set; }
```

引擎名：`line_length_guidelines`

Draws vertical lines at the provided columns. The first entry is considered a main hard guideline and is drawn more prominently.

### SymbolLookupOnClick

```csharp
public bool SymbolLookupOnClick { get; set; }
```

引擎名：`symbol_lookup_on_click`

Set when a validated word from `SymbolValidate` is clicked, the `SymbolLookup` should be emitted.

### SymbolTooltipOnHover

```csharp
public bool SymbolTooltipOnHover { get; set; }
```

引擎名：`symbol_tooltip_on_hover`

If `true`, the `SymbolHovered` signal is emitted when hovering over a word.

## 方法

### AddAutoBraceCompletionPair

```csharp
public void AddAutoBraceCompletionPair(string startKey, string endKey)
```

引擎名：`add_auto_brace_completion_pair`

Adds a brace pair.

Both the start and end keys must be symbols. Only the start key has to be unique.

### AddCodeCompletionOption

```csharp
public void AddCodeCompletionOption(CodeEdit.CodeCompletionKind type, string displayText, string insertText, Color? textColor = null, Resource icon = null, Variant value = default, int location = 1024)
```

引擎名：`add_code_completion_option`

Submits an item to the queue of potential candidates for the autocomplete menu. Call `UpdateCodeCompletionOptions()` to update the list.

`location` indicates location of the option relative to the location of the code completion query. See [`CodeEdit.CodeCompletionLocation`](CodeEdit.md) for how to set this value.

**Note:** This list will replace all current candidates.

参数：
- `textColor`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### AddCodeCompletionOption

```csharp
public void AddCodeCompletionOption(CodeEdit.CodeCompletionKind type, string displayText, string insertText, Color? textColor, Resource icon, Variant? value)
```

引擎名：`add_code_completion_option`

Submits an item to the queue of potential candidates for the autocomplete menu. Call `UpdateCodeCompletionOptions()` to update the list.

`location` indicates location of the option relative to the location of the code completion query. See [`CodeEdit.CodeCompletionLocation`](CodeEdit.md) for how to set this value.

**Note:** This list will replace all current candidates.

参数：
- `textColor`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### AddCodeCompletionOption

```csharp
public void AddCodeCompletionOption(CodeEdit.CodeCompletionKind type, string displayText, string insertText, Color? textColor, Resource icon, Variant? value, int location)
```

引擎名：`add_code_completion_option`

Submits an item to the queue of potential candidates for the autocomplete menu. Call `UpdateCodeCompletionOptions()` to update the list.

`location` indicates location of the option relative to the location of the code completion query. See [`CodeEdit.CodeCompletionLocation`](CodeEdit.md) for how to set this value.

**Note:** This list will replace all current candidates.

参数：
- `textColor`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.
- `value`：If the parameter is null, then the default value is `(Variant)(0)`.

### AddCommentDelimiter

```csharp
public void AddCommentDelimiter(string startKey, string endKey, bool lineOnly = false)
```

引擎名：`add_comment_delimiter`

Adds a comment delimiter from `startKey` to `endKey`. Both keys should be symbols, and `startKey` must not be shared with other delimiters.

If `lineOnly` is `true` or `endKey` is an empty `System.String`, the region does not carry over to the next line.

### AddStringDelimiter

```csharp
public void AddStringDelimiter(string startKey, string endKey, bool lineOnly = false)
```

引擎名：`add_string_delimiter`

Defines a string delimiter from `startKey` to `endKey`. Both keys should be symbols, and `startKey` must not be shared with other delimiters.

If `lineOnly` is `true` or `endKey` is an empty `System.String`, the region does not carry over to the next line.

### CanFoldLine

```csharp
public bool CanFoldLine(int line)
```

引擎名：`can_fold_line`

Returns `true` if the given line is foldable. A line is foldable if it is the start of a valid code region (see `GetCodeRegionStartTag()`), if it is the start of a comment or string block, or if the next non-empty line is more indented (see [`TextEdit.GetIndentLevel`](TextEdit.md)).

### CancelCodeCompletion

```csharp
public void CancelCodeCompletion()
```

引擎名：`cancel_code_completion`

Cancels the autocomplete menu.

### ClearBookmarkedLines

```csharp
public void ClearBookmarkedLines()
```

引擎名：`clear_bookmarked_lines`

Clears all bookmarked lines.

### ClearBreakpointedLines

```csharp
public void ClearBreakpointedLines()
```

引擎名：`clear_breakpointed_lines`

Clears all breakpointed lines.

### ClearCommentDelimiters

```csharp
public void ClearCommentDelimiters()
```

引擎名：`clear_comment_delimiters`

Removes all comment delimiters.

### ClearExecutingLines

```csharp
public void ClearExecutingLines()
```

引擎名：`clear_executing_lines`

Clears all executed lines.

### ClearStringDelimiters

```csharp
public void ClearStringDelimiters()
```

引擎名：`clear_string_delimiters`

Removes all string delimiters.

### ConfirmCodeCompletion

```csharp
public void ConfirmCodeCompletion(bool replace = false)
```

引擎名：`confirm_code_completion`

Inserts the selected entry into the text. If `replace` is `true`, any existing text is replaced rather than merged.

### ConvertIndent

```csharp
public void ConvertIndent(int fromLine = -1, int toLine = -1)
```

引擎名：`convert_indent`

Converts the indents of lines between `fromLine` and `toLine` to tabs or spaces as set by `IndentUseSpaces`.

Values of `-1` convert the entire text.

### CreateCodeRegion

```csharp
public void CreateCodeRegion()
```

引擎名：`create_code_region`

Creates a new code region with the selection. At least one single line comment delimiter have to be defined (see `AddCommentDelimiter()`).

A code region is a part of code that is highlighted when folded and can help organize your script.

Code region start and end tags can be customized (see `SetCodeRegionTags()`).

Code regions are delimited using start and end tags (respectively `region` and `endregion` by default) preceded by one line comment delimiter. (eg. `#region` and `#endregion`)

### DeleteLines

```csharp
public void DeleteLines()
```

引擎名：`delete_lines`

Deletes all lines that are selected or have a caret on them.

### DoIndent

```csharp
public void DoIndent()
```

引擎名：`do_indent`

If there is no selection, indentation is inserted at the caret. Otherwise, the selected lines are indented like `IndentLines()`. Equivalent to the `ProjectSettings.input/ui_text_indent` action. The indentation characters used depend on `IndentUseSpaces` and `IndentSize`.

### DuplicateLines

```csharp
public void DuplicateLines()
```

引擎名：`duplicate_lines`

Duplicates all lines currently selected with any caret. Duplicates the entire line beneath the current one no matter where the caret is within the line.

### DuplicateSelection

```csharp
public void DuplicateSelection()
```

引擎名：`duplicate_selection`

Duplicates all selected text and duplicates all lines with a caret on them.

### FoldAllLines

```csharp
public void FoldAllLines()
```

引擎名：`fold_all_lines`

Folds all lines that are possible to be folded (see `CanFoldLine()`).

### FoldLine

```csharp
public void FoldLine(int line)
```

引擎名：`fold_line`

Folds the given line, if possible (see `CanFoldLine()`).

### GetAutoBraceCompletionCloseKey

```csharp
public string GetAutoBraceCompletionCloseKey(string openKey)
```

引擎名：`get_auto_brace_completion_close_key`

Gets the matching auto brace close key for `openKey`.

### GetAutoBraceCompletionPairs

```csharp
public Dictionary GetAutoBraceCompletionPairs()
```

引擎名：`get_auto_brace_completion_pairs`

### GetAutoIndentPrefixes

```csharp
public Array<string> GetAutoIndentPrefixes()
```

引擎名：`get_auto_indent_prefixes`

### GetBookmarkedLines

```csharp
public int[] GetBookmarkedLines()
```

引擎名：`get_bookmarked_lines`

Gets all bookmarked lines.

### GetBreakpointedLines

```csharp
public int[] GetBreakpointedLines()
```

引擎名：`get_breakpointed_lines`

Gets all breakpointed lines.

### GetCodeCompletionOption

```csharp
public Dictionary GetCodeCompletionOption(int index)
```

引擎名：`get_code_completion_option`

Gets the completion option at `index`. The return [`Collections.Dictionary`](Collections.Dictionary.md) has the following key-values:

`kind`: [`CodeEdit.CodeCompletionKind`](CodeEdit.md)

`display_text`: Text that is shown on the autocomplete menu.

`insert_text`: Text that is to be inserted when this item is selected.

`font_color`: Color of the text on the autocomplete menu.

`icon`: Icon to draw on the autocomplete menu.

`default_value`: Value of the symbol.

### GetCodeCompletionOptions

```csharp
public Array<Dictionary> GetCodeCompletionOptions()
```

引擎名：`get_code_completion_options`

Gets all completion options, see `GetCodeCompletionOption()` for return content.

### GetCodeCompletionPrefixes

```csharp
public Array<string> GetCodeCompletionPrefixes()
```

引擎名：`get_code_completion_prefixes`

### GetCodeCompletionSelectedIndex

```csharp
public int GetCodeCompletionSelectedIndex()
```

引擎名：`get_code_completion_selected_index`

Gets the index of the current selected completion option.

### GetCodeRegionEndTag

```csharp
public string GetCodeRegionEndTag()
```

引擎名：`get_code_region_end_tag`

Returns the code region end tag (without comment delimiter).

### GetCodeRegionStartTag

```csharp
public string GetCodeRegionStartTag()
```

引擎名：`get_code_region_start_tag`

Returns the code region start tag (without comment delimiter).

### GetCommentDelimiters

```csharp
public Array<string> GetCommentDelimiters()
```

引擎名：`get_comment_delimiters`

### GetDelimiterEndKey

```csharp
public string GetDelimiterEndKey(int delimiterIndex)
```

引擎名：`get_delimiter_end_key`

Gets the end key for a string or comment region index.

### GetDelimiterEndPosition

```csharp
public Vector2 GetDelimiterEndPosition(int line, int column)
```

引擎名：`get_delimiter_end_position`

If `line` `column` is in a string or comment, returns the end position of the region. If not or no end could be found, both [`Vector2`](Vector2.md) values will be `-1`.

### GetDelimiterStartKey

```csharp
public string GetDelimiterStartKey(int delimiterIndex)
```

引擎名：`get_delimiter_start_key`

Gets the start key for a string or comment region index.

### GetDelimiterStartPosition

```csharp
public Vector2 GetDelimiterStartPosition(int line, int column)
```

引擎名：`get_delimiter_start_position`

If `line` `column` is in a string or comment, returns the start position of the region. If not or no start could be found, both [`Vector2`](Vector2.md) values will be `-1`.

### GetExecutingLines

```csharp
public int[] GetExecutingLines()
```

引擎名：`get_executing_lines`

Gets all executing lines.

### GetFoldedLines

```csharp
public Array<int> GetFoldedLines()
```

引擎名：`get_folded_lines`

Returns all lines that are currently folded.

### GetIndentSize

```csharp
public int GetIndentSize()
```

引擎名：`get_indent_size`

### GetLineLengthGuidelines

```csharp
public Array<int> GetLineLengthGuidelines()
```

引擎名：`get_line_length_guidelines`

### GetLineNumbersMinDigits

```csharp
public int GetLineNumbersMinDigits()
```

引擎名：`get_line_numbers_min_digits`

### GetStringDelimiters

```csharp
public Array<string> GetStringDelimiters()
```

引擎名：`get_string_delimiters`

### GetTextForCodeCompletion

```csharp
public string GetTextForCodeCompletion()
```

引擎名：`get_text_for_code_completion`

Returns the full text with char `0xFFFF` at the caret location.

### GetTextForSymbolLookup

```csharp
public string GetTextForSymbolLookup()
```

引擎名：`get_text_for_symbol_lookup`

Returns the full text with char `0xFFFF` at the cursor location.

### GetTextWithCursorChar

```csharp
public string GetTextWithCursorChar(int line, int column)
```

引擎名：`get_text_with_cursor_char`

Returns the full text with char `0xFFFF` at the specified location.

### HasAutoBraceCompletionCloseKey

```csharp
public bool HasAutoBraceCompletionCloseKey(string closeKey)
```

引擎名：`has_auto_brace_completion_close_key`

Returns `true` if close key `closeKey` exists.

### HasAutoBraceCompletionOpenKey

```csharp
public bool HasAutoBraceCompletionOpenKey(string openKey)
```

引擎名：`has_auto_brace_completion_open_key`

Returns `true` if open key `openKey` exists.

### HasCommentDelimiter

```csharp
public bool HasCommentDelimiter(string startKey)
```

引擎名：`has_comment_delimiter`

Returns `true` if comment `startKey` exists.

### HasStringDelimiter

```csharp
public bool HasStringDelimiter(string startKey)
```

引擎名：`has_string_delimiter`

Returns `true` if string `startKey` exists.

### IndentLines

```csharp
public void IndentLines()
```

引擎名：`indent_lines`

Indents all lines that are selected or have a caret on them. Uses spaces or a tab depending on `IndentUseSpaces`. See `UnindentLines()`.

### IsAutoBraceCompletionEnabled

```csharp
public bool IsAutoBraceCompletionEnabled()
```

引擎名：`is_auto_brace_completion_enabled`

### IsAutoIndentEnabled

```csharp
public bool IsAutoIndentEnabled()
```

引擎名：`is_auto_indent_enabled`

### IsCodeCompletionEnabled

```csharp
public bool IsCodeCompletionEnabled()
```

引擎名：`is_code_completion_enabled`

### IsDrawLineNumbersEnabled

```csharp
public bool IsDrawLineNumbersEnabled()
```

引擎名：`is_draw_line_numbers_enabled`

### IsDrawingBookmarksGutter

```csharp
public bool IsDrawingBookmarksGutter()
```

引擎名：`is_drawing_bookmarks_gutter`

### IsDrawingBreakpointsGutter

```csharp
public bool IsDrawingBreakpointsGutter()
```

引擎名：`is_drawing_breakpoints_gutter`

### IsDrawingExecutingLinesGutter

```csharp
public bool IsDrawingExecutingLinesGutter()
```

引擎名：`is_drawing_executing_lines_gutter`

### IsDrawingFoldGutter

```csharp
public bool IsDrawingFoldGutter()
```

引擎名：`is_drawing_fold_gutter`

### IsHighlightMatchingBracesEnabled

```csharp
public bool IsHighlightMatchingBracesEnabled()
```

引擎名：`is_highlight_matching_braces_enabled`

### IsInComment

```csharp
public int IsInComment(int line, int column = -1)
```

引擎名：`is_in_comment`

Returns delimiter index if `line` `column` is in a comment. If `column` is not provided, will return delimiter index if the entire `line` is a comment. Otherwise `-1`.

### IsInString

```csharp
public int IsInString(int line, int column = -1)
```

引擎名：`is_in_string`

Returns the delimiter index if `line` `column` is in a string. If `column` is not provided, will return the delimiter index if the entire `line` is a string. Otherwise `-1`.

### IsIndentUsingSpaces

```csharp
public bool IsIndentUsingSpaces()
```

引擎名：`is_indent_using_spaces`

### IsLineBookmarked

```csharp
public bool IsLineBookmarked(int line)
```

引擎名：`is_line_bookmarked`

Returns `true` if the given line is bookmarked. See `SetLineAsBookmarked()`.

### IsLineBreakpointed

```csharp
public bool IsLineBreakpointed(int line)
```

引擎名：`is_line_breakpointed`

Returns `true` if the given line is breakpointed. See `SetLineAsBreakpoint()`.

### IsLineCodeRegionEnd

```csharp
public bool IsLineCodeRegionEnd(int line)
```

引擎名：`is_line_code_region_end`

Returns `true` if the given line is a code region end. See `SetCodeRegionTags()`.

### IsLineCodeRegionStart

```csharp
public bool IsLineCodeRegionStart(int line)
```

引擎名：`is_line_code_region_start`

Returns `true` if the given line is a code region start. See `SetCodeRegionTags()`.

### IsLineExecuting

```csharp
public bool IsLineExecuting(int line)
```

引擎名：`is_line_executing`

Returns `true` if the given line is marked as executing. See `SetLineAsExecuting()`.

### IsLineFolded

```csharp
public bool IsLineFolded(int line)
```

引擎名：`is_line_folded`

Returns `true` if the given line is folded. See `FoldLine()`.

### IsLineFoldingEnabled

```csharp
public bool IsLineFoldingEnabled()
```

引擎名：`is_line_folding_enabled`

### IsLineNumbersZeroPadded

```csharp
public bool IsLineNumbersZeroPadded()
```

引擎名：`is_line_numbers_zero_padded`

### IsSymbolLookupOnClickEnabled

```csharp
public bool IsSymbolLookupOnClickEnabled()
```

引擎名：`is_symbol_lookup_on_click_enabled`

### IsSymbolTooltipOnHoverEnabled

```csharp
public bool IsSymbolTooltipOnHoverEnabled()
```

引擎名：`is_symbol_tooltip_on_hover_enabled`

### JoinLines

```csharp
public void JoinLines(string lineEnding = " ")
```

引擎名：`join_lines`

Joins all selected lines or lines containing a caret with their next line. Whitespace in between will be removed. If the next line has content, the `lineEnding` will be inserted in between.

### MoveLinesDown

```csharp
public void MoveLinesDown()
```

引擎名：`move_lines_down`

Moves all lines down that are selected or have a caret on them.

### MoveLinesUp

```csharp
public void MoveLinesUp()
```

引擎名：`move_lines_up`

Moves all lines up that are selected or have a caret on them.

### RemoveCommentDelimiter

```csharp
public void RemoveCommentDelimiter(string startKey)
```

引擎名：`remove_comment_delimiter`

Removes the comment delimiter with `startKey`.

### RemoveStringDelimiter

```csharp
public void RemoveStringDelimiter(string startKey)
```

引擎名：`remove_string_delimiter`

Removes the string delimiter with `startKey`.

### RequestCodeCompletion

```csharp
public void RequestCodeCompletion(bool force = false)
```

引擎名：`request_code_completion`

Emits `CodeCompletionRequested`, if `force` is `true` will bypass all checks. Otherwise will check that the caret is in a word or in front of a prefix. Will ignore the request if all current options are of type file path, node path, or signal.

### SetAutoBraceCompletionEnabled

```csharp
public void SetAutoBraceCompletionEnabled(bool enable)
```

引擎名：`set_auto_brace_completion_enabled`

### SetAutoBraceCompletionPairs

```csharp
public void SetAutoBraceCompletionPairs(Dictionary pairs)
```

引擎名：`set_auto_brace_completion_pairs`

### SetAutoIndentEnabled

```csharp
public void SetAutoIndentEnabled(bool enable)
```

引擎名：`set_auto_indent_enabled`

### SetAutoIndentPrefixes

```csharp
public void SetAutoIndentPrefixes(Array<string> prefixes)
```

引擎名：`set_auto_indent_prefixes`

### SetCodeCompletionEnabled

```csharp
public void SetCodeCompletionEnabled(bool enable)
```

引擎名：`set_code_completion_enabled`

### SetCodeCompletionPrefixes

```csharp
public void SetCodeCompletionPrefixes(Array<string> prefixes)
```

引擎名：`set_code_completion_prefixes`

### SetCodeCompletionSelectedIndex

```csharp
public void SetCodeCompletionSelectedIndex(int index)
```

引擎名：`set_code_completion_selected_index`

Sets the current selected completion option.

### SetCodeHint

```csharp
public void SetCodeHint(string codeHint)
```

引擎名：`set_code_hint`

Sets the code hint text. Pass an empty string to clear.

### SetCodeHintDrawBelow

```csharp
public void SetCodeHintDrawBelow(bool drawBelow)
```

引擎名：`set_code_hint_draw_below`

If `true`, the code hint will draw below the main caret. If `false`, the code hint will draw above the main caret. See `SetCodeHint()`.

### SetCodeRegionTags

```csharp
public void SetCodeRegionTags(string start = "region", string end = "endregion")
```

引擎名：`set_code_region_tags`

Sets the code region start and end tags (without comment delimiter).

### SetCommentDelimiters

```csharp
public void SetCommentDelimiters(Array<string> commentDelimiters)
```

引擎名：`set_comment_delimiters`

### SetDrawBookmarksGutter

```csharp
public void SetDrawBookmarksGutter(bool enable)
```

引擎名：`set_draw_bookmarks_gutter`

### SetDrawBreakpointsGutter

```csharp
public void SetDrawBreakpointsGutter(bool enable)
```

引擎名：`set_draw_breakpoints_gutter`

### SetDrawExecutingLinesGutter

```csharp
public void SetDrawExecutingLinesGutter(bool enable)
```

引擎名：`set_draw_executing_lines_gutter`

### SetDrawFoldGutter

```csharp
public void SetDrawFoldGutter(bool enable)
```

引擎名：`set_draw_fold_gutter`

### SetDrawLineNumbers

```csharp
public void SetDrawLineNumbers(bool enable)
```

引擎名：`set_draw_line_numbers`

### SetHighlightMatchingBracesEnabled

```csharp
public void SetHighlightMatchingBracesEnabled(bool enable)
```

引擎名：`set_highlight_matching_braces_enabled`

### SetIndentSize

```csharp
public void SetIndentSize(int size)
```

引擎名：`set_indent_size`

### SetIndentUsingSpaces

```csharp
public void SetIndentUsingSpaces(bool useSpaces)
```

引擎名：`set_indent_using_spaces`

### SetLineAsBookmarked

```csharp
public void SetLineAsBookmarked(int line, bool bookmarked)
```

引擎名：`set_line_as_bookmarked`

Sets the given line as bookmarked. If `true` and `GuttersDrawBookmarks` is `true`, draws the `bookmark` icon in the gutter for this line. See `GetBookmarkedLines()` and `IsLineBookmarked()`.

### SetLineAsBreakpoint

```csharp
public void SetLineAsBreakpoint(int line, bool breakpointed)
```

引擎名：`set_line_as_breakpoint`

Sets the given line as a breakpoint. If `true` and `GuttersDrawBreakpointsGutter` is `true`, draws the `breakpoint` icon in the gutter for this line. See `GetBreakpointedLines()` and `IsLineBreakpointed()`.

### SetLineAsExecuting

```csharp
public void SetLineAsExecuting(int line, bool executing)
```

引擎名：`set_line_as_executing`

Sets the given line as executing. If `true` and `GuttersDrawExecutingLines` is `true`, draws the `executing_line` icon in the gutter for this line. See `GetExecutingLines()` and `IsLineExecuting()`.

### SetLineFoldingEnabled

```csharp
public void SetLineFoldingEnabled(bool enabled)
```

引擎名：`set_line_folding_enabled`

### SetLineLengthGuidelines

```csharp
public void SetLineLengthGuidelines(Array<int> guidelineColumns)
```

引擎名：`set_line_length_guidelines`

### SetLineNumbersMinDigits

```csharp
public void SetLineNumbersMinDigits(int count)
```

引擎名：`set_line_numbers_min_digits`

### SetLineNumbersZeroPadded

```csharp
public void SetLineNumbersZeroPadded(bool enable)
```

引擎名：`set_line_numbers_zero_padded`

### SetStringDelimiters

```csharp
public void SetStringDelimiters(Array<string> stringDelimiters)
```

引擎名：`set_string_delimiters`

### SetSymbolLookupOnClickEnabled

```csharp
public void SetSymbolLookupOnClickEnabled(bool enable)
```

引擎名：`set_symbol_lookup_on_click_enabled`

### SetSymbolLookupWordAsValid

```csharp
public void SetSymbolLookupWordAsValid(bool valid)
```

引擎名：`set_symbol_lookup_word_as_valid`

Sets the symbol emitted by `SymbolValidate` as a valid lookup.

### SetSymbolTooltipOnHoverEnabled

```csharp
public void SetSymbolTooltipOnHoverEnabled(bool enable)
```

引擎名：`set_symbol_tooltip_on_hover_enabled`

### ToggleFoldableLine

```csharp
public void ToggleFoldableLine(int line)
```

引擎名：`toggle_foldable_line`

Toggle the folding of the code block at the given line.

### ToggleFoldableLinesAtCarets

```csharp
public void ToggleFoldableLinesAtCarets()
```

引擎名：`toggle_foldable_lines_at_carets`

Toggle the folding of the code block on all lines with a caret on them.

### UnfoldAllLines

```csharp
public void UnfoldAllLines()
```

引擎名：`unfold_all_lines`

Unfolds all lines that are folded.

### UnfoldLine

```csharp
public void UnfoldLine(int line)
```

引擎名：`unfold_line`

Unfolds the given line if it is folded or if it is hidden under a folded line.

### UnindentLines

```csharp
public void UnindentLines()
```

引擎名：`unindent_lines`

Unindents all lines that are selected or have a caret on them. Uses spaces or a tab depending on `IndentUseSpaces`. Equivalent to the `ProjectSettings.input/ui_text_dedent` action. See `IndentLines()`.

### UpdateCodeCompletionOptions

```csharp
public void UpdateCodeCompletionOptions(bool force)
```

引擎名：`update_code_completion_options`

Submits all completion options added with `AddCodeCompletionOption()`. Will try to force the autocomplete menu to popup, if `force` is `true`.

**Note:** This will replace all current candidates.

### _ConfirmCodeCompletion

```csharp
public virtual void _ConfirmCodeCompletion(bool replace)
```

引擎名：`_confirm_code_completion`

Override this method to define how the selected entry should be inserted. If `replace` is `true`, any existing text should be replaced.

### _FilterCodeCompletionCandidates

```csharp
public virtual Array<Dictionary> _FilterCodeCompletionCandidates(Array<Dictionary> candidates)
```

引擎名：`_filter_code_completion_candidates`

Override this method to define what items in `candidates` should be displayed.

Both `candidates` and the return is an [`Collections.Array`](Collections.Array.md) of [`Collections.Dictionary`](Collections.Dictionary.md), see `GetCodeCompletionOption()` for [`Collections.Dictionary`](Collections.Dictionary.md) content.

### _RequestCodeCompletion

```csharp
public virtual void _RequestCodeCompletion(bool force)
```

引擎名：`_request_code_completion`

Override this method to define what happens when the user requests code completion. If `force` is `true`, any checks should be bypassed.

## 事件（信号）

### BreakpointToggled

```csharp
public event CodeEdit.BreakpointToggledEventHandler BreakpointToggled
```

引擎名：`breakpoint_toggled`

Emitted when a breakpoint is added or removed from a line. If the line is removed via backspace, a signal is emitted at the old line.

### CodeCompletionRequested

```csharp
public event Action CodeCompletionRequested
```

引擎名：`code_completion_requested`

Emitted when the user requests code completion. This signal will not be sent if `_RequestCodeCompletion()` is overridden or `CodeCompletionEnabled` is `false`.

### SymbolHovered

```csharp
public event CodeEdit.SymbolHoveredEventHandler SymbolHovered
```

引擎名：`symbol_hovered`

Emitted when the user hovers over a symbol. Unlike [`Control.MouseEntered`](Control.md), this signal is not emitted immediately, but when the cursor is over the symbol for `ProjectSettings.gui/timers/tooltip_delay_sec` seconds.

**Note:** `SymbolTooltipOnHover` must be `true` for this signal to be emitted.

### SymbolLookup

```csharp
public event CodeEdit.SymbolLookupEventHandler SymbolLookup
```

引擎名：`symbol_lookup`

Emitted when the user has clicked on a valid symbol.

### SymbolValidate

```csharp
public event CodeEdit.SymbolValidateEventHandler SymbolValidate
```

引擎名：`symbol_validate`

Emitted when the user hovers over a symbol. The symbol should be validated and responded to, by calling `SetSymbolLookupWordAsValid()`.

**Note:** `SymbolLookupOnClick` must be `true` for this signal to be emitted.

## BreakpointToggledEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`CodeEdit.BreakpointToggled`](CodeEdit.md) event of a [`CodeEdit`](CodeEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void BreakpointToggledEventHandler(long line)
```

## CodeCompletionKind

命名空间 `Godot`；enum

### 枚举值

- `Class = 0`：Marks the option as a class.
- `Function = 1`：Marks the option as a function.
- `Signal = 2`：Marks the option as a Godot signal.
- `Variable = 3`：Marks the option as a variable.
- `Member = 4`：Marks the option as a member.
- `Enum = 5`：Marks the option as an enum entry.
- `Constant = 6`：Marks the option as a constant.
- `NodePath = 7`：Marks the option as a Godot node path.
- `FilePath = 8`：Marks the option as a file path.
- `PlainText = 9`：Marks the option as unclassified or plain text.
- `Keyword = 10`：Marks the option as a keyword.

## CodeCompletionLocation

命名空间 `Godot`；enum

### 枚举值

- `Local = 0`：The option is local to the location of the code completion query - e.g.
- `ParentMask = 256`：The option is from the containing class or a parent class, relative to the location of the code completion query.
- `OtherUserCode = 512`：The option is from user code which is not local and not in a derived class (e.g.
- `Other = 1024`：The option is from other engine code, not covered by the other enum constants - e.g.

## SymbolHoveredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`CodeEdit.SymbolHovered`](CodeEdit.md) event of a [`CodeEdit`](CodeEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void SymbolHoveredEventHandler(string symbol, long line, long column)
```

## SymbolLookupEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`CodeEdit.SymbolLookup`](CodeEdit.md) event of a [`CodeEdit`](CodeEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void SymbolLookupEventHandler(string symbol, long line, long column)
```

## SymbolValidateEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`CodeEdit.SymbolValidate`](CodeEdit.md) event of a [`CodeEdit`](CodeEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void SymbolValidateEventHandler(string symbol)
```
