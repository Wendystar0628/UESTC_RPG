<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Tree

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A control used to show a set of internal [`TreeItem`](TreeItem.md)s in a hierarchical structure. The tree items can be selected, expanded and collapsed. The tree can have multiple columns with custom controls like [`LineEdit`](LineEdit.md)s, buttons and popups. It can be useful for structured displays and interactions.

Trees are built via code, using [`TreeItem`](TreeItem.md) objects to create the structure. They have a single root, but multiple roots can be simulated with `HideRoot`:

```csharp
public override void _Ready()
{
var tree = new Tree();
TreeItem root = tree.CreateItem();
tree.HideRoot = true;
TreeItem child1 = tree.CreateItem(root);
TreeItem child2 = tree.CreateItem(root);
TreeItem subchild1 = tree.CreateItem(child1);
subchild1.SetText(0, "Subchild1");
}

```

To iterate over all the [`TreeItem`](TreeItem.md) objects in a `Tree` object, use [`TreeItem.GetNext`](TreeItem.md) and [`TreeItem.GetFirstChild`](TreeItem.md) after getting the root through `GetRoot()`. You can use [`GodotObject.Free`](GodotObject.md) on a [`TreeItem`](TreeItem.md) to remove it from the `Tree`.

**Incremental search:** Like [`ItemList`](ItemList.md) and [`PopupMenu`](PopupMenu.md), `Tree` supports searching within the list while the control is focused. Press a key that matches the first letter of an item's name to select the first item starting with the given letter. After that point, there are two ways to perform incremental search: 1) Press the same key again before the timeout duration to select the next item starting with the same letter. 2) Press letter keys that match the rest of the word before the timeout duration to match to select the item in question directly. Both of these actions will be reset to the beginning of the list if the timeout duration has passed since the last keystroke was registered. You can adjust the timeout duration by changing `ProjectSettings.gui/timers/incremental_search_max_interval_msec`.

## 构造函数

### Tree

```csharp
public Tree()
```

## 属性

### AllowReselect

```csharp
public bool AllowReselect { get; set; }
```

引擎名：`allow_reselect`

If `true`, the currently selected cell may be selected again.

### AllowRmbSelect

```csharp
public bool AllowRmbSelect { get; set; }
```

引擎名：`allow_rmb_select`

If `true`, a right mouse button click can select items.

### AllowSearch

```csharp
public bool AllowSearch { get; set; }
```

引擎名：`allow_search`

If `true`, allows navigating the `Tree` with letter keys through incremental search.

### AutoTooltip

```csharp
public bool AutoTooltip { get; set; }
```

引擎名：`auto_tooltip`

If `true`, tree items with no tooltip assigned display their text as their tooltip. See also [`TreeItem.GetTooltipText`](TreeItem.md) and [`TreeItem.GetButtonTooltipText`](TreeItem.md).

### ColumnTitlesVisible

```csharp
public bool ColumnTitlesVisible { get; set; }
```

引擎名：`column_titles_visible`

If `true`, column titles are visible.

### Columns

```csharp
public int Columns { get; set; }
```

引擎名：`columns`

The number of columns.

Prints an error and does not allow setting the columns during mouse selection.

### DropModeFlags

```csharp
public int DropModeFlags { get; set; }
```

引擎名：`drop_mode_flags`

The drop mode as an OR combination of flags. See [`Tree.DropModeFlagsEnum`](Tree.md) constants. Once dropping is done, reverts to [`Tree.DropModeFlagsEnum.Disabled`](Tree.md). Setting this during [`Control._CanDropData()`](Control.md) is recommended.

This controls the drop sections, i.e. the decision and drawing of possible drop locations based on the mouse position.

### EnableDragUnfolding

```csharp
public bool EnableDragUnfolding { get; set; }
```

引擎名：`enable_drag_unfolding`

If `true`, tree items will unfold when hovered over during a drag-and-drop. The delay for when this happens is dictated by `dragging_unfold_wait_msec`.

### EnableRecursiveFolding

```csharp
public bool EnableRecursiveFolding { get; set; }
```

