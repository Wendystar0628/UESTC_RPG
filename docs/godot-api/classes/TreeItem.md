<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TreeItem

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

A single item of a [`Tree`](Tree.md) control. It can contain other `TreeItem`s as children, which allows it to create a hierarchy. It can also contain text and buttons. `TreeItem` is not a [`Node`](Node.md), it is internal to the [`Tree`](Tree.md).

To create a `TreeItem`, use [`Tree.CreateItem`](Tree.md) or `CreateChild()`. To remove a `TreeItem`, use [`GodotObject.Free`](GodotObject.md).

**Note:** The ID values used for buttons are 32-bit, unlike `System.Int32` which is always 64-bit. They go from `-2147483648` to `2147483647`.

## 属性

### Collapsed

```csharp
public bool Collapsed { get; set; }
```

引擎名：`collapsed`

If `true`, the TreeItem is collapsed.

### CustomMinimumHeight

```csharp
public int CustomMinimumHeight { get; set; }
```

引擎名：`custom_minimum_height`

The custom minimum height.

### DisableFolding

```csharp
public bool DisableFolding { get; set; }
```

引擎名：`disable_folding`

If `true`, folding is disabled for this TreeItem.

### Visible

```csharp
public bool Visible { get; set; }
```

引擎名：`visible`

If `true`, the `TreeItem` is visible (default).

Note that if a `TreeItem` is set to not be visible, none of its children will be visible either.

## 方法

### AddButton

```csharp
public void AddButton(int column, Texture2D button, int id = -1, bool disabled = false, string tooltipText = "", string description = "")
```

引擎名：`add_button`

Adds a button with [`Texture2D`](Texture2D.md) `button` to the end of the cell at column `column`. The `id` is used to identify the button in the according [`Tree.ButtonClicked`](Tree.md) signal and can be different from the buttons index. If not specified, the next available index is used, which may be retrieved by calling `GetButtonCount()` immediately before this method. Optionally, the button can be `disabled` and have a `tooltipText`. `description` is used as the button description for assistive apps.

### AddButton

```csharp
public void AddButton(int column, Texture2D button, int id, bool disabled, string tooltipText)
```

引擎名：`add_button`

Adds a button with [`Texture2D`](Texture2D.md) `button` to the end of the cell at column `column`. The `id` is used to identify the button in the according [`Tree.ButtonClicked`](Tree.md) signal and can be different from the buttons index. If not specified, the next available index is used, which may be retrieved by calling `GetButtonCount()` immediately before this method. Optionally, the button can be `disabled` and have a `tooltipText`. `description` is used as the button description for assistive apps.

### AddChild

```csharp
public void AddChild(TreeItem child)
```

引擎名：`add_child`

Adds a previously unparented `TreeItem` as a direct child of this one. The `child` item must not be a part of any [`Tree`](Tree.md) or parented to any `TreeItem`. See also `RemoveChild()`.

### CallRecursive

```csharp
public void CallRecursive(StringName method, ReadOnlySpan<Variant> args)
```

引擎名：`call_recursive`

Calls the `method` on the actual TreeItem and its children recursively. Pass parameters as a comma separated list.

### CallRecursive

```csharp
public void CallRecursive(StringName method, params Variant[] args)
```

引擎名：`call_recursive`

Calls the `method` on the actual TreeItem and its children recursively. Pass parameters as a comma separated list.

### ClearButtons

```csharp
public void ClearButtons()
```

引擎名：`clear_buttons`

Removes all buttons from all columns of this item.

### ClearCustomBgColor

```csharp
public void ClearCustomBgColor(int column)
```

引擎名：`clear_custom_bg_color`

Resets the background color for the given column to default.

### ClearCustomColor

```csharp
public void ClearCustomColor(int column)
```

引擎名：`clear_custom_color`

Resets the color for the given column to default.

### CreateChild

```csharp
public TreeItem CreateChild(int index = -1)
```

