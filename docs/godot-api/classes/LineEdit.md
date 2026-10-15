<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# LineEdit

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`LineEdit` provides an input field for editing a single line of text.

- When the `LineEdit` control is focused using the keyboard arrow keys, it will only gain focus and not enter edit mode.

- To enter edit mode, click on the control with the mouse, see also `KeepEditingOnTextSubmit`.

- To exit edit mode, press `ui_text_submit` or `ui_cancel` (by default Escape) actions.

- Check `Edit()`, `Unedit()`, `IsEditing()`, and `EditingToggled` for more information.

While entering text, it is possible to insert special characters using Unicode, OEM or Windows alt codes:

- To enter Unicode codepoints, hold Alt and type the codepoint on the numpad. For example, to enter the character `á` (U+00E1), hold Alt and type +E1 on the numpad (the leading zeroes can be omitted).

- To enter OEM codepoints, hold Alt and type the code on the numpad. For example, to enter the character `á` (OEM 160), hold Alt and type `160` on the numpad.

- To enter Windows codepoints, hold Alt and type the code on the numpad. For example, to enter the character `á` (Windows 0225), hold Alt and type 0, 2, 2, 5 on the numpad. The leading zero here must **not** be omitted, as this is how Windows codepoints are distinguished from OEM codepoints.

**Important:**

- Focusing the `LineEdit` with `ui_focus_next` (by default Tab) or `ui_focus_prev` (by default Shift + Tab) or [`Control.GrabFocus`](Control.md) still enters edit mode (for compatibility).

`LineEdit` features many built-in shortcuts that are always available (Ctrl here maps to Cmd on macOS):

- Ctrl + C: Copy

- Ctrl + X: Cut

- Ctrl + V or Ctrl + Y: Paste/"yank"

- Ctrl + Z: Undo

- Ctrl + ~: Swap input direction.

- Ctrl + Shift + Z: Redo

- Ctrl + U: Delete text from the caret position to the beginning of the line

- Ctrl + K: Delete text from the caret position to the end of the line

- Ctrl + A: Select all text

- Up Arrow/Down Arrow: Move the caret to the beginning/end of the line

On macOS, some extra keyboard shortcuts are available:

- Cmd + F: Same as Right Arrow, move the caret one character right

- Cmd + B: Same as Left Arrow, move the caret one character left

- Cmd + P: Same as Up Arrow, move the caret to the previous line

- Cmd + N: Same as Down Arrow, move the caret to the next line

- Cmd + D: Same as Delete, delete the character on the right side of caret

- Cmd + H: Same as Backspace, delete the character on the left side of the caret

- Cmd + A: Same as Home, move the caret to the beginning of the line

- Cmd + E: Same as End, move the caret to the end of the line

- Cmd + Left Arrow: Same as Home, move the caret to the beginning of the line

- Cmd + Right Arrow: Same as End, move the caret to the end of the line

**Note:** Caret movement shortcuts listed above are not affected by `ShortcutKeysEnabled`.

## 构造函数

### LineEdit

```csharp
public LineEdit()
```

## 属性

### Alignment

```csharp
public HorizontalAlignment Alignment { get; set; }
```

引擎名：`alignment`

The text's horizontal alignment.

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

### CaretColumn

```csharp
public int CaretColumn { get; set; }
```

引擎名：`caret_column`

The caret's column position inside the `LineEdit`. When set, the text may scroll to accommodate it.

### CaretForceDisplayed

```csharp
public bool CaretForceDisplayed { get; set; }
```

引擎名：`caret_force_displayed`

If `true`, the `LineEdit` will always show the caret, even if not editing or focus is lost.

### CaretMidGrapheme

```csharp
public bool CaretMidGrapheme { get; set; }
```

引擎名：`caret_mid_grapheme`

Allow moving caret, selecting and removing the individual composite character components.

**Note:** Backspace is always removing individual composite character components.

### ClearButtonEnabled

```csharp
public bool ClearButtonEnabled { get; set; }
```

引擎名：`clear_button_enabled`

If `true`, the `LineEdit` will show a clear button if `Text` is not empty, which can be used to clear the text quickly.

### ContextMenuEnabled

```csharp
public bool ContextMenuEnabled { get; set; }
```

引擎名：`context_menu_enabled`

