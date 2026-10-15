<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ItemList

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This control provides a vertical list of selectable items that may be in a single or in multiple columns, with each item having options for text and an icon. Tooltips are supported and may be different for every item in the list.

Selectable items in the list may be selected or deselected and multiple selection may be enabled. Selection with right mouse button may also be enabled to allow use of popup context menus. Items may also be "activated" by double-clicking them or by pressing Enter.

Item text only supports single-line strings. Newline characters (e.g. `\n`) in the string won't produce a newline. Text wrapping is enabled in [`ItemList.IconModeEnum.Top`](ItemList.md) mode, but the column's width is adjusted to fully fit its content by default. You need to set `FixedColumnWidth` greater than zero to wrap the text.

All `set_*` methods allow negative item indices, i.e. `-1` to access the last item, `-2` to select the second-to-last item, and so on.

**Incremental search:** Like [`PopupMenu`](PopupMenu.md) and [`Tree`](Tree.md), `ItemList` supports searching within the list while the control is focused. Press a key that matches the first letter of an item's name to select the first item starting with the given letter. After that point, there are two ways to perform incremental search: 1) Press the same key again before the timeout duration to select the next item starting with the same letter. 2) Press letter keys that match the rest of the word before the timeout duration to match to select the item in question directly. Both of these actions will be reset to the beginning of the list if the timeout duration has passed since the last keystroke was registered. You can adjust the timeout duration by changing `ProjectSettings.gui/timers/incremental_search_max_interval_msec`.

## 构造函数

### ItemList

```csharp
public ItemList()
```

## 属性

### AllowReselect

```csharp
public bool AllowReselect { get; set; }
```

引擎名：`allow_reselect`

If `true`, the currently selected item can be selected again.

### AllowRmbSelect

```csharp
public bool AllowRmbSelect { get; set; }
```

引擎名：`allow_rmb_select`

If `true`, right mouse button click can select items.

### AllowSearch

```csharp
public bool AllowSearch { get; set; }
```

引擎名：`allow_search`

If `true`, allows navigating the `ItemList` with letter keys through incremental search.

### AutoHeight

```csharp
public bool AutoHeight { get; set; }
```

引擎名：`auto_height`

If `true`, the control will automatically resize the height to fit its content.

### AutoWidth

```csharp
public bool AutoWidth { get; set; }
```

引擎名：`auto_width`

If `true`, the control will automatically resize the width to fit its content.

### FixedColumnWidth

```csharp
public int FixedColumnWidth { get; set; }
```

引擎名：`fixed_column_width`

The width all columns will be adjusted to.

A value of zero disables the adjustment, each item will have a width equal to the width of its content and the columns will have an uneven width.

### FixedIconSize

```csharp
public Vector2I FixedIconSize { get; set; }
```

引擎名：`fixed_icon_size`

The size all icons will be adjusted to.

If either X or Y component is not greater than zero, icon size won't be affected.

### IconMode

```csharp
public ItemList.IconModeEnum IconMode { get; set; }
```

引擎名：`icon_mode`

The icon position, whether above or to the left of the text. See the [`ItemList.IconModeEnum`](ItemList.md) constants.

### IconScale

```csharp
public float IconScale { get; set; }
```

引擎名：`icon_scale`

The scale of icon applied after `FixedIconSize` and transposing takes effect.

### ItemCount

```csharp
public int ItemCount { get; set; }
```

引擎名：`item_count`

The number of items currently in the list.

### MaxColumns

```csharp
public int MaxColumns { get; set; }
```

引擎名：`max_columns`

Maximum columns the list will have.

If greater than zero, the content will be split among the specified columns.

A value of zero means unlimited columns, i.e. all items will be put in the same row.

### MaxTextLines

```csharp
public int MaxTextLines { get; set; }
```

引擎名：`max_text_lines`

Maximum lines of text allowed in each item. Space will be reserved even when there is not enough lines of text to display.

**Note:** This property takes effect only when `IconMode` is [`ItemList.IconModeEnum.Top`](ItemList.md). To make the text wrap, `FixedColumnWidth` should be greater than zero.

### SameColumnWidth

```csharp
public bool SameColumnWidth { get; set; }
```

引擎名：`same_column_width`

Whether all columns will have the same width.

If `true`, the width is equal to the largest column width of all columns.

### ScrollHintMode

```csharp
public ItemList.ScrollHintModeEnum ScrollHintMode { get; set; }
```

引擎名：`scroll_hint_mode`

The way which scroll hints (indicators that show that the content can still be scrolled in a certain direction) will be shown.