引擎名：`enable_recursive_folding`

If `true`, recursive folding is enabled for this `Tree`. Holding down Shift while clicking the fold arrow or using `ui_right`/`ui_left` shortcuts collapses or uncollapses the [`TreeItem`](TreeItem.md) and all its descendants.

### HideFolding

```csharp
public bool HideFolding { get; set; }
```

引擎名：`hide_folding`

If `true`, the folding arrow is hidden.

### HideRoot

```csharp
public bool HideRoot { get; set; }
```

引擎名：`hide_root`

If `true`, the tree's root is hidden.

### ScrollHintMode

```csharp
public Tree.ScrollHintModeEnum ScrollHintMode { get; set; }
```

引擎名：`scroll_hint_mode`

The way which scroll hints (indicators that show that the content can still be scrolled in a certain direction) will be shown.

### ScrollHorizontalEnabled

```csharp
public bool ScrollHorizontalEnabled { get; set; }
```

引擎名：`scroll_horizontal_enabled`

If `true`, enables horizontal scrolling.

### ScrollVerticalEnabled

```csharp
public bool ScrollVerticalEnabled { get; set; }
```

引擎名：`scroll_vertical_enabled`

If `true`, enables vertical scrolling.

### SelectMode

```csharp
public Tree.SelectModeEnum SelectMode { get; set; }
```

引擎名：`select_mode`

Allows single or multiple selection. See the [`Tree.SelectModeEnum`](Tree.md) constants.

### TileScrollHint

```csharp
public bool TileScrollHint { get; set; }
```

引擎名：`tile_scroll_hint`

If `true`, the scroll hint texture will be tiled instead of stretched. See `ScrollHintMode`.

## 方法

### AreColumnTitlesVisible

```csharp
public bool AreColumnTitlesVisible()
```

引擎名：`are_column_titles_visible`

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the tree. This removes all items.

Prints an error and does not allow clearing the tree if called during mouse selection.

### CreateItem

```csharp
public TreeItem CreateItem(TreeItem parent = null, int index = -1)
```

引擎名：`create_item`

Creates an item in the tree and adds it as a child of `parent`, which can be either a valid [`TreeItem`](TreeItem.md) or `null`.

If `parent` is `null`, the root item will be the parent, or the new item will be the root itself if the tree is empty.

The new item will be the `index`-th child of parent, or it will be the last child if there are not enough siblings.

Prints an error and returns `null` if called during mouse selection, or if the `parent` does not belong to this tree.

### DeselectAll

```csharp
public void DeselectAll()
```

引擎名：`deselect_all`

Deselects all tree items (rows and columns). In [`Tree.SelectModeEnum.Multi`](Tree.md) mode also removes selection cursor.

### EditSelected

```csharp
public bool EditSelected()
```

引擎名：`edit_selected`

Edits the selected tree item as if it was clicked.

Either the item must be set editable with [`TreeItem.SetEditable`](TreeItem.md) or `forceEdit` must be `true`.

Returns `true` if the item could be edited. Fails if no item is selected.

### EditSelected

```csharp
public bool EditSelected(bool forceEdit = false)
```

引擎名：`edit_selected`

Edits the selected tree item as if it was clicked.

Either the item must be set editable with [`TreeItem.SetEditable`](TreeItem.md) or `forceEdit` must be `true`.

Returns `true` if the item could be edited. Fails if no item is selected.

### EnsureCursorIsVisible

```csharp
public void EnsureCursorIsVisible()
```

引擎名：`ensure_cursor_is_visible`

Makes the currently focused cell visible.

This will scroll the tree if necessary. In [`Tree.SelectModeEnum.Row`](Tree.md) mode, this will not do horizontal scrolling, as all the cells in the selected row is focused logically.

**Note:** Despite the name of this method, the focus cursor itself is only visible in [`Tree.SelectModeEnum.Multi`](Tree.md) mode.

### GetAllowReselect

```csharp
public bool GetAllowReselect()
```

引擎名：`get_allow_reselect`