引擎名：`create_child`

Creates an item and adds it as a child.

The new item will be inserted as position `index` (the default value `-1` means the last position), or it will be the last child if `index` is higher than the child count.

### Deselect

```csharp
public void Deselect(int column)
```

引擎名：`deselect`

Deselects the given column.

### EraseButton

```csharp
public void EraseButton(int column, int buttonIndex)
```

引擎名：`erase_button`

Removes the button at index `buttonIndex` in column `column`.

### GetAutoTranslateMode

```csharp
public Node.AutoTranslateModeEnum GetAutoTranslateMode(int column)
```

引擎名：`get_auto_translate_mode`

Returns the column's auto translate mode.

### GetAutowrapMode

```csharp
public TextServer.AutowrapMode GetAutowrapMode(int column)
```

引擎名：`get_autowrap_mode`

Returns the text autowrap mode in the given `column`. By default it is [`TextServer.AutowrapMode.Off`](TextServer.md).

### GetAutowrapTrimFlags

```csharp
public TextServer.LineBreakFlag GetAutowrapTrimFlags(int column)
```

引擎名：`get_autowrap_trim_flags`

Returns the autowrap trim flags for the given `column`. By default, both [`TextServer.LineBreakFlag.TrimStartEdgeSpaces`](TextServer.md) and [`TextServer.LineBreakFlag.TrimEndEdgeSpaces`](TextServer.md) are enabled.

### GetButton

```csharp
public Texture2D GetButton(int column, int buttonIndex)
```

引擎名：`get_button`

Returns the [`Texture2D`](Texture2D.md) of the button at index `buttonIndex` in column `column`.

### GetButtonById

```csharp
public int GetButtonById(int column, int id)
```

引擎名：`get_button_by_id`

Returns the button index if there is a button with ID `id` in column `column`, otherwise returns -1.

### GetButtonColor

```csharp
public Color GetButtonColor(int column, int id)
```

引擎名：`get_button_color`

Returns the color of the button with ID `id` in column `column`. If the specified button does not exist, returns `Color.BLACK`.

### GetButtonCount

```csharp
public int GetButtonCount(int column)
```

引擎名：`get_button_count`

Returns the number of buttons in column `column`.

### GetButtonId

```csharp
public int GetButtonId(int column, int buttonIndex)
```

引擎名：`get_button_id`

Returns the ID for the button at index `buttonIndex` in column `column`.

### GetButtonTooltipText

```csharp
public string GetButtonTooltipText(int column, int buttonIndex)
```

引擎名：`get_button_tooltip_text`

Returns the tooltip text for the button at index `buttonIndex` in column `column`.

### GetCellMode

```csharp
public TreeItem.TreeCellMode GetCellMode(int column)
```

引擎名：`get_cell_mode`

Returns the column's cell mode.

### GetChild

```csharp
public TreeItem GetChild(int index)
```

引擎名：`get_child`

Returns a child item by its `index` (see `GetChildCount()`). This method is often used for iterating all children of an item.

Negative indices access the children from the last one.

### GetChildCount

```csharp
public int GetChildCount()
```

引擎名：`get_child_count`

Returns the number of child items.

### GetChildren

```csharp
public Array<TreeItem> GetChildren()
```

引擎名：`get_children`

Returns an array of references to the item's children.

### GetCustomBgColor

```csharp
public Color GetCustomBgColor(int column)
```

引擎名：`get_custom_bg_color`

Returns the custom background color of column `column`.

### GetCustomColor

```csharp
public Color GetCustomColor(int column)
```

引擎名：`get_custom_color`

Returns the custom color of column `column`.

### GetCustomDrawCallback

```csharp
public Callable GetCustomDrawCallback(int column)
```

引擎名：`get_custom_draw_callback`

Returns the custom callback of column `column`.

### GetCustomFont

```csharp
public Font GetCustomFont(int column)
```

