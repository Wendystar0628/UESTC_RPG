<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OptionButton

命名空间 `Godot`；class；继承 [`Button`](Button.md) → [`BaseButton`](BaseButton.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`OptionButton` is a type of button that brings up a dropdown with selectable items when pressed. The item selected becomes the "current" item and is displayed as the button text.

See also [`BaseButton`](BaseButton.md) which contains common properties and methods associated with this node.

**Note:** The IDs used for items are limited to signed 32-bit integers, not the full 64 bits of `System.Int32`. These have a range of `-2^31` to `2^31 - 1`, that is, `-2147483648` to `2147483647`.

**Note:** The [`Button.Text`](Button.md) and [`Button.Icon`](Button.md) properties are set automatically based on the selected item. They shouldn't be changed manually.

## 构造函数

### OptionButton

```csharp
public OptionButton()
```

## 属性

### AllowReselect

```csharp
public bool AllowReselect { get; set; }
```

引擎名：`allow_reselect`

If `true`, the currently selected item can be selected again.

### FitToLongestItem

```csharp
public bool FitToLongestItem { get; set; }
```

引擎名：`fit_to_longest_item`

If `true`, minimum size will be determined by the longest item's width, instead of the currently selected one's. It will also take the popup's margins into account, making the button match its total width.

**Note:** For performance reasons, the minimum size doesn't update immediately when adding, removing or modifying items.

### ItemCount

```csharp
public int ItemCount { get; set; }
```

引擎名：`item_count`

The number of items to select from.

### SearchBarEnabled

```csharp
public bool SearchBarEnabled { get; set; }
```

引擎名：`search_bar_enabled`

If `true`, shows a search bar at the top of the [`PopupMenu`](PopupMenu.md) for filtering items. See `SearchBarMinItemCount` for dynamically controlling its visibility based on the number of items.

### SearchBarFuzzySearchEnabled

```csharp
public bool SearchBarFuzzySearchEnabled { get; set; }
```

引擎名：`search_bar_fuzzy_search_enabled`

If `true`, enables fuzzy searching in the [`PopupMenu`](PopupMenu.md) search bar. This allows the search results to include items that almost match the search query, as well items that match the individual characters of the search query, but not in sequence.

Use `SearchBarFuzzySearchMaxMisses` to set the maximum number of mismatches allowed in the search results.

### SearchBarFuzzySearchMaxMisses

```csharp
public int SearchBarFuzzySearchMaxMisses { get; set; }
```

引擎名：`search_bar_fuzzy_search_max_misses`

Sets the maximum number of mismatches allowed in each search result when fuzzy searching is enabled for the [`PopupMenu`](PopupMenu.md) search bar. Any item with more mismatches will be hidden from the search results.

### SearchBarMinItemCount

```csharp
public int SearchBarMinItemCount { get; set; }
```

引擎名：`search_bar_min_item_count`

Sets the minimum number of items required for the [`PopupMenu`](PopupMenu.md) search bar to be visible. `SearchBarEnabled` must be `true` for this to have any effect.

### Selected

```csharp
public int Selected { get; set; }
```

引擎名：`selected`

The index of the currently selected item, or `-1` if no item is selected.

## 方法

### AddIconItem

```csharp
public void AddIconItem(Texture2D texture, string label, int id = -1)
```

引擎名：`add_icon_item`

Adds an item, with a `texture` icon, text `label` and (optionally) `id`. If no `id` is passed, the item index will be used as the item's ID. New items are appended at the end.

**Note:** The item will be selected if there are no other items.

### AddItem

```csharp
public void AddItem(string label, int id = -1)
```

引擎名：`add_item`

Adds an item, with text `label` and (optionally) `id`. If no `id` is passed, the item index will be used as the item's ID. New items are appended at the end.

**Note:** The item will be selected if there are no other items.

### AddSeparator

```csharp
public void AddSeparator(string text = "")
```

引擎名：`add_separator`

Adds a separator to the list of items. Separators help to group items, and can optionally be given a `text` header. A separator also gets an index assigned, and is appended at the end of the item list.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears all the items in the `OptionButton`.

### GetAllowReselect

```csharp
public bool GetAllowReselect()
```

引擎名：`get_allow_reselect`

### GetItemAutoTranslateMode

```csharp
public Node.AutoTranslateModeEnum GetItemAutoTranslateMode(int idx)
```

引擎名：`get_item_auto_translate_mode`

Returns the auto translate mode of the item at index `idx`.

### GetItemCount

```csharp
public int GetItemCount()
```

引擎名：`get_item_count`

### GetItemIcon

```csharp
public Texture2D GetItemIcon(int idx)
```

引擎名：`get_item_icon`

Returns the icon of the item at index `idx`.

### GetItemId

```csharp
public int GetItemId(int idx)
```

引擎名：`get_item_id`

Returns the ID of the item at index `idx`.

### GetItemIndex

```csharp
public int GetItemIndex(int id)
```

引擎名：`get_item_index`

Returns the index of the item with the given `id`.

### GetItemMetadata

```csharp
public Variant GetItemMetadata(int idx)
```

引擎名：`get_item_metadata`

Retrieves the metadata of an item. Metadata may be any type and can be used to store extra information about an item, such as an external string ID.

### GetItemText

```csharp
public string GetItemText(int idx)
```

引擎名：`get_item_text`

Returns the text of the item at index `idx`.

### GetItemTooltip

```csharp
public string GetItemTooltip(int idx)
```

引擎名：`get_item_tooltip`

Returns the tooltip of the item at index `idx`.

### GetPopup

```csharp
public PopupMenu GetPopup()
```

引擎名：`get_popup`

Returns the [`PopupMenu`](PopupMenu.md) contained in this button.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`Window.Visible`](Window.md) property.