If `true`, the context menu will appear when right-clicked.

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

### DrawControlChars

```csharp
public bool DrawControlChars { get; set; }
```

引擎名：`draw_control_chars`

If `true`, control characters are displayed.

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

### ExpandToTextLength

```csharp
public bool ExpandToTextLength { get; set; }
```

引擎名：`expand_to_text_length`

If `true`, the `LineEdit` width will increase to stay longer than the `Text`. It will **not** compress if the `Text` is shortened.

### Flat

```csharp
public bool Flat { get; set; }
```

引擎名：`flat`

If `true`, the `LineEdit` doesn't display decoration.

### IconExpandMode

```csharp
public LineEdit.ExpandMode IconExpandMode { get; set; }
```

引擎名：`icon_expand_mode`

Define the scaling behavior of the `RightIcon`.

### KeepEditingOnTextSubmit

```csharp
public bool KeepEditingOnTextSubmit { get; set; }
```

引擎名：`keep_editing_on_text_submit`

If `true`, the `LineEdit` will not exit edit mode when text is submitted by pressing `ui_text_submit` action (by default: Enter or Kp Enter).

### Language

```csharp
public string Language { get; set; }
```

引擎名：`language`

Language code used for line-breaking and text shaping algorithms. If left empty, the current locale is used instead.

### MaxLength

```csharp
public int MaxLength { get; set; }
```

引擎名：`max_length`

Maximum number of characters that can be entered inside the `LineEdit`. If `0`, there is no limit.

When a limit is defined, characters that would exceed `MaxLength` are truncated. This happens both for existing `Text` contents when setting the max length, or for new text inserted in the `LineEdit`, including pasting.

If any input text is truncated, the `TextChangeRejected` signal is emitted with the truncated substring as a parameter:

```csharp
Text = "Hello world";
MaxLength = 5;
// `Text` becomes "Hello".
MaxLength = 10;
Text += " goodbye";
// `Text` becomes "Hello good".
// `text_change_rejected` is emitted with "bye" as a parameter.

```

### MiddleMousePasteEnabled

```csharp
public bool MiddleMousePasteEnabled { get; set; }
```

引擎名：`middle_mouse_paste_enabled`

If `false`, using middle mouse button to paste clipboard will be disabled.

**Note:** This method is only implemented on Linux.

### PlaceholderText

```csharp
public string PlaceholderText { get; set; }
```

引擎名：`placeholder_text`

Text shown when the `LineEdit` is empty. It is **not** the `LineEdit`'s default value (see `Text`).

### RightIcon

```csharp
public Texture2D RightIcon { get; set; }
```

引擎名：`right_icon`

Sets the icon that will appear in the right end of the `LineEdit` if there's no `Text`, or always, if `ClearButtonEnabled` is set to `false`.

### RightIconScale

```csharp
public float RightIconScale { get; set; }
```

引擎名：`right_icon_scale`

Scale ratio of the icon when `IconExpandMode` is set to [`LineEdit.ExpandMode.FitToLineEdit`](LineEdit.md).

### Secret

```csharp
public bool Secret { get; set; }
```

引擎名：`secret`

If `true`, every character is replaced with the secret character (see `SecretCharacter`).

### SecretCharacter

```csharp
public string SecretCharacter { get; set; }
```

引擎名：`secret_character`

The character to use to mask secret input. Only a single character can be used as the secret character. If it is longer than one character, only the first one will be used. If it is empty, a space will be used instead.

### SelectAllOnFocus

```csharp
public bool SelectAllOnFocus { get; set; }
```

引擎名：`select_all_on_focus`

If `true`, the `LineEdit` will select the whole text when it gains focus.

### SelectingEnabled

```csharp
public bool SelectingEnabled { get; set; }
```

引擎名：`selecting_enabled`

If `false`, it's impossible to select the text using mouse nor keyboard.

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

### Text

```csharp
public string Text { get; set; }
```

引擎名：`text`

String value of the `LineEdit`.

**Note:** Changing text using this property won't emit the `TextChanged` signal.

### TextDirection

```csharp
public Control.TextDirection TextDirection { get; set; }
```

引擎名：`text_direction`

Base text writing direction.

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

### VirtualKeyboardType

```csharp
public LineEdit.VirtualKeyboardTypeEnum VirtualKeyboardType { get; set; }
```