引擎名：`get_custom_font`

Returns custom font used to draw text in the column `column`.

### GetCustomFontSize

```csharp
public int GetCustomFontSize(int column)
```

引擎名：`get_custom_font_size`

Returns custom font size used to draw text in the column `column`.

### GetCustomMinimumHeight

```csharp
public int GetCustomMinimumHeight()
```

引擎名：`get_custom_minimum_height`

### GetCustomStylebox

```csharp
public StyleBox GetCustomStylebox(int column)
```

引擎名：`get_custom_stylebox`

Returns the given column's custom [`StyleBox`](StyleBox.md) used to draw the background.

### GetDescription

```csharp
public string GetDescription(int column)
```

引擎名：`get_description`

Returns the given column's description for assistive apps.

### GetExpandRight

```csharp
public bool GetExpandRight(int column)
```

引擎名：`get_expand_right`

Returns `true` if `expand_right` is set.

### GetFirstChild

```csharp
public TreeItem GetFirstChild()
```

引擎名：`get_first_child`

Returns the TreeItem's first child.

### GetIcon

```csharp
public Texture2D GetIcon(int column)
```

引擎名：`get_icon`

Returns the given column's icon [`Texture2D`](Texture2D.md). Error if no icon is set.

### GetIconMaxWidth

```csharp
public int GetIconMaxWidth(int column)
```

引擎名：`get_icon_max_width`

Returns the maximum allowed width of the icon in the given `column`.

### GetIconModulate

```csharp
public Color GetIconModulate(int column)
```

引擎名：`get_icon_modulate`

Returns the [`Color`](Color.md) modulating the column's icon.

### GetIconOverlay

```csharp
public Texture2D GetIconOverlay(int column)
```

引擎名：`get_icon_overlay`

Returns the given column's icon overlay [`Texture2D`](Texture2D.md).

### GetIconRegion

```csharp
public Rect2 GetIconRegion(int column)
```

引擎名：`get_icon_region`

Returns the icon [`Texture2D`](Texture2D.md) region as [`Rect2`](Rect2.md).

### GetIndex

```csharp
public int GetIndex()
```

引擎名：`get_index`

Returns the node's order in the tree. For example, if called on the first child item the position is `0`.

### GetLanguage

```csharp
public string GetLanguage(int column)
```

引擎名：`get_language`

Returns item's text language code.

### GetMetadata

```csharp
public Variant GetMetadata(int column)
```

引擎名：`get_metadata`

Returns the metadata value that was set for the given column using `SetMetadata()`.

### GetNext

```csharp
public TreeItem GetNext()
```

引擎名：`get_next`

Returns the next sibling TreeItem in the tree or a `null` object if there is none.

### GetNextInTree

```csharp
public TreeItem GetNextInTree(bool wrap = false)
```

引擎名：`get_next_in_tree`

Returns the next TreeItem in the tree (in the context of a depth-first search) or a `null` object if there is none.

If `wrap` is enabled, the method will wrap around to the first element in the tree when called on the last element, otherwise it returns `null`.

### GetNextVisible

```csharp
public TreeItem GetNextVisible(bool wrap = false)
```

引擎名：`get_next_visible`

Returns the next visible TreeItem in the tree (in the context of a depth-first search) or a `null` object if there is none.

If `wrap` is enabled, the method will wrap around to the first visible element in the tree when called on the last visible element, otherwise it returns `null`.

### GetParent

```csharp
public TreeItem GetParent()
```

引擎名：`get_parent`

Returns the parent TreeItem or a `null` object if there is none.

### GetPrev

```csharp
public TreeItem GetPrev()
```

引擎名：`get_prev`

Returns the previous sibling TreeItem in the tree or a `null` object if there is none.

### GetPrevInTree

```csharp
public TreeItem GetPrevInTree(bool wrap = false)
```

引擎名：`get_prev_in_tree`