### GetAllowRmbSelect

```csharp
public bool GetAllowRmbSelect()
```

引擎名：`get_allow_rmb_select`

### GetAllowSearch

```csharp
public bool GetAllowSearch()
```

引擎名：`get_allow_search`

### GetButtonIdAtPosition

```csharp
public int GetButtonIdAtPosition(Vector2 position)
```

引擎名：`get_button_id_at_position`

Returns the button ID at `position`, or -1 if no button is there.

### GetColumnAtPosition

```csharp
public int GetColumnAtPosition(Vector2 position)
```

引擎名：`get_column_at_position`

Returns the column index at `position`, or -1 if no item is there.

### GetColumnExpandRatio

```csharp
public int GetColumnExpandRatio(int column)
```

引擎名：`get_column_expand_ratio`

Returns the expand ratio assigned to the column.

### GetColumnTitle

```csharp
public string GetColumnTitle(int column)
```

引擎名：`get_column_title`

Returns the column's title.

### GetColumnTitleAlignment

```csharp
public HorizontalAlignment GetColumnTitleAlignment(int column)
```

引擎名：`get_column_title_alignment`

Returns the column title alignment.

### GetColumnTitleDirection

```csharp
public Control.TextDirection GetColumnTitleDirection(int column)
```

引擎名：`get_column_title_direction`

Returns column title base writing direction.

### GetColumnTitleLanguage

```csharp
public string GetColumnTitleLanguage(int column)
```

引擎名：`get_column_title_language`

Returns column title language code.

### GetColumnTitleTooltipText

```csharp
public string GetColumnTitleTooltipText(int column)
```

引擎名：`get_column_title_tooltip_text`

Returns the column title's tooltip text.

### GetColumnWidth

```csharp
public int GetColumnWidth(int column)
```

引擎名：`get_column_width`

Returns the column's width in pixels.

### GetColumns

```csharp
public int GetColumns()
```

引擎名：`get_columns`

### GetCustomDrawingCanvasItem

```csharp
public Rid GetCustomDrawingCanvasItem()
```

引擎名：`get_custom_drawing_canvas_item`

Returns the internal canvas item designated for custom drawing. See [`TreeItem.SetCustomDrawCallback`](TreeItem.md).

**Note:** This canvas item clears automatically on each Tree draw call.

### GetCustomPopupRect

```csharp
public Rect2 GetCustomPopupRect()
```

引擎名：`get_custom_popup_rect`

Returns the rectangle for custom popups. Helper to create custom cell controls that display a popup. See [`TreeItem.SetCellMode`](TreeItem.md).

### GetDropModeFlags

```csharp
public int GetDropModeFlags()
```

引擎名：`get_drop_mode_flags`

### GetDropSectionAtPosition

```csharp
public int GetDropSectionAtPosition(Vector2 position)
```

引擎名：`get_drop_section_at_position`

Returns the drop section at `position`, as permitted by enabled [`Tree.DropModeFlagsEnum`](Tree.md).

- `-1` if the position is **above** the item. Typically used to insert as the item's previous sibling.

- `0` if the position is **on** the item. Typically used to insert as the item's last child.

- `1` if the position is **below** the item, when the item has no children. Typically used to insert as the item's next sibling. If the item *does* have children, this section is still reachable by hovering to the left of the item's collapse arrow, and below.

- `2` if the position is **below** the item, when the item has children. Typically used to insert as the item's first child.

- `-100` if the position is not over any item, or no [`Tree.DropModeFlagsEnum`](Tree.md) are set.

See [`Tree.DropModeFlagsEnum`](Tree.md) for a description of each drop region. To get the item which the returned drop section refers to, use `GetItemAtPosition()`.

### GetEdited

```csharp
public TreeItem GetEdited()
```

引擎名：`get_edited`

Returns the currently edited item. Can be used with `ItemEdited` to get the item that was modified.

```csharp
public override void _Ready()
{
GetNode<Tree>("Tree").ItemEdited += OnTreeItemEdited;
}

public void OnTreeItemEdited()
{
GD.Print(GetNode<Tree>("Tree").GetEdited()); // This item just got edited (e.g. checked).
}

```