引擎名：`virtual_keyboard_type`

Specifies the type of virtual keyboard to show.

## 方法

### ApplyIme

```csharp
public void ApplyIme()
```

引擎名：`apply_ime`

Applies text from the [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) (IME) and closes the IME if it is open.

### CancelIme

```csharp
public void CancelIme()
```

引擎名：`cancel_ime`

Closes the [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) (IME) if it is open. Any text in the IME will be lost.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Erases the `LineEdit`'s `Text`.

### DeleteCharAtCaret

```csharp
public void DeleteCharAtCaret()
```

引擎名：`delete_char_at_caret`

Deletes one character at the caret's current position (equivalent to pressing Delete).

### DeleteText

```csharp
public void DeleteText(int fromColumn, int toColumn)
```

引擎名：`delete_text`

Deletes a section of the `Text` going from position `fromColumn` to `toColumn`. Both parameters should be within the text's length.

### Deselect

```csharp
public void Deselect()
```

引擎名：`deselect`

Clears the current selection.

### Edit

```csharp
public void Edit()
```

引擎名：`edit`

Allows entering edit mode whether the `LineEdit` is focused or not. If `hideFocus` is `true`, the focused state will not be shown (see [`Control.GrabFocus`](Control.md)).

See also `KeepEditingOnTextSubmit`.

### Edit

```csharp
public void Edit(bool hideFocus = false)
```

引擎名：`edit`

Allows entering edit mode whether the `LineEdit` is focused or not. If `hideFocus` is `true`, the focused state will not be shown (see [`Control.GrabFocus`](Control.md)).

See also `KeepEditingOnTextSubmit`.

### GetCaretBlinkInterval

```csharp
public float GetCaretBlinkInterval()
```

引擎名：`get_caret_blink_interval`

### GetCaretColumn

```csharp
public int GetCaretColumn()
```

引擎名：`get_caret_column`

### GetDrawControlChars

```csharp
public bool GetDrawControlChars()
```

引擎名：`get_draw_control_chars`

### GetHorizontalAlignment

```csharp
public HorizontalAlignment GetHorizontalAlignment()
```

引擎名：`get_horizontal_alignment`

### GetIconExpandMode

```csharp
public LineEdit.ExpandMode GetIconExpandMode()
```

引擎名：`get_icon_expand_mode`

### GetLanguage

```csharp
public string GetLanguage()
```

引擎名：`get_language`

### GetMaxLength

```csharp
public int GetMaxLength()
```

引擎名：`get_max_length`

### GetMenu

```csharp
public PopupMenu GetMenu()
```

引擎名：`get_menu`

Returns the [`PopupMenu`](PopupMenu.md) of this `LineEdit`. By default, this menu is displayed when right-clicking on the `LineEdit`.

You can add custom menu items or remove standard ones. Make sure your IDs don't conflict with the standard ones (see [`LineEdit.MenuItems`](LineEdit.md)). For example:

```csharp
public override void _Ready()
{
var menu = GetMenu();
// Remove all items after "Redo".
menu.ItemCount = menu.GetItemIndex(LineEdit.MenuItems.Redo) + 1;
// Add custom items.
menu.AddSeparator();
menu.AddItem("Insert Date", LineEdit.MenuItems.Max + 1);
// Add event handler.
menu.IdPressed += OnItemPressed;
}

public void OnItemPressed(int id)
{
if (id == LineEdit.MenuItems.Max + 1)
{
InsertTextAtCaret(Time.GetDateStringFromSystem());
}
}

```

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`Window.Visible`](Window.md) property.

### GetNextCompositeCharacterColumn

```csharp
public int GetNextCompositeCharacterColumn(int column)
```

引擎名：`get_next_composite_character_column`

Returns the correct column at the end of a composite character like ❤️‍🩹 (mending heart; Unicode: `U+2764 U+FE0F U+200D U+1FA79`) which is comprised of more than one Unicode code point, if the caret is at the start of the composite character. Also returns the correct column with the caret at mid grapheme and for non-composite characters.

**Note:** To check at caret location use `get_next_composite_character_column(get_caret_column())`

### GetPlaceholder

```csharp
public string GetPlaceholder()
```

引擎名：`get_placeholder`