Returns the previous TreeItem in the tree (in the context of a depth-first search) or a `null` object if there is none.

If `wrap` is enabled, the method will wrap around to the last element in the tree when called on the first visible element, otherwise it returns `null`.

### GetPrevVisible

```csharp
public TreeItem GetPrevVisible(bool wrap = false)
```

引擎名：`get_prev_visible`

Returns the previous visible sibling TreeItem in the tree (in the context of a depth-first search) or a `null` object if there is none.

If `wrap` is enabled, the method will wrap around to the last visible element in the tree when called on the first visible element, otherwise it returns `null`.

### GetRange

```csharp
public double GetRange(int column)
```

引擎名：`get_range`

Returns the value of a [`TreeItem.TreeCellMode.Range`](TreeItem.md) column.

### GetRangeConfig

```csharp
public Dictionary GetRangeConfig(int column)
```

引擎名：`get_range_config`

Returns a dictionary containing the range parameters for a given column. The keys are "min", "max", "step", and "expr".

### GetStructuredTextBidiOverride

```csharp
public TextServer.StructuredTextParser GetStructuredTextBidiOverride(int column)
```

引擎名：`get_structured_text_bidi_override`

Returns the BiDi algorithm override set for this cell.

### GetStructuredTextBidiOverrideOptions

```csharp
public Array GetStructuredTextBidiOverrideOptions(int column)
```

引擎名：`get_structured_text_bidi_override_options`

Returns the additional BiDi options set for this cell.

### GetSuffix

```csharp
public string GetSuffix(int column)
```

引擎名：`get_suffix`

Gets the suffix string shown after the column value.

### GetText

```csharp
public string GetText(int column)
```

引擎名：`get_text`

Returns the given column's text.

### GetTextAlignment

```csharp
public HorizontalAlignment GetTextAlignment(int column)
```

引擎名：`get_text_alignment`

Returns the given column's text alignment.

### GetTextDirection

```csharp
public Control.TextDirection GetTextDirection(int column)
```

引擎名：`get_text_direction`

Returns item's text base writing direction.

### GetTextOverrunBehavior

```csharp
public TextServer.OverrunBehavior GetTextOverrunBehavior(int column)
```

引擎名：`get_text_overrun_behavior`

Returns the clipping behavior when the text exceeds the item's bounding rectangle in the given `column`. By default it is [`TextServer.OverrunBehavior.TrimEllipsis`](TextServer.md).

### GetTooltipText

```csharp
public string GetTooltipText(int column)
```

引擎名：`get_tooltip_text`

Returns the given column's tooltip text.

### GetTree

```csharp
public Tree GetTree()
```

引擎名：`get_tree`

Returns the [`Tree`](Tree.md) that owns this TreeItem.

### IsAcceptingChildren

```csharp
public bool IsAcceptingChildren()
```

引擎名：`is_accepting_children`

Returns `true` if this `TreeItem` is allowed to accept children.

### IsAnyCollapsed

```csharp
public bool IsAnyCollapsed(bool onlyVisible = false)
```

引擎名：`is_any_collapsed`

Returns `true` if this `TreeItem`, or any of its descendants, is collapsed.

If `onlyVisible` is `true` it ignores non-visible `TreeItem`s.

### IsButtonDisabled

```csharp
public bool IsButtonDisabled(int column, int buttonIndex)
```

引擎名：`is_button_disabled`

Returns `true` if the button at index `buttonIndex` for the given `column` is disabled.

### IsChecked

```csharp
public bool IsChecked(int column)
```

引擎名：`is_checked`

Returns `true` if the given `column` is checked.

### IsCollapsed

```csharp
public bool IsCollapsed()
```

引擎名：`is_collapsed`

### IsCustomSetAsButton

```csharp
public bool IsCustomSetAsButton(int column)
```

引擎名：`is_custom_set_as_button`

Returns `true` if the cell was made into a button with `SetCustomAsButton()`.