### GetEditedColumn

```csharp
public int GetEditedColumn()
```

引擎名：`get_edited_column`

Returns the column for the currently edited item.

### GetItemAreaRect

```csharp
public Rect2 GetItemAreaRect(TreeItem item, int column = -1, int buttonIndex = -1)
```

引擎名：`get_item_area_rect`

Returns the rectangle area for the specified [`TreeItem`](TreeItem.md). If `column` is specified, only get the position and size of that column, otherwise get the rectangle containing all columns. If a button index is specified, the rectangle of that button will be returned.

### GetItemAtPosition

```csharp
public TreeItem GetItemAtPosition(Vector2 position)
```

引擎名：`get_item_at_position`

Returns the tree item at the specified position (relative to the tree origin position).

### GetNextSelected

```csharp
public TreeItem GetNextSelected(TreeItem from)
```

引擎名：`get_next_selected`

Returns the next selected [`TreeItem`](TreeItem.md) after the given one, or `null` if the end is reached.

If `from` is `null`, this returns the first selected item.

### GetPressedButton

```csharp
public int GetPressedButton()
```

引擎名：`get_pressed_button`

Returns the last pressed button's index.

### GetRoot

```csharp
public TreeItem GetRoot()
```

引擎名：`get_root`

Returns the tree's root item, or `null` if the tree is empty.

### GetScroll

```csharp
public Vector2 GetScroll()
```

引擎名：`get_scroll`

Returns the current scrolling position.

### GetScrollHintMode

```csharp
public Tree.ScrollHintModeEnum GetScrollHintMode()
```

引擎名：`get_scroll_hint_mode`

### GetSelectMode

```csharp
public Tree.SelectModeEnum GetSelectMode()
```

引擎名：`get_select_mode`

### GetSelected

```csharp
public TreeItem GetSelected()
```

引擎名：`get_selected`

Returns the currently focused item, or `null` if no item is focused.

In [`Tree.SelectModeEnum.Row`](Tree.md) and [`Tree.SelectModeEnum.Single`](Tree.md) modes, the focused item is same as the selected item. In [`Tree.SelectModeEnum.Multi`](Tree.md) mode, the focused item is the item under the focus cursor, not necessarily selected.

To get the currently selected item(s), use `GetNextSelected()`.

### GetSelectedColumn

```csharp
public int GetSelectedColumn()
```

引擎名：`get_selected_column`

Returns the currently focused column, or -1 if no column is focused.

In [`Tree.SelectModeEnum.Single`](Tree.md) mode, the focused column is the selected column. In [`Tree.SelectModeEnum.Row`](Tree.md) mode, the focused column is always 0 if any item is selected. In [`Tree.SelectModeEnum.Multi`](Tree.md) mode, the focused column is the column under the focus cursor, and there are not necessarily any column selected.

To tell whether a column of an item is selected, use [`TreeItem.IsSelected`](TreeItem.md).

### IsAutoTooltipEnabled

```csharp
public bool IsAutoTooltipEnabled()
```

引擎名：`is_auto_tooltip_enabled`

### IsColumnClippingContent

```csharp
public bool IsColumnClippingContent(int column)
```

引擎名：`is_column_clipping_content`

Returns `true` if the column has enabled clipping (see `SetColumnClipContent()`).

### IsColumnExpanding

```csharp
public bool IsColumnExpanding(int column)
```

引擎名：`is_column_expanding`

Returns `true` if the column has enabled expanding (see `SetColumnExpand()`).

### IsDragUnfoldingEnabled

```csharp
public bool IsDragUnfoldingEnabled()
```

引擎名：`is_drag_unfolding_enabled`

### IsFoldingHidden

```csharp
public bool IsFoldingHidden()
```

引擎名：`is_folding_hidden`

### IsHScrollEnabled

```csharp
public bool IsHScrollEnabled()
```

引擎名：`is_h_scroll_enabled`

### IsRecursiveFoldingEnabled