### GetPreviousCompositeCharacterColumn

```csharp
public int GetPreviousCompositeCharacterColumn(int column)
```

引擎名：`get_previous_composite_character_column`

Returns the correct column at the start of a composite character like ❤️‍🩹 (mending heart; Unicode: `U+2764 U+FE0F U+200D U+1FA79`) which is comprised of more than one Unicode code point, if the caret is at the end of the composite character. Also returns the correct column with the caret at mid grapheme and for non-composite characters.

**Note:** To check at caret location use `get_previous_composite_character_column(get_caret_column())`

### GetRightIcon

```csharp
public Texture2D GetRightIcon()
```

引擎名：`get_right_icon`

### GetRightIconScale

```csharp
public float GetRightIconScale()
```

引擎名：`get_right_icon_scale`

### GetScrollOffset

```csharp
public float GetScrollOffset()
```

引擎名：`get_scroll_offset`

Returns the scroll offset due to `CaretColumn`, as a number of characters.

### GetSecretCharacter

```csharp
public string GetSecretCharacter()
```

引擎名：`get_secret_character`

### GetSelectedText

```csharp
public string GetSelectedText()
```

引擎名：`get_selected_text`

Returns the text inside the selection.

### GetSelectionFromColumn

```csharp
public int GetSelectionFromColumn()
```

引擎名：`get_selection_from_column`

Returns the selection begin column.

### GetSelectionToColumn

```csharp
public int GetSelectionToColumn()
```

引擎名：`get_selection_to_column`

Returns the selection end column.

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
public Control.TextDirection GetTextDirection()
```

引擎名：`get_text_direction`

### GetVirtualKeyboardShowOnFocus

```csharp
public bool GetVirtualKeyboardShowOnFocus()
```

引擎名：`get_virtual_keyboard_show_on_focus`

### GetVirtualKeyboardType

```csharp
public LineEdit.VirtualKeyboardTypeEnum GetVirtualKeyboardType()
```

引擎名：`get_virtual_keyboard_type`

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
public bool HasSelection()
```

引擎名：`has_selection`

Returns `true` if the user has selected text.

### HasUndo

```csharp
public bool HasUndo()
```

引擎名：`has_undo`

Returns `true` if an "undo" action is available.

### InsertTextAtCaret

```csharp
public void InsertTextAtCaret(string text)
```

引擎名：`insert_text_at_caret`

Inserts `text` at the caret. If the resulting value is longer than `MaxLength`, nothing happens.

### IsBackspaceDeletesCompositeCharacterEnabled

```csharp
public bool IsBackspaceDeletesCompositeCharacterEnabled()
```

引擎名：`is_backspace_deletes_composite_character_enabled`

### IsCaretBlinkEnabled

```csharp
public bool IsCaretBlinkEnabled()
```

引擎名：`is_caret_blink_enabled`

### IsCaretForceDisplayed

```csharp
public bool IsCaretForceDisplayed()
```

引擎名：`is_caret_force_displayed`

### IsCaretMidGraphemeEnabled

```csharp
public bool IsCaretMidGraphemeEnabled()
```

引擎名：`is_caret_mid_grapheme_enabled`

### IsClearButtonEnabled

```csharp
public bool IsClearButtonEnabled()
```

引擎名：`is_clear_button_enabled`

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

### IsEditable

```csharp
public bool IsEditable()
```

引擎名：`is_editable`

### IsEditing

```csharp
public bool IsEditing()
```

引擎名：`is_editing`

Returns whether the `LineEdit` is being edited.

### IsEditingKeptOnTextSubmit

```csharp
public bool IsEditingKeptOnTextSubmit()
```

引擎名：`is_editing_kept_on_text_submit`

### IsEmojiMenuEnabled

```csharp
public bool IsEmojiMenuEnabled()
```

引擎名：`is_emoji_menu_enabled`

### IsExpandToTextLengthEnabled

```csharp
public bool IsExpandToTextLengthEnabled()
```

引擎名：`is_expand_to_text_length_enabled`

### IsFlat

```csharp
public bool IsFlat()
```

引擎名：`is_flat`

### IsMenuVisible

```csharp
public bool IsMenuVisible()
```

引擎名：`is_menu_visible`