### SelectMode

```csharp
public ItemList.SelectModeEnum SelectMode { get; set; }
```

引擎名：`select_mode`

Allows single or multiple item selection. See the [`ItemList.SelectModeEnum`](ItemList.md) constants.

### TextOverrunBehavior

```csharp
public TextServer.OverrunBehavior TextOverrunBehavior { get; set; }
```

引擎名：`text_overrun_behavior`

The clipping behavior when the text exceeds an item's bounding rectangle.

### TileScrollHint

```csharp
public bool TileScrollHint { get; set; }
```

引擎名：`tile_scroll_hint`

If `true`, the scroll hint texture will be tiled instead of stretched. See `ScrollHintMode`.

### WraparoundItems

```csharp
public bool WraparoundItems { get; set; }
```

引擎名：`wraparound_items`

If `true`, the control will automatically move items into a new row to fit its content. See also [`HFlowContainer`](HFlowContainer.md) for this behavior.

If `false`, the control will add a horizontal scrollbar to make all items visible.

## 方法

### AddIconItem

```csharp
public int AddIconItem(Texture2D icon, bool selectable = true)
```

引擎名：`add_icon_item`

Adds an item to the item list with no text, only an icon. Returns the index of an added item.

### AddItem

```csharp
public int AddItem(string text, Texture2D icon = null, bool selectable = true)
```

引擎名：`add_item`

Adds an item to the item list with specified text. Returns the index of an added item.

Specify an `icon`, or use `null` as the `icon` for a list item with no icon.

If `selectable` is `true`, the list item will be selectable.

### CenterOnCurrent

```csharp
public void CenterOnCurrent(bool centerVerically = true, bool centerHorizontally = true)
```

引擎名：`center_on_current`

Ensures the currently selected item (the first selected item if multiple selection is enabled) is visible, adjusting the scroll position as necessary to place the item at the center of the list if possible. See also `EnsureCurrentIsVisible()`.

Fails and prints an error if both arguments are `false`.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Removes all items from the list.

### Deselect

```csharp
public void Deselect(int idx)
```

引擎名：`deselect`

Ensures the item associated with the specified index is not selected.

### DeselectAll

```csharp
public void DeselectAll()
```

引擎名：`deselect_all`

Ensures there are no items selected.

### EnsureCurrentIsVisible

```csharp
public void EnsureCurrentIsVisible()
```

引擎名：`ensure_current_is_visible`

Ensures the currently selected item (the first selected item if multiple selection is enabled) is visible, adjusting the scroll position as necessary. See also `CenterOnCurrent()`.

### ForceUpdateListSize

```csharp
public void ForceUpdateListSize()
```

引擎名：`force_update_list_size`

Forces an update to the list size based on its items. This happens automatically whenever size of the items, or other relevant settings like `AutoHeight`, change. The method can be used to trigger the update ahead of next drawing pass.

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

### GetFixedColumnWidth

```csharp
public int GetFixedColumnWidth()
```

引擎名：`get_fixed_column_width`

### GetFixedIconSize

```csharp
public Vector2I GetFixedIconSize()
```

引擎名：`get_fixed_icon_size`

### GetHScrollBar

```csharp
public HScrollBar GetHScrollBar()
```

引擎名：`get_h_scroll_bar`

Returns the horizontal scrollbar.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`CanvasItem.Visible`](CanvasItem.md) property.

### GetIconMode

```csharp
public ItemList.IconModeEnum GetIconMode()
```

引擎名：`get_icon_mode`

### GetIconScale

```csharp
public float GetIconScale()
```

引擎名：`get_icon_scale`

### GetItemAtPosition

```csharp
public int GetItemAtPosition(Vector2 position, bool exact = false)
```

引擎名：`get_item_at_position`

Returns the item index at the given `position`.

When there is no item at that point, -1 will be returned if `exact` is `true`, and the closest item index will be returned otherwise.

**Note:** The returned value is unreliable if called right after modifying the `ItemList`, before it redraws in the next frame.

### GetItemAutoTranslateMode

```csharp
public Node.AutoTranslateModeEnum GetItemAutoTranslateMode(int idx)
```

引擎名：`get_item_auto_translate_mode`

Returns item's auto translate mode.

### GetItemCount

```csharp
public int GetItemCount()
```

引擎名：`get_item_count`

### GetItemCustomBgColor

```csharp
public Color GetItemCustomBgColor(int idx)
```

引擎名：`get_item_custom_bg_color`

Returns the custom background color of the item specified by `idx` index.

### GetItemCustomFgColor