```csharp
public bool IsRecursiveFoldingEnabled()
```

引擎名：`is_recursive_folding_enabled`

### IsRootHidden

```csharp
public bool IsRootHidden()
```

引擎名：`is_root_hidden`

### IsScrollHintTiled

```csharp
public bool IsScrollHintTiled()
```

引擎名：`is_scroll_hint_tiled`

### IsVScrollEnabled

```csharp
public bool IsVScrollEnabled()
```

引擎名：`is_v_scroll_enabled`

### ScrollToItem

```csharp
public void ScrollToItem(TreeItem item, bool centerOnItem = false)
```

引擎名：`scroll_to_item`

Causes the `Tree` to jump to the specified [`TreeItem`](TreeItem.md).

### SetAllowReselect

```csharp
public void SetAllowReselect(bool allow)
```

引擎名：`set_allow_reselect`

### SetAllowRmbSelect

```csharp
public void SetAllowRmbSelect(bool allow)
```

引擎名：`set_allow_rmb_select`

### SetAllowSearch

```csharp
public void SetAllowSearch(bool allow)
```

引擎名：`set_allow_search`

### SetAutoTooltip

```csharp
public void SetAutoTooltip(bool enable)
```

引擎名：`set_auto_tooltip`

### SetColumnClipContent

```csharp
public void SetColumnClipContent(int column, bool enable)
```

引擎名：`set_column_clip_content`

Allows to enable clipping for column's content, making the content size ignored.

### SetColumnCustomMinimumWidth

```csharp
public void SetColumnCustomMinimumWidth(int column, int minWidth)
```

引擎名：`set_column_custom_minimum_width`

Overrides the calculated minimum width of a column. It can be set to `0` to restore the default behavior. Columns that have the "Expand" flag will use their "min_width" in a similar fashion to [`Control.SizeFlagsStretchRatio`](Control.md).

### SetColumnExpand

```csharp
public void SetColumnExpand(int column, bool expand)
```

引擎名：`set_column_expand`

If `true`, the column will have the "Expand" flag of [`Control`](Control.md). Columns that have the "Expand" flag will use their expand ratio in a similar fashion to [`Control.SizeFlagsStretchRatio`](Control.md) (see `SetColumnExpandRatio()`).

### SetColumnExpandRatio

```csharp
public void SetColumnExpandRatio(int column, int ratio)
```

引擎名：`set_column_expand_ratio`

Sets the relative expand ratio for a column. See `SetColumnExpand()`.

### SetColumnTitle

```csharp
public void SetColumnTitle(int column, string title)
```

引擎名：`set_column_title`

Sets the title of a column.

### SetColumnTitleAlignment

```csharp
public void SetColumnTitleAlignment(int column, HorizontalAlignment titleAlignment)
```

引擎名：`set_column_title_alignment`

Sets the column title alignment. Note that [`HorizontalAlignment.Fill`](HorizontalAlignment.md) is not supported for column titles.

### SetColumnTitleDirection

```csharp
public void SetColumnTitleDirection(int column, Control.TextDirection direction)
```

引擎名：`set_column_title_direction`

Sets column title base writing direction.

### SetColumnTitleLanguage

```csharp
public void SetColumnTitleLanguage(int column, string language)
```

引擎名：`set_column_title_language`

Sets the language code of the given `column`'s title to `language`. This is used for line-breaking and text shaping algorithms. If `language` is empty, the current locale is used.

### SetColumnTitleTooltipText

```csharp
public void SetColumnTitleTooltipText(int column, string tooltipText)
```

引擎名：`set_column_title_tooltip_text`

Sets the column title's tooltip text.

### SetColumnTitlesVisible

```csharp
public void SetColumnTitlesVisible(bool visible)
```

引擎名：`set_column_titles_visible`

### SetColumns

```csharp
public void SetColumns(int amount)
```

引擎名：`set_columns`

### SetDropModeFlags

```csharp
public void SetDropModeFlags(int flags)
```

引擎名：`set_drop_mode_flags`

### SetEnableDragUnfolding