### GetSearchBarFuzzySearchMaxMisses

```csharp
public int GetSearchBarFuzzySearchMaxMisses()
```

引擎名：`get_search_bar_fuzzy_search_max_misses`

### GetSearchBarMinItemCount

```csharp
public int GetSearchBarMinItemCount()
```

引擎名：`get_search_bar_min_item_count`

### GetSelectableItem

```csharp
public int GetSelectableItem(bool fromLast = false)
```

引擎名：`get_selectable_item`

Returns the index of the first item which is not disabled, or marked as a separator. If `fromLast` is `true`, the items will be searched in reverse order.

Returns `-1` if no item is found.

### GetSelected

```csharp
public int GetSelected()
```

引擎名：`get_selected`

### GetSelectedId

```csharp
public int GetSelectedId()
```

引擎名：`get_selected_id`

Returns the ID of the selected item, or `-1` if no item is selected.

### GetSelectedMetadata

```csharp
public Variant GetSelectedMetadata()
```

引擎名：`get_selected_metadata`

Gets the metadata of the selected item. Metadata for items can be set using `SetItemMetadata()`.

### HasSelectableItems

```csharp
public bool HasSelectableItems()
```

引擎名：`has_selectable_items`

Returns `true` if this button contains at least one item which is not disabled, or marked as a separator.

### IsFitToLongestItem

```csharp
public bool IsFitToLongestItem()
```

引擎名：`is_fit_to_longest_item`

### IsItemDisabled

```csharp
public bool IsItemDisabled(int idx)
```

引擎名：`is_item_disabled`

Returns `true` if the item at index `idx` is disabled.

### IsItemSeparator

```csharp
public bool IsItemSeparator(int idx)
```

引擎名：`is_item_separator`

Returns `true` if the item at index `idx` is marked as a separator.

### IsSearchBarEnabled

```csharp
public bool IsSearchBarEnabled()
```

引擎名：`is_search_bar_enabled`

### IsSearchBarFuzzySearchEnabled

```csharp
public bool IsSearchBarFuzzySearchEnabled()
```

引擎名：`is_search_bar_fuzzy_search_enabled`

### RemoveItem

```csharp
public void RemoveItem(int idx)
```

引擎名：`remove_item`

Removes the item at index `idx`.

### Select

```csharp
public void Select(int idx)
```

引擎名：`select`

Selects an item by index and makes it the current item. This will work even if the item is disabled.

Passing `-1` as the index deselects any currently selected item.

### SetAllowReselect

```csharp
public void SetAllowReselect(bool allow)
```