```csharp
public Color GetItemCustomFgColor(int idx)
```

引擎名：`get_item_custom_fg_color`

Returns the custom foreground color of the item specified by `idx` index.

### GetItemIcon

```csharp
public Texture2D GetItemIcon(int idx)
```

引擎名：`get_item_icon`

Returns the icon associated with the specified index.

### GetItemIconModulate

```csharp
public Color GetItemIconModulate(int idx)
```

引擎名：`get_item_icon_modulate`

Returns a [`Color`](Color.md) modulating item's icon at the specified index.

### GetItemIconRegion

```csharp
public Rect2 GetItemIconRegion(int idx)
```

引擎名：`get_item_icon_region`

Returns the region of item's icon used. The whole icon will be used if the region has no area.

### GetItemLanguage

```csharp
public string GetItemLanguage(int idx)
```

引擎名：`get_item_language`

Returns item's text language code.

### GetItemMetadata

```csharp
public Variant GetItemMetadata(int idx)
```

引擎名：`get_item_metadata`

Returns the metadata value of the specified index.

### GetItemRect

```csharp
public Rect2 GetItemRect(int idx, bool expand = true)
```

引擎名：`get_item_rect`

Returns the position and size of the item with the specified index, in the coordinate system of the `ItemList` node. If `expand` is `true` the last column expands to fill the rest of the row.

**Note:** The returned value is unreliable if called right after modifying the `ItemList`, before it redraws in the next frame.

### GetItemText

```csharp
public string GetItemText(int idx)
```

引擎名：`get_item_text`

Returns the text associated with the specified index.

### GetItemTextDirection

```csharp
public Control.TextDirection GetItemTextDirection(int idx)
```

引擎名：`get_item_text_direction`

Returns item's text base writing direction.

### GetItemTooltip

```csharp
public string GetItemTooltip(int idx)
```

引擎名：`get_item_tooltip`

Returns the tooltip hint associated with the specified index.

### GetMaxColumns

```csharp
public int GetMaxColumns()
```

引擎名：`get_max_columns`

### GetMaxTextLines

```csharp
public int GetMaxTextLines()
```

引擎名：`get_max_text_lines`

### GetScrollHintMode

```csharp
public ItemList.ScrollHintModeEnum GetScrollHintMode()
```

引擎名：`get_scroll_hint_mode`

### GetSelectMode

```csharp
public ItemList.SelectModeEnum GetSelectMode()
```

引擎名：`get_select_mode`

### GetSelectedItems

```csharp
public int[] GetSelectedItems()
```

引擎名：`get_selected_items`

Returns an array with the indexes of the selected items.

### GetTextOverrunBehavior

```csharp
public TextServer.OverrunBehavior GetTextOverrunBehavior()
```

引擎名：`get_text_overrun_behavior`

### GetVScrollBar

```csharp
public VScrollBar GetVScrollBar()
```

引擎名：`get_v_scroll_bar`

Returns the vertical scrollbar.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`CanvasItem.Visible`](CanvasItem.md) property.

### HasAutoHeight

```csharp
public bool HasAutoHeight()
```

引擎名：`has_auto_height`

### HasAutoWidth

```csharp
public bool HasAutoWidth()
```

引擎名：`has_auto_width`

### HasWraparoundItems

```csharp
public bool HasWraparoundItems()
```

引擎名：`has_wraparound_items`

### IsAnythingSelected

```csharp
public bool IsAnythingSelected()
```

引擎名：`is_anything_selected`

Returns `true` if one or more items are selected.

### IsItemDisabled

```csharp
public bool IsItemDisabled(int idx)
```

引擎名：`is_item_disabled`

Returns `true` if the item at the specified index is disabled.

### IsItemIconTransposed

```csharp
public bool IsItemIconTransposed(int idx)
```

引擎名：`is_item_icon_transposed`

Returns `true` if the item icon will be drawn transposed, i.e. the X and Y axes are swapped.

### IsItemSelectable

```csharp
public bool IsItemSelectable(int idx)
```

引擎名：`is_item_selectable`

Returns `true` if the item at the specified index is selectable.

### IsItemTooltipEnabled

```csharp
public bool IsItemTooltipEnabled(int idx)
```

引擎名：`is_item_tooltip_enabled`

Returns `true` if the tooltip is enabled for specified item index.

### IsSameColumnWidth

```csharp
public bool IsSameColumnWidth()
```

引擎名：`is_same_column_width`

### IsScrollHintTiled

```csharp
public bool IsScrollHintTiled()
```

引擎名：`is_scroll_hint_tiled`