Returns whether the menu is visible. Use this instead of `get_menu().visible` to improve performance (so the creation of the menu is avoided).

### IsMiddleMousePasteEnabled

```csharp
public bool IsMiddleMousePasteEnabled()
```

引擎名：`is_middle_mouse_paste_enabled`

### IsSecret

```csharp
public bool IsSecret()
```

引擎名：`is_secret`

### IsSelectAllOnFocus

```csharp
public bool IsSelectAllOnFocus()
```

引擎名：`is_select_all_on_focus`

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

Executes a given action as defined in the [`LineEdit.MenuItems`](LineEdit.md) enum.

### Select

```csharp
public void Select(int from = 0, int to = -1)
```

引擎名：`select`

Selects characters inside `LineEdit` between `from` and `to`. By default, `from` is at the beginning and `to` at the end.

```csharp
Text = "Welcome";
Select(); // Will select "Welcome".
Select(4); // Will select "ome".
Select(2, 5); // Will select "lco".

```

### SelectAll

```csharp
public void SelectAll()
```

引擎名：`select_all`

Selects the whole `System.String`.

### SetBackspaceDeletesCompositeCharacterEnabled

```csharp
public void SetBackspaceDeletesCompositeCharacterEnabled(bool enable)
```

引擎名：`set_backspace_deletes_composite_character_enabled`

### SetCaretBlinkEnabled

```csharp
public void SetCaretBlinkEnabled(bool enabled)
```

引擎名：`set_caret_blink_enabled`

### SetCaretBlinkInterval

```csharp
public void SetCaretBlinkInterval(float interval)
```

引擎名：`set_caret_blink_interval`

### SetCaretColumn

```csharp
public void SetCaretColumn(int position)
```

引擎名：`set_caret_column`

### SetCaretForceDisplayed

```csharp
public void SetCaretForceDisplayed(bool enabled)
```

引擎名：`set_caret_force_displayed`

### SetCaretMidGraphemeEnabled

```csharp
public void SetCaretMidGraphemeEnabled(bool enabled)
```

引擎名：`set_caret_mid_grapheme_enabled`

### SetClearButtonEnabled

```csharp
public void SetClearButtonEnabled(bool enable)
```

引擎名：`set_clear_button_enabled`

### SetContextMenuEnabled

```csharp
public void SetContextMenuEnabled(bool enable)
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

### SetDrawControlChars

```csharp
public void SetDrawControlChars(bool enable)
```

引擎名：`set_draw_control_chars`

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

### SetExpandToTextLengthEnabled

```csharp
public void SetExpandToTextLengthEnabled(bool enabled)
```

引擎名：`set_expand_to_text_length_enabled`

### SetFlat

```csharp
public void SetFlat(bool enabled)
```

引擎名：`set_flat`

### SetHorizontalAlignment

```csharp
public void SetHorizontalAlignment(HorizontalAlignment alignment)
```

引擎名：`set_horizontal_alignment`

### SetIconExpandMode

```csharp
public void SetIconExpandMode(LineEdit.ExpandMode mode)
```

引擎名：`set_icon_expand_mode`

### SetKeepEditingOnTextSubmit

```csharp
public void SetKeepEditingOnTextSubmit(bool enable)
```

引擎名：`set_keep_editing_on_text_submit`

### SetLanguage

```csharp
public void SetLanguage(string language)
```

引擎名：`set_language`

### SetMaxLength

```csharp
public void SetMaxLength(int chars)
```

引擎名：`set_max_length`

### SetMiddleMousePasteEnabled

```csharp
public void SetMiddleMousePasteEnabled(bool enable)
```

引擎名：`set_middle_mouse_paste_enabled`

### SetPlaceholder

```csharp
public void SetPlaceholder(string text)
```

引擎名：`set_placeholder`

### SetRightIcon

```csharp
public void SetRightIcon(Texture2D icon)
```

引擎名：`set_right_icon`

### SetRightIconScale

```csharp
public void SetRightIconScale(float scale)
```

引擎名：`set_right_icon_scale`

### SetSecret

```csharp
public void SetSecret(bool enabled)
```

引擎名：`set_secret`

### SetSecretCharacter

```csharp
public void SetSecretCharacter(string character)
```

引擎名：`set_secret_character`

### SetSelectAllOnFocus

```csharp
public void SetSelectAllOnFocus(bool enabled)
```

引擎名：`set_select_all_on_focus`

### SetSelectingEnabled

```csharp
public void SetSelectingEnabled(bool enable)
```

引擎名：`set_selecting_enabled`

### SetShortcutKeysEnabled

```csharp
public void SetShortcutKeysEnabled(bool enable)
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