### IsEditMultiline

```csharp
public bool IsEditMultiline(int column)
```

引擎名：`is_edit_multiline`

Returns `true` if the given `column` is multiline editable.

### IsEditable

```csharp
public bool IsEditable(int column)
```

引擎名：`is_editable`

Returns `true` if the given `column` is editable.

### IsFoldingDisabled

```csharp
public bool IsFoldingDisabled()
```

引擎名：`is_folding_disabled`

### IsIndeterminate

```csharp
public bool IsIndeterminate(int column)
```

引擎名：`is_indeterminate`

Returns `true` if the given `column` is indeterminate.

### IsSelectable

```csharp
public bool IsSelectable(int column)
```

引擎名：`is_selectable`

Returns `true` if the given `column` is selectable.

### IsSelected

```csharp
public bool IsSelected(int column)
```

引擎名：`is_selected`

Returns `true` if the given `column` is selected.

### IsVisible

```csharp
public bool IsVisible()
```

引擎名：`is_visible`

### IsVisibleInTree

```csharp
public bool IsVisibleInTree()
```

引擎名：`is_visible_in_tree`

Returns `true` if `Visible` is `true` and all its ancestors are also visible.

### MoveAfter

```csharp
public void MoveAfter(TreeItem item)
```

引擎名：`move_after`

Moves this TreeItem right after the given `item`.

**Note:** You can't move to the root or move the root.

### MoveBefore

```csharp
public void MoveBefore(TreeItem item)
```

引擎名：`move_before`

Moves this TreeItem right before the given `item`.

**Note:** You can't move to the root or move the root.

### PropagateCheck

```csharp
public void PropagateCheck(int column, bool emitSignal = true)
```

引擎名：`propagate_check`

Propagates this item's checked status to its children and parents for the given `column`. It is possible to process the items affected by this method call by connecting to [`Tree.CheckPropagatedToItem`](Tree.md). The order that the items affected will be processed is as follows: the item invoking this method, children of that item, and finally parents of that item. If `emitSignal` is `false`, then [`Tree.CheckPropagatedToItem`](Tree.md) will not be emitted.

### RemoveChild

```csharp
public void RemoveChild(TreeItem child)
```

引擎名：`remove_child`

Removes the given child `TreeItem` and all its children from the [`Tree`](Tree.md). Note that it doesn't free the item from memory, so it can be reused later (see `AddChild()`). To completely remove a `TreeItem` use [`GodotObject.Free`](GodotObject.md).

**Note:** If you want to move a child from one [`Tree`](Tree.md) to another, then instead of removing and adding it manually you can use `MoveBefore()` or `MoveAfter()`.

### Select

```csharp
public void Select(int column)
```

引擎名：`select`

Selects the given `column`. If `setAsCursor` is `true`, the [`Tree`](Tree.md)'s cursor will be moved to this item (only matters if [`Tree.SelectMode`](Tree.md) is set to [`Tree.SelectModeEnum.Multi`](Tree.md)).

### Select

```csharp
public void Select(int column, bool setAsCursor = true)
```

引擎名：`select`

Selects the given `column`. If `setAsCursor` is `true`, the [`Tree`](Tree.md)'s cursor will be moved to this item (only matters if [`Tree.SelectMode`](Tree.md) is set to [`Tree.SelectModeEnum.Multi`](Tree.md)).

### SetAcceptChildren

```csharp
public void SetAcceptChildren(bool allowed)
```

引擎名：`set_accept_children`

Sets `TreeItem`'s ability to accept children.

### SetAutoTranslateMode

```csharp
public void SetAutoTranslateMode(int column, Node.AutoTranslateModeEnum mode)
```

引擎名：`set_auto_translate_mode`

Sets the given column's auto translate mode to `mode`.

All columns use [`Node.AutoTranslateModeEnum.Inherit`](Node.md) by default, which uses the same auto translate mode as the [`Tree`](Tree.md) itself.