### IsSelected

```csharp
public bool IsSelected(int idx)
```

引擎名：`is_selected`

Returns `true` if the item at the specified index is currently selected.

### MoveItem

```csharp
public void MoveItem(int fromIdx, int toIdx)
```

引擎名：`move_item`

Moves item from index `fromIdx` to `toIdx`.

### RemoveItem

```csharp
public void RemoveItem(int idx)
```

引擎名：`remove_item`

Removes the item specified by `idx` index from the list.

### Select

```csharp
public void Select(int idx, bool single = true)
```

引擎名：`select`

Selects the item at the specified index.

**Note:** This method does not trigger the item selection signal.

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

### SetAutoHeight

```csharp
public void SetAutoHeight(bool enable)
```

引擎名：`set_auto_height`

### SetAutoWidth

```csharp
public void SetAutoWidth(bool enable)
```

引擎名：`set_auto_width`

### SetFixedColumnWidth

```csharp
public void SetFixedColumnWidth(int width)
```

引擎名：`set_fixed_column_width`

### SetFixedIconSize

```csharp
public void SetFixedIconSize(Vector2I size)
```

引擎名：`set_fixed_icon_size`

### SetIconMode

```csharp
public void SetIconMode(ItemList.IconModeEnum mode)
```

引擎名：`set_icon_mode`

### SetIconScale

```csharp
public void SetIconScale(float scale)
```

引擎名：`set_icon_scale`

### SetItemAutoTranslateMode

```csharp
public void SetItemAutoTranslateMode(int idx, Node.AutoTranslateModeEnum mode)
```

引擎名：`set_item_auto_translate_mode`

Sets the auto translate mode of the item associated with the specified index.

Items use [`Node.AutoTranslateModeEnum.Inherit`](Node.md) by default, which uses the same auto translate mode as the `ItemList` itself.

### SetItemCount

```csharp
public void SetItemCount(int count)
```

引擎名：`set_item_count`

### SetItemCustomBgColor

```csharp
public void SetItemCustomBgColor(int idx, Color customBgColor)
```

引擎名：`set_item_custom_bg_color`

Sets the background color of the item specified by `idx` index to the specified [`Color`](Color.md).

### SetItemCustomFgColor

```csharp
public void SetItemCustomFgColor(int idx, Color customFgColor)
```

引擎名：`set_item_custom_fg_color`

Sets the foreground color of the item specified by `idx` index to the specified [`Color`](Color.md).

### SetItemDisabled

```csharp
public void SetItemDisabled(int idx, bool disabled)
```

引擎名：`set_item_disabled`

Disables (or enables) the item at the specified index.

Disabled items cannot be selected and do not trigger activation signals (when double-clicking or pressing Enter).

### SetItemIcon

```csharp
public void SetItemIcon(int idx, Texture2D icon)
```

引擎名：`set_item_icon`

Sets (or replaces) the icon's [`Texture2D`](Texture2D.md) associated with the specified index.

### SetItemIconModulate

```csharp
public void SetItemIconModulate(int idx, Color modulate)
```

引擎名：`set_item_icon_modulate`

Sets a modulating [`Color`](Color.md) of the item associated with the specified index.

### SetItemIconRegion

```csharp
public void SetItemIconRegion(int idx, Rect2 rect)
```

引擎名：`set_item_icon_region`

Sets the region of item's icon used. The whole icon will be used if the region has no area.

### SetItemIconTransposed

```csharp
public void SetItemIconTransposed(int idx, bool transposed)
```

引擎名：`set_item_icon_transposed`

Sets whether the item icon will be drawn transposed.

### SetItemLanguage

```csharp
public void SetItemLanguage(int idx, string language)
```

引擎名：`set_item_language`

Sets the language code of the text for the item at the given index to `language`. This is used for line-breaking and text shaping algorithms. If `language` is empty, the current locale is used.

### SetItemMetadata

```csharp
public void SetItemMetadata(int idx, Variant metadata)
```

引擎名：`set_item_metadata`

Sets a value (of any type) to be stored with the item associated with the specified index.

### SetItemSelectable

```csharp
public void SetItemSelectable(int idx, bool selectable)
```

引擎名：`set_item_selectable`

Allows or disallows selection of the item associated with the specified index.

### SetItemText

```csharp
public void SetItemText(int idx, string text)
```

引擎名：`set_item_text`

Sets text of the item associated with the specified index.

### SetItemTextDirection

```csharp
public void SetItemTextDirection(int idx, Control.TextDirection direction)
```

引擎名：`set_item_text_direction`