```csharp
public void SetEnableDragUnfolding(bool enable)
```

引擎名：`set_enable_drag_unfolding`

### SetEnableRecursiveFolding

```csharp
public void SetEnableRecursiveFolding(bool enable)
```

引擎名：`set_enable_recursive_folding`

### SetHScrollEnabled

```csharp
public void SetHScrollEnabled(bool hScroll)
```

引擎名：`set_h_scroll_enabled`

### SetHideFolding

```csharp
public void SetHideFolding(bool hide)
```

引擎名：`set_hide_folding`

### SetHideRoot

```csharp
public void SetHideRoot(bool enable)
```

引擎名：`set_hide_root`

### SetScrollHintMode

```csharp
public void SetScrollHintMode(Tree.ScrollHintModeEnum scrollHintMode)
```

引擎名：`set_scroll_hint_mode`

### SetSelectMode

```csharp
public void SetSelectMode(Tree.SelectModeEnum mode)
```

引擎名：`set_select_mode`

### SetSelected

```csharp
public void SetSelected(TreeItem item, int column)
```

引擎名：`set_selected`

Selects the specified [`TreeItem`](TreeItem.md) and column.

### SetTileScrollHint

```csharp
public void SetTileScrollHint(bool tileScrollHint)
```

引擎名：`set_tile_scroll_hint`

### SetVScrollEnabled

```csharp
public void SetVScrollEnabled(bool hScroll)
```

引擎名：`set_v_scroll_enabled`

## 事件（信号）

### ButtonClicked

```csharp
public event Tree.ButtonClickedEventHandler ButtonClicked
```

引擎名：`button_clicked`

Emitted when a button on the tree was pressed (see [`TreeItem.AddButton`](TreeItem.md)).

### CellSelected

```csharp
public event Action CellSelected
```

引擎名：`cell_selected`

Emitted when a cell is selected.

### CheckPropagatedToItem

```csharp
public event Tree.CheckPropagatedToItemEventHandler CheckPropagatedToItem
```

引擎名：`check_propagated_to_item`

Emitted when [`TreeItem.PropagateCheck`](TreeItem.md) is called. Connect to this signal to process the items that are affected when [`TreeItem.PropagateCheck`](TreeItem.md) is invoked. The order that the items affected will be processed is as follows: the item that invoked the method, children of that item, and finally parents of that item.

### ColumnTitleClicked

```csharp
public event Tree.ColumnTitleClickedEventHandler ColumnTitleClicked
```

引擎名：`column_title_clicked`

Emitted when a column's title is clicked with either [`MouseButton.Left`](MouseButton.md) or [`MouseButton.Right`](MouseButton.md).

### CustomItemClicked

```csharp
public event Tree.CustomItemClickedEventHandler CustomItemClicked
```

引擎名：`custom_item_clicked`

Emitted when an item with [`TreeItem.TreeCellMode.Custom`](TreeItem.md) is clicked with a mouse button.

### CustomPopupEdited

```csharp
public event Tree.CustomPopupEditedEventHandler CustomPopupEdited
```

引擎名：`custom_popup_edited`

Emitted when a cell with the [`TreeItem.TreeCellMode.Custom`](TreeItem.md) is clicked to be edited.

### EmptyClicked

```csharp
public event Tree.EmptyClickedEventHandler EmptyClicked
```

引擎名：`empty_clicked`

Emitted when a mouse button is clicked in the empty space of the tree.

### ItemActivated

```csharp
public event Action ItemActivated
```

引擎名：`item_activated`

Emitted when an item is double-clicked, or selected with a `ui_accept` input event (e.g. using Enter or Space on the keyboard).

### ItemCollapsed

```csharp
public event Tree.ItemCollapsedEventHandler ItemCollapsed
```

引擎名：`item_collapsed`

Emitted when an item is expanded or collapsed by clicking on the folding arrow or through code.

**Note:** Despite its name, this signal is also emitted when an item is expanded.

### ItemEdited

```csharp
public event Action ItemEdited
```

引擎名：`item_edited`