### SetAutowrapMode

```csharp
public void SetAutowrapMode(int column, TextServer.AutowrapMode autowrapMode)
```

引擎名：`set_autowrap_mode`

Sets the autowrap mode in the given `column`. If set to something other than [`TextServer.AutowrapMode.Off`](TextServer.md), the text gets wrapped inside the cell's bounding rectangle.

### SetAutowrapTrimFlags

```csharp
public void SetAutowrapTrimFlags(int column, TextServer.LineBreakFlag flags)
```

引擎名：`set_autowrap_trim_flags`

Sets the autowrap trim flags for the given `column`. These flags control whether leading and trailing spaces are trimmed on wrapped lines. Set to `0` to disable all trimming.

### SetButton

```csharp
public void SetButton(int column, int buttonIndex, Texture2D button)
```

引擎名：`set_button`

Sets the given column's button [`Texture2D`](Texture2D.md) at index `buttonIndex` to `button`.

### SetButtonColor

```csharp
public void SetButtonColor(int column, int buttonIndex, Color color)
```

引擎名：`set_button_color`

Sets the given column's button color at index `buttonIndex` to `color`.

### SetButtonDescription

```csharp
public void SetButtonDescription(int column, int buttonIndex, string description)
```

引擎名：`set_button_description`

Sets the given column's button description at index `buttonIndex` for assistive apps.

### SetButtonDisabled

```csharp
public void SetButtonDisabled(int column, int buttonIndex, bool disabled)
```

引擎名：`set_button_disabled`

If `true`, disables the button at index `buttonIndex` in the given `column`.

### SetButtonTooltipText

```csharp
public void SetButtonTooltipText(int column, int buttonIndex, string tooltip)
```

引擎名：`set_button_tooltip_text`

Sets the tooltip text for the button at index `buttonIndex` in the given `column`.

### SetCellMode

```csharp
public void SetCellMode(int column, TreeItem.TreeCellMode mode)
```

引擎名：`set_cell_mode`

Sets the given column's cell mode to `mode`. This determines how the cell is displayed and edited.

### SetChecked

```csharp
public void SetChecked(int column, bool checked)
```

引擎名：`set_checked`

If `checked` is `true`, the given `column` is checked. Clears column's indeterminate status.

### SetCollapsed

```csharp
public void SetCollapsed(bool enable)
```

引擎名：`set_collapsed`

### SetCollapsedRecursive

```csharp
public void SetCollapsedRecursive(bool enable)
```

引擎名：`set_collapsed_recursive`

Collapses or uncollapses this `TreeItem` and all the descendants of this item.

### SetCustomAsButton

```csharp
public void SetCustomAsButton(int column, bool enable)
```

引擎名：`set_custom_as_button`

Makes a cell with [`TreeItem.TreeCellMode.Custom`](TreeItem.md) display as a non-flat button with a [`StyleBox`](StyleBox.md).

### SetCustomBgColor

```csharp
public void SetCustomBgColor(int column, Color color, bool justOutline = false)
```

引擎名：`set_custom_bg_color`

Sets the given column's custom background color and whether to just use it as an outline.

**Note:** If a custom [`StyleBox`](StyleBox.md) is set, the background color will be drawn behind it.

### SetCustomColor

```csharp
public void SetCustomColor(int column, Color color)
```

引擎名：`set_custom_color`

Sets the given column's custom color.

### SetCustomDraw

```csharp
public void SetCustomDraw(int column, GodotObject object, StringName callback)
```

引擎名：`set_custom_draw`

Sets the given column's custom draw callback to the `callback` method on `object`.

The method named `callback` should accept two arguments: the `TreeItem` that is drawn and its position and size as a [`Rect2`](Rect2.md).

### SetCustomDrawCallback

```csharp
public void SetCustomDrawCallback(int column, Callable callback)
```

引擎名：`set_custom_draw_callback`