### SetVirtualKeyboardEnabled

```csharp
public void SetVirtualKeyboardEnabled(bool enable)
```

引擎名：`set_virtual_keyboard_enabled`

### SetVirtualKeyboardShowOnFocus

```csharp
public void SetVirtualKeyboardShowOnFocus(bool showOnFocus)
```

引擎名：`set_virtual_keyboard_show_on_focus`

### SetVirtualKeyboardType

```csharp
public void SetVirtualKeyboardType(LineEdit.VirtualKeyboardTypeEnum type)
```

引擎名：`set_virtual_keyboard_type`

### Unedit

```csharp
public void Unedit()
```

引擎名：`unedit`

Allows exiting edit mode while preserving focus.

## 事件（信号）

### EditingToggled

```csharp
public event LineEdit.EditingToggledEventHandler EditingToggled
```

引擎名：`editing_toggled`

Emitted when the `LineEdit` switches in or out of edit mode.

### TextChangeRejected

```csharp
public event LineEdit.TextChangeRejectedEventHandler TextChangeRejected
```

引擎名：`text_change_rejected`

Emitted when appending text that overflows the `MaxLength`. The appended text is truncated to fit `MaxLength`, and the part that couldn't fit is passed as the `rejectedSubstring` argument.

### TextChanged

```csharp
public event LineEdit.TextChangedEventHandler TextChanged
```

引擎名：`text_changed`

Emitted when the text changes.

### TextSubmitted

```csharp
public event LineEdit.TextSubmittedEventHandler TextSubmitted
```

引擎名：`text_submitted`

Emitted when the user presses the `ui_text_submit` action (by default: Enter or Kp Enter) while the `LineEdit` has focus.

## EditingToggledEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`LineEdit.EditingToggled`](LineEdit.md) event of a [`LineEdit`](LineEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void EditingToggledEventHandler(bool toggledOn)
```

## ExpandMode

命名空间 `Godot`；enum

### 枚举值

- `OriginalSize = 0`：Use the original size for the right icon.
- `FitToText = 1`：Scale the right icon's size to match the size of the text.
- `FitToLineEdit = 2`：Scale the right icon to fit the LineEdit.

## MenuItems

命名空间 `Godot`；enum

### 枚举值

- `Cut = 0`：Cuts (copies and clears) the selected text.
- `Copy = 1`：Copies the selected text.
- `Paste = 2`：Pastes the clipboard text over the selected text (or at the caret's position).
- `Clear = 3`：Erases the whole [`LineEdit`](LineEdit.md) text.
- `SelectAll = 4`：Selects the whole [`LineEdit`](LineEdit.md) text.
- `Undo = 5`：Undoes the previous action.
- `Redo = 6`：Reverse the last undo action.
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
- `Max = 31`：Represents the size of the `LineEdit.MenuItems` enum.

## TextChangeRejectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`LineEdit.TextChangeRejected`](LineEdit.md) event of a [`LineEdit`](LineEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void TextChangeRejectedEventHandler(string rejectedSubstring)
```

## TextChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`LineEdit.TextChanged`](LineEdit.md) event of a [`LineEdit`](LineEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void TextChangedEventHandler(string newText)
```

## TextSubmittedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`LineEdit.TextSubmitted`](LineEdit.md) event of a [`LineEdit`](LineEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void TextSubmittedEventHandler(string newText)
```

## VirtualKeyboardTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Default text virtual keyboard.
- `Multiline = 1`：Multiline virtual keyboard.
- `Number = 2`：Virtual number keypad, useful for PIN entry.
- `NumberDecimal = 3`：Virtual number keypad, useful for entering fractional numbers.
- `Phone = 4`：Virtual phone number keypad.
- `EmailAddress = 5`：Virtual keyboard with additional keys to assist with typing email addresses.
- `Password = 6`：Virtual keyboard for entering a password.
- `Url = 7`：Virtual keyboard with additional keys to assist with typing URLs.