引擎名：`set_allow_reselect`

### SetDisableShortcuts

```csharp
public void SetDisableShortcuts(bool disabled)
```

引擎名：`set_disable_shortcuts`

If `true`, shortcuts are disabled and cannot be used to trigger the button.

### SetFitToLongestItem

```csharp
public void SetFitToLongestItem(bool fit)
```

引擎名：`set_fit_to_longest_item`

### SetItemAutoTranslateMode

```csharp
public void SetItemAutoTranslateMode(int idx, Node.AutoTranslateModeEnum mode)
```

引擎名：`set_item_auto_translate_mode`

Sets the auto translate mode of the item at index `idx`.

Items use [`Node.AutoTranslateModeEnum.Inherit`](Node.md) by default, which uses the same auto translate mode as the `OptionButton` itself.

### SetItemCount

```csharp
public void SetItemCount(int count)
```

引擎名：`set_item_count`

### SetItemDisabled

```csharp
public void SetItemDisabled(int idx, bool disabled)
```

引擎名：`set_item_disabled`

Sets whether the item at index `idx` is disabled.

Disabled items are drawn differently in the dropdown and are not selectable by the user. If the current selected item is set as disabled, it will remain selected.

### SetItemIcon

```csharp
public void SetItemIcon(int idx, Texture2D texture)
```

引擎名：`set_item_icon`

Sets the icon of the item at index `idx`.

### SetItemId

```csharp
public void SetItemId(int idx, int id)
```

引擎名：`set_item_id`

Sets the ID of the item at index `idx`.

### SetItemMetadata

```csharp
public void SetItemMetadata(int idx, Variant metadata)
```

引擎名：`set_item_metadata`

Sets the metadata of an item. Metadata may be of any type and can be used to store extra information about an item, such as an external string ID.

### SetItemText

```csharp
public void SetItemText(int idx, string text)
```

引擎名：`set_item_text`

Sets the text of the item at index `idx`.

### SetItemTooltip

```csharp
public void SetItemTooltip(int idx, string tooltip)
```

引擎名：`set_item_tooltip`

Sets the tooltip of the item at index `idx`.

### SetSearchBarEnabled

```csharp
public void SetSearchBarEnabled(bool enabled)
```

引擎名：`set_search_bar_enabled`

### SetSearchBarFuzzySearchEnabled

```csharp
public void SetSearchBarFuzzySearchEnabled(bool enabled)
```

引擎名：`set_search_bar_fuzzy_search_enabled`

### SetSearchBarFuzzySearchMaxMisses

```csharp
public void SetSearchBarFuzzySearchMaxMisses(int maxMisses)
```

引擎名：`set_search_bar_fuzzy_search_max_misses`

### SetSearchBarMinItemCount

```csharp
public void SetSearchBarMinItemCount(int count)
```

引擎名：`set_search_bar_min_item_count`

### ShowPopup

```csharp
public void ShowPopup()
```

引擎名：`show_popup`

Adjusts popup position and sizing for the `OptionButton`, then shows the [`PopupMenu`](PopupMenu.md). Prefer this over using `get_popup().popup()`.

## 事件（信号）

### ItemFocused

```csharp
public event OptionButton.ItemFocusedEventHandler ItemFocused
```

引擎名：`item_focused`

Emitted when the user navigates to an item using the `ProjectSettings.input/ui_up` or `ProjectSettings.input/ui_down` input actions. The index of the item focused is passed as argument.

### ItemSelected

```csharp
public event OptionButton.ItemSelectedEventHandler ItemSelected
```

引擎名：`item_selected`

Emitted when the current item has been changed by the user. The index of the item selected is passed as argument.

`AllowReselect` must be enabled to reselect an item.

## ItemFocusedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OptionButton.ItemFocused`](OptionButton.md) event of a [`OptionButton`](OptionButton.md) class.

### 签名

#### Invoke

```csharp
delegate void ItemFocusedEventHandler(long index)
```

## ItemSelectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OptionButton.ItemSelected`](OptionButton.md) event of a [`OptionButton`](OptionButton.md) class.

### 签名

#### Invoke

```csharp
delegate void ItemSelectedEventHandler(long index)
```