Sets the given column's custom draw callback. Use an empty [`Callable`](Callable.md) (`Callable()`) to clear the custom callback. The cell has to be in [`TreeItem.TreeCellMode.Custom`](TreeItem.md) to use this feature.

The `callback` should accept two arguments: the `TreeItem` that is drawn and its position and size as a [`Rect2`](Rect2.md).

To draw custom content over the native style, please use [`Tree.GetCustomDrawingCanvasItem`](Tree.md).

### SetCustomFont

```csharp
public void SetCustomFont(int column, Font font)
```

引擎名：`set_custom_font`

Sets custom font used to draw text in the given `column`.

### SetCustomFontSize

```csharp
public void SetCustomFontSize(int column, int fontSize)
```

引擎名：`set_custom_font_size`

Sets custom font size used to draw text in the given `column`.

### SetCustomMinimumHeight

```csharp
public void SetCustomMinimumHeight(int height)
```

引擎名：`set_custom_minimum_height`

### SetCustomStylebox

```csharp
public void SetCustomStylebox(int column, StyleBox stylebox)
```

引擎名：`set_custom_stylebox`

Sets the given column's custom [`StyleBox`](StyleBox.md) used to draw the background.

**Note:** If a custom background color is set, the [`StyleBox`](StyleBox.md) will be drawn in front of it.

### SetDescription

```csharp
public void SetDescription(int column, string description)
```

引擎名：`set_description`

Sets the given column's description for assistive apps.

### SetDisableFolding

```csharp
public void SetDisableFolding(bool disable)
```

引擎名：`set_disable_folding`

### SetEditMultiline

```csharp
public void SetEditMultiline(int column, bool multiline)
```

引擎名：`set_edit_multiline`

If `multiline` is `true`, the given `column` is multiline editable.

**Note:** This option only affects the type of control ([`LineEdit`](LineEdit.md) or [`TextEdit`](TextEdit.md)) that appears when editing the column. You can set multiline values with `SetText()` even if the column is not multiline editable.

### SetEditable

```csharp
public void SetEditable(int column, bool enabled)
```

引擎名：`set_editable`

If `enabled` is `true`, the given `column` is editable.

### SetExpandRight

```csharp
public void SetExpandRight(int column, bool enable)
```

引擎名：`set_expand_right`

If `enable` is `true`, the given `column` is expanded to the right.

### SetIcon

```csharp
public void SetIcon(int column, Texture2D texture)
```

引擎名：`set_icon`

Sets the given cell's icon [`Texture2D`](Texture2D.md). If the cell is in [`TreeItem.TreeCellMode.Icon`](TreeItem.md) mode, the icon is displayed in the center of the cell. Otherwise, the icon is displayed before the cell's text. [`TreeItem.TreeCellMode.Range`](TreeItem.md) does not display an icon.

### SetIconMaxWidth

```csharp
public void SetIconMaxWidth(int column, int width)
```

引擎名：`set_icon_max_width`

Sets the maximum allowed width of the icon in the given `column`. This limit is applied on top of the default size of the icon and on top of `Tree.icon_max_width`. The height is adjusted according to the icon's ratio.

### SetIconModulate

```csharp
public void SetIconModulate(int column, Color modulate)
```

引擎名：`set_icon_modulate`

Modulates the given column's icon with `modulate`.

### SetIconOverlay

```csharp
public void SetIconOverlay(int column, Texture2D texture)
```

引擎名：`set_icon_overlay`

Sets the given cell's icon overlay [`Texture2D`](Texture2D.md). The cell has to be in [`TreeItem.TreeCellMode.Icon`](TreeItem.md) mode, and icon has to be set. Overlay is drawn on top of icon, in the bottom left corner.

### SetIconRegion

```csharp
public void SetIconRegion(int column, Rect2 region)
```

引擎名：`set_icon_region`

Sets the given column's icon's texture region.

### SetIndeterminate