Emitted when an item is edited.

### ItemIconDoubleClicked

```csharp
public event Action ItemIconDoubleClicked
```

引擎名：`item_icon_double_clicked`

Emitted when an item's icon is double-clicked. For a signal that emits when any part of the item is double-clicked, see `ItemActivated`.

### ItemMouseSelected

```csharp
public event Tree.ItemMouseSelectedEventHandler ItemMouseSelected
```

引擎名：`item_mouse_selected`

Emitted when an item is selected with a mouse button.

### ItemSelected

```csharp
public event Action ItemSelected
```

引擎名：`item_selected`

Emitted when an item is selected.

### MultiSelected

```csharp
public event Tree.MultiSelectedEventHandler MultiSelected
```

引擎名：`multi_selected`

Emitted instead of `ItemSelected` if `SelectMode` is set to [`Tree.SelectModeEnum.Multi`](Tree.md).

### NothingSelected

```csharp
public event Action NothingSelected
```

引擎名：`nothing_selected`

Emitted when a left mouse button click does not select any item.

## ButtonClickedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Tree.ButtonClicked`](Tree.md) event of a [`Tree`](Tree.md) class.

### 签名

#### Invoke

```csharp
delegate void ButtonClickedEventHandler(TreeItem item, long column, long id, long mouseButtonIndex)
```

## CheckPropagatedToItemEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Tree.CheckPropagatedToItem`](Tree.md) event of a [`Tree`](Tree.md) class.

### 签名

#### Invoke

```csharp
delegate void CheckPropagatedToItemEventHandler(TreeItem item, long column)
```

## ColumnTitleClickedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Tree.ColumnTitleClicked`](Tree.md) event of a [`Tree`](Tree.md) class.

### 签名

#### Invoke

```csharp
delegate void ColumnTitleClickedEventHandler(long column, long mouseButtonIndex)
```

## CustomItemClickedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Tree.CustomItemClicked`](Tree.md) event of a [`Tree`](Tree.md) class.

### 签名

#### Invoke

```csharp
delegate void CustomItemClickedEventHandler(long mouseButtonIndex)
```

## CustomPopupEditedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Tree.CustomPopupEdited`](Tree.md) event of a [`Tree`](Tree.md) class.

### 签名

#### Invoke

```csharp
delegate void CustomPopupEditedEventHandler(bool arrowClicked)
```

## DropModeFlagsEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disables all drop sections.
- `OnItem = 1`：Enables the "on item" drop section.
- `Inbetween = 2`：Enables "above item" and "below item" drop sections.

## EmptyClickedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Tree.EmptyClicked`](Tree.md) event of a [`Tree`](Tree.md) class.

### 签名

#### Invoke

```csharp
delegate void EmptyClickedEventHandler(Vector2 clickPosition, long mouseButtonIndex)
```

## ItemCollapsedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Tree.ItemCollapsed`](Tree.md) event of a [`Tree`](Tree.md) class.

### 签名

#### Invoke

```csharp
delegate void ItemCollapsedEventHandler(TreeItem item)
```

## ItemMouseSelectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Tree.ItemMouseSelected`](Tree.md) event of a [`Tree`](Tree.md) class.

### 签名

#### Invoke

```csharp
delegate void ItemMouseSelectedEventHandler(Vector2 mousePosition, long mouseButtonIndex)
```

## MultiSelectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Tree.MultiSelected`](Tree.md) event of a [`Tree`](Tree.md) class.

### 签名

#### Invoke

```csharp
delegate void MultiSelectedEventHandler(TreeItem item, long column, bool selected)
```

## ScrollHintModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Scroll hints will never be shown.
- `Both = 1`：Scroll hints will be shown at the top and bottom.
- `Top = 2`：Only the top scroll hint will be shown.
- `Bottom = 3`：Only the bottom scroll hint will be shown.

## SelectModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Single = 0`：Allows selection of a single cell at a time.
- `Row = 1`：Allows selection of a single row at a time.
- `Multi = 2`：Allows selection of multiple cells at the same time.