Sets item's text base writing direction.

### SetItemTooltip

```csharp
public void SetItemTooltip(int idx, string tooltip)
```

引擎名：`set_item_tooltip`

Sets the tooltip hint for the item associated with the specified index.

### SetItemTooltipEnabled

```csharp
public void SetItemTooltipEnabled(int idx, bool enable)
```

引擎名：`set_item_tooltip_enabled`

Sets whether the tooltip hint is enabled for specified item index.

### SetMaxColumns

```csharp
public void SetMaxColumns(int amount)
```

引擎名：`set_max_columns`

### SetMaxTextLines

```csharp
public void SetMaxTextLines(int lines)
```

引擎名：`set_max_text_lines`

### SetSameColumnWidth

```csharp
public void SetSameColumnWidth(bool enable)
```

引擎名：`set_same_column_width`

### SetScrollHintMode

```csharp
public void SetScrollHintMode(ItemList.ScrollHintModeEnum scrollHintMode)
```

引擎名：`set_scroll_hint_mode`

### SetSelectMode

```csharp
public void SetSelectMode(ItemList.SelectModeEnum mode)
```

引擎名：`set_select_mode`

### SetTextOverrunBehavior

```csharp
public void SetTextOverrunBehavior(TextServer.OverrunBehavior overrunBehavior)
```

引擎名：`set_text_overrun_behavior`

### SetTileScrollHint

```csharp
public void SetTileScrollHint(bool tileScrollHint)
```

引擎名：`set_tile_scroll_hint`

### SetWraparoundItems

```csharp
public void SetWraparoundItems(bool enable)
```

引擎名：`set_wraparound_items`

### SortItemsByText

```csharp
public void SortItemsByText()
```

引擎名：`sort_items_by_text`

Sorts items in the list by their text.

## 事件（信号）

### EmptyClicked

```csharp
public event ItemList.EmptyClickedEventHandler EmptyClicked
```

引擎名：`empty_clicked`

Emitted when any mouse click is issued within the rect of the list but on empty space.

`atPosition` is the click position in this control's local coordinate system.

### ItemActivated

```csharp
public event ItemList.ItemActivatedEventHandler ItemActivated
```

引擎名：`item_activated`

Emitted when specified list item is activated via double-clicking or by pressing Enter.

### ItemClicked

```csharp
public event ItemList.ItemClickedEventHandler ItemClicked
```

引擎名：`item_clicked`

Emitted when specified list item has been clicked with any mouse button.

`atPosition` is the click position in this control's local coordinate system.

### ItemSelected

```csharp
public event ItemList.ItemSelectedEventHandler ItemSelected
```

引擎名：`item_selected`

Emitted when specified item has been selected. Only applicable in single selection mode.

`AllowReselect` must be enabled to reselect an item.

### MultiSelected

```csharp
public event ItemList.MultiSelectedEventHandler MultiSelected
```

引擎名：`multi_selected`

Emitted when a multiple selection is altered on a list allowing multiple selection.

## EmptyClickedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`ItemList.EmptyClicked`](ItemList.md) event of a [`ItemList`](ItemList.md) class.

### 签名

#### Invoke

```csharp
delegate void EmptyClickedEventHandler(Vector2 atPosition, long mouseButtonIndex)
```

## IconModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Top = 0`：Icon is drawn above the text.
- `Left = 1`：Icon is drawn to the left of the text.

## ItemActivatedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`ItemList.ItemActivated`](ItemList.md) event of a [`ItemList`](ItemList.md) class.

### 签名

#### Invoke

```csharp
delegate void ItemActivatedEventHandler(long index)
```

## ItemClickedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`ItemList.ItemClicked`](ItemList.md) event of a [`ItemList`](ItemList.md) class.

### 签名

#### Invoke

```csharp
delegate void ItemClickedEventHandler(long index, Vector2 atPosition, long mouseButtonIndex)
```

## ItemSelectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`ItemList.ItemSelected`](ItemList.md) event of a [`ItemList`](ItemList.md) class.

### 签名

#### Invoke

```csharp
delegate void ItemSelectedEventHandler(long index)
```

## MultiSelectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`ItemList.MultiSelected`](ItemList.md) event of a [`ItemList`](ItemList.md) class.

### 签名

#### Invoke

```csharp
delegate void MultiSelectedEventHandler(long index, bool selected)
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

- `Single = 0`：Only allow selecting a single item.
- `Multi = 1`：Allows selecting multiple items by holding Ctrl or Shift.
- `Toggle = 2`：Allows selecting multiple items by toggling them on and off.