```csharp
public void SetIndeterminate(int column, bool indeterminate)
```

引擎名：`set_indeterminate`

If `indeterminate` is `true`, the given `column` is marked indeterminate.

**Note:** If set `true` from `false`, then column is cleared of checked status.

### SetLanguage

```csharp
public void SetLanguage(int column, string language)
```

引擎名：`set_language`

Sets the language code of the given `column`'s text to `language`. This is used for line-breaking and text shaping algorithms. If `language` is empty, the current locale is used.

### SetMetadata

```csharp
public void SetMetadata(int column, Variant meta)
```

引擎名：`set_metadata`

Sets the metadata value for the given column, which can be retrieved later using `GetMetadata()`. This can be used, for example, to store a reference to the original data.

### SetRange

```csharp
public void SetRange(int column, double value)
```

引擎名：`set_range`

Sets the value of a [`TreeItem.TreeCellMode.Range`](TreeItem.md) column.

### SetRangeConfig

```csharp
public void SetRangeConfig(int column, double min, double max, double step, bool expr = false)
```

引擎名：`set_range_config`

Sets the range of accepted values for a column. The column must be in the [`TreeItem.TreeCellMode.Range`](TreeItem.md) mode.

If `expr` is `true`, the edit mode slider will use an exponential scale as with [`Range.ExpEdit`](Range.md).

### SetSelectable

```csharp
public void SetSelectable(int column, bool selectable)
```

引擎名：`set_selectable`

If `selectable` is `true`, the given `column` is selectable.

### SetStructuredTextBidiOverride

```csharp
public void SetStructuredTextBidiOverride(int column, TextServer.StructuredTextParser parser)
```

引擎名：`set_structured_text_bidi_override`

Set BiDi algorithm override for the structured text. Has effect for cells that display text.

### SetStructuredTextBidiOverrideOptions

```csharp
public void SetStructuredTextBidiOverrideOptions(int column, Array args)
```

引擎名：`set_structured_text_bidi_override_options`

Set additional options for BiDi override. Has effect for cells that display text.

### SetSuffix

```csharp
public void SetSuffix(int column, string text)
```

引擎名：`set_suffix`

Sets a string to be shown after a column's value (for example, a unit abbreviation).

### SetText

```csharp
public void SetText(int column, string text)
```

引擎名：`set_text`

Sets the given column's text value.

### SetTextAlignment

```csharp
public void SetTextAlignment(int column, HorizontalAlignment textAlignment)
```

引擎名：`set_text_alignment`

Sets the given column's text alignment to `textAlignment`.

### SetTextDirection

```csharp
public void SetTextDirection(int column, Control.TextDirection direction)
```

引擎名：`set_text_direction`

Sets item's text base writing direction.

### SetTextOverrunBehavior

```csharp
public void SetTextOverrunBehavior(int column, TextServer.OverrunBehavior overrunBehavior)
```

引擎名：`set_text_overrun_behavior`

Sets the clipping behavior when the text exceeds the item's bounding rectangle in the given `column`.

### SetTooltipText

```csharp
public void SetTooltipText(int column, string tooltip)
```

引擎名：`set_tooltip_text`

Sets the given column's tooltip text.

### SetVisible

```csharp
public void SetVisible(bool enable)
```

引擎名：`set_visible`

### UncollapseTree

```csharp
public void UncollapseTree()
```

引擎名：`uncollapse_tree`

Uncollapses all `TreeItem`s necessary to reveal this `TreeItem`, i.e. all ancestor `TreeItem`s.

## TreeCellMode

命名空间 `Godot`；enum

### 枚举值

- `String = 0`：Cell shows a string label, optionally with an icon.
- `Check = 1`：Cell shows a checkbox, optionally with text and an icon.
- `Range = 2`：Cell shows a numeric range.
- `Icon = 3`：Cell shows an icon.
- `Custom = 4`：Cell shows as a clickable button.
