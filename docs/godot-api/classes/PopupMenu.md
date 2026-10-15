<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PopupMenu

命名空间 `Godot`；class；继承 [`Popup`](Popup.md) → [`Window`](Window.md) → [`Viewport`](Viewport.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`PopupMenu` is a modal window used to display a list of options. Useful for toolbars and context menus.

The size of a `PopupMenu` can be limited by using [`Window.MaxSize`](Window.md). If the height of the list of items is larger than the maximum height of the `PopupMenu`, a [`ScrollContainer`](ScrollContainer.md) within the popup will allow the user to scroll the contents. If no maximum size is set, or if it is set to `0`, the `PopupMenu` height will be limited by its parent rect.

All `set_*` methods allow negative item indices, i.e. `-1` to access the last item, `-2` to select the second-to-last item, and so on.

**Incremental search:** Like [`ItemList`](ItemList.md) and [`Tree`](Tree.md), `PopupMenu` supports searching within the list while the control is focused. Press a key that matches the first letter of an item's name to select the first item starting with the given letter. After that point, there are two ways to perform incremental search: 1) Press the same key again before the timeout duration to select the next item starting with the same letter. 2) Press letter keys that match the rest of the word before the timeout duration to match to select the item in question directly. Both of these actions will be reset to the beginning of the list if the timeout duration has passed since the last keystroke was registered. You can adjust the timeout duration by changing `ProjectSettings.gui/timers/incremental_search_max_interval_msec`.

**Note:** `PopupMenu` is invisible by default. To make it visible, call one of the `popup_*` methods from [`Window`](Window.md) on the node, such as [`Window.PopupCenteredClamped`](Window.md).

**Note:** The ID values used for items are limited to 32 bits, not full 64 bits of `System.Int32`. This has a range of `-2^32` to `2^32 - 1`, i.e. `-2147483648` to `2147483647`.

## 构造函数

### PopupMenu

```csharp
public PopupMenu()
```

## 属性

### AllowSearch

```csharp
public bool AllowSearch { get; set; }
```

引擎名：`allow_search`

If `true`, allows navigating `PopupMenu` with letter keys.

### HideOnCheckableItemSelection

```csharp
public bool HideOnCheckableItemSelection { get; set; }
```

引擎名：`hide_on_checkable_item_selection`

If `true`, hides the `PopupMenu` when a checkbox or radio button is selected.

### HideOnItemSelection

```csharp
public bool HideOnItemSelection { get; set; }
```

引擎名：`hide_on_item_selection`

If `true`, hides the `PopupMenu` when an item is selected.

### HideOnStateItemSelection

```csharp
public bool HideOnStateItemSelection { get; set; }
```

引擎名：`hide_on_state_item_selection`

If `true`, hides the `PopupMenu` when a state item is selected.

### ItemCount

```csharp
public int ItemCount { get; set; }
```

引擎名：`item_count`

The number of items currently in the list.

### PreferNativeMenu

```csharp
public bool PreferNativeMenu { get; set; }
```

引擎名：`prefer_native_menu`

If `true`, [`MenuBar`](MenuBar.md) will use native menu when supported.

**Note:** If `PopupMenu` is linked to [`StatusIndicator`](StatusIndicator.md), [`MenuBar`](MenuBar.md), or another `PopupMenu` item it can use native menu regardless of this property, use `IsNativeMenu()` to check it.

### SearchBarEnabled

```csharp
public bool SearchBarEnabled { get; set; }
```

引擎名：`search_bar_enabled`

If `true`, shows a search bar at the top of the `PopupMenu` for filtering items. See `SearchBarMinItemCount` for dynamically controlling its visibility based on the number of items.

**Note:** When enabled, `AllowSearch` is ignored.

### SearchBarFuzzySearchEnabled

```csharp
public bool SearchBarFuzzySearchEnabled { get; set; }
```

引擎名：`search_bar_fuzzy_search_enabled`

If `true`, enables fuzzy searching in the `PopupMenu` search bar. This allows the search results to include items that almost match the search query, as well items that match the individual characters of the search query, but not in sequence.

Use `SearchBarFuzzySearchMaxMisses` to set the maximum number of mismatches allowed in the search results.

### SearchBarFuzzySearchMaxMisses

```csharp
public int SearchBarFuzzySearchMaxMisses { get; set; }
```

引擎名：`search_bar_fuzzy_search_max_misses`

Sets the maximum number of mismatches allowed in each search result when fuzzy searching is enabled for the `PopupMenu` search bar. Any item with more mismatches will be hidden from the search results.

### SearchBarMinItemCount

```csharp
public int SearchBarMinItemCount { get; set; }
```

引擎名：`search_bar_min_item_count`

Sets the minimum number of items required for the search bar to be visible. `SearchBarEnabled` must be `true` for this to have any effect. Separator items are not counted.

### ShrinkHeight

```csharp
public bool ShrinkHeight { get; set; }
```

引擎名：`shrink_height`

If `true`, shrinks `PopupMenu` to minimum height when it's shown.

### ShrinkWidth

```csharp
public bool ShrinkWidth { get; set; }
```

引擎名：`shrink_width`

If `true`, shrinks `PopupMenu` to minimum width when it's shown.

### SubmenuPopupDelay

```csharp
public float SubmenuPopupDelay { get; set; }
```

引擎名：`submenu_popup_delay`

Sets the delay time in seconds for the submenu item to popup on mouse hovering. If the popup menu is added as a child of another (acting as a submenu), it will inherit the delay time of the parent menu item.

**Note:** If the mouse is exiting a submenu item with an open submenu and enters a different submenu item, the submenu popup delay time is affected by the direction of the mouse movement toward the open submenu. If the mouse is moving toward the submenu, the open submenu will wait approximately `0.5` seconds before closing, which then allows the hovered submenu item to open. This additional delay allows the mouse time to move to the open submenu across other menu items without prematurely closing. If the mouse is not moving toward the open submenu, for example in a downward direction, the open submenu will close immediately.

### SystemMenuId

```csharp
public NativeMenu.SystemMenus SystemMenuId { get; set; }
```

引擎名：`system_menu_id`

If set to one of the values of [`NativeMenu.SystemMenus`](NativeMenu.md), this `PopupMenu` is bound to the special system menu. Only one `PopupMenu` can be bound to each special menu at a time.

## 方法

### ActivateItemByEvent

```csharp
public bool ActivateItemByEvent(InputEvent event, bool forGlobalOnly = false)
```

引擎名：`activate_item_by_event`

Checks the provided `event` against the `PopupMenu`'s shortcuts and accelerators, and activates the first item with matching events. If `forGlobalOnly` is `true`, only shortcuts and accelerators with `global` set to `true` will be called.

Returns `true` if an item was successfully activated.

**Note:** Certain [`Control`](Control.md)s, such as [`MenuButton`](MenuButton.md), will call this method automatically.

### AddCheckItem

```csharp
public void AddCheckItem(string label, int id = -1, Key accel = Key.None)
```

引擎名：`add_check_item`

Adds a new checkable item with text `label`.

An `id` can optionally be provided, as well as an accelerator (`accel`). If no `id` is provided, one will be created from the index. If no `accel` is provided, then the default value of 0 (corresponding to [`Key.None`](Key.md)) will be assigned to the item (which means it won't have any accelerator). See `GetItemAccelerator()` for more info on accelerators.

**Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See `SetItemChecked()` for more info on how to control it.

### AddCheckShortcut

```csharp
public void AddCheckShortcut(Shortcut shortcut, int id = -1, bool global = false)
```

引擎名：`add_check_shortcut`

Adds a new checkable item and assigns the specified [`Shortcut`](Shortcut.md) to it. Sets the label of the checkbox to the [`Shortcut`](Shortcut.md)'s name.

An `id` can optionally be provided. If no `id` is provided, one will be created from the index.

**Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See `SetItemChecked()` for more info on how to control it.

### AddIconCheckItem

```csharp
public void AddIconCheckItem(Texture2D texture, string label, int id = -1, Key accel = Key.None)
```

引擎名：`add_icon_check_item`

Adds a new checkable item with text `label` and icon `texture`.

An `id` can optionally be provided, as well as an accelerator (`accel`). If no `id` is provided, one will be created from the index. If no `accel` is provided, then the default value of 0 (corresponding to [`Key.None`](Key.md)) will be assigned to the item (which means it won't have any accelerator). See `GetItemAccelerator()` for more info on accelerators.

**Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See `SetItemChecked()` for more info on how to control it.

### AddIconCheckShortcut

```csharp
public void AddIconCheckShortcut(Texture2D texture, Shortcut shortcut, int id = -1, bool global = false)
```

引擎名：`add_icon_check_shortcut`

Adds a new checkable item and assigns the specified [`Shortcut`](Shortcut.md) and icon `texture` to it. Sets the label of the checkbox to the [`Shortcut`](Shortcut.md)'s name.

An `id` can optionally be provided. If no `id` is provided, one will be created from the index.

**Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See `SetItemChecked()` for more info on how to control it.

### AddIconItem

```csharp
public void AddIconItem(Texture2D texture, string label, int id = -1, Key accel = Key.None)
```

引擎名：`add_icon_item`

Adds a new item with text `label` and icon `texture`.

An `id` can optionally be provided, as well as an accelerator (`accel`). If no `id` is provided, one will be created from the index. If no `accel` is provided, then the default value of 0 (corresponding to [`Key.None`](Key.md)) will be assigned to the item (which means it won't have any accelerator). See `GetItemAccelerator()` for more info on accelerators.

### AddIconRadioCheckItem

```csharp
public void AddIconRadioCheckItem(Texture2D texture, string label, int id = -1, Key accel = Key.None)
```

引擎名：`add_icon_radio_check_item`

Same as `AddIconCheckItem()`, but uses a radio check button.

### AddIconRadioCheckShortcut

```csharp
public void AddIconRadioCheckShortcut(Texture2D texture, Shortcut shortcut, int id = -1, bool global = false)
```

引擎名：`add_icon_radio_check_shortcut`

Same as `AddIconCheckShortcut()`, but uses a radio check button.

### AddIconShortcut

```csharp
public void AddIconShortcut(Texture2D texture, Shortcut shortcut, int id = -1, bool global = false, bool allowEcho = false)
```

引擎名：`add_icon_shortcut`

Adds a new item and assigns the specified [`Shortcut`](Shortcut.md) and icon `texture` to it. Sets the label of the checkbox to the [`Shortcut`](Shortcut.md)'s name.

An `id` can optionally be provided. If no `id` is provided, one will be created from the index.

If `allowEcho` is `true`, the shortcut can be activated with echo events.

### AddIconShortcut

```csharp
public void AddIconShortcut(Texture2D texture, Shortcut shortcut, int id, bool global)
```

引擎名：`add_icon_shortcut`

Adds a new item and assigns the specified [`Shortcut`](Shortcut.md) and icon `texture` to it. Sets the label of the checkbox to the [`Shortcut`](Shortcut.md)'s name.

An `id` can optionally be provided. If no `id` is provided, one will be created from the index.

If `allowEcho` is `true`, the shortcut can be activated with echo events.

### AddItem

```csharp
public void AddItem(string label, int id = -1, Key accel = Key.None)
```

引擎名：`add_item`

Adds a new item with text `label`.

An `id` can optionally be provided, as well as an accelerator (`accel`). If no `id` is provided, one will be created from the index. If no `accel` is provided, then the default value of 0 (corresponding to [`Key.None`](Key.md)) will be assigned to the item (which means it won't have any accelerator). See `GetItemAccelerator()` for more info on accelerators.

**Note:** The provided `id` is used only in `IdPressed` and `IdFocused` signals. It's not related to the `index` arguments in e.g. `SetItemChecked()`.

### AddMultistateItem

```csharp
public void AddMultistateItem(string label, int maxStates, int defaultState = 0, int id = -1, Key accel = Key.None)
```

引擎名：`add_multistate_item`

Adds a new multistate item with text `label`.

Contrarily to normal binary items, multistate items can have more than two states, as defined by `maxStates`. The default value is defined by `defaultState`.

An `id` can optionally be provided, as well as an accelerator (`accel`). If no `id` is provided, one will be created from the index. If no `accel` is provided, then the default value of 0 (corresponding to [`Key.None`](Key.md)) will be assigned to the item (which means it won't have any accelerator). See `GetItemAccelerator()` for more info on accelerators.

```csharp
func _ready():
add_multistate_item("Item", 3, 0)

index_pressed.connect(func(index: int):
toggle_item_multistate(index)
match get_item_multistate(index):
0:
print("First state")
1:
print("Second state")
2:
print("Third state")
)

```

**Note:** Multistate items don't update their state automatically and must be done manually. See `ToggleItemMultistate()`, `SetItemMultistate()` and `GetItemMultistate()` for more info on how to control it.

### AddRadioCheckItem

```csharp
public void AddRadioCheckItem(string label, int id = -1, Key accel = Key.None)
```

引擎名：`add_radio_check_item`

Adds a new radio check button with text `label`.

An `id` can optionally be provided, as well as an accelerator (`accel`). If no `id` is provided, one will be created from the index. If no `accel` is provided, then the default value of 0 (corresponding to [`Key.None`](Key.md)) will be assigned to the item (which means it won't have any accelerator). See `GetItemAccelerator()` for more info on accelerators.

**Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See `SetItemChecked()` for more info on how to control it.

### AddRadioCheckShortcut

```csharp
public void AddRadioCheckShortcut(Shortcut shortcut, int id = -1, bool global = false)
```

引擎名：`add_radio_check_shortcut`

Adds a new radio check button and assigns a [`Shortcut`](Shortcut.md) to it. Sets the label of the checkbox to the [`Shortcut`](Shortcut.md)'s name.

An `id` can optionally be provided. If no `id` is provided, one will be created from the index.

**Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See `SetItemChecked()` for more info on how to control it.

### AddSeparator

```csharp
public void AddSeparator(string label = "", int id = -1)
```

引擎名：`add_separator`

Adds a separator between items. Separators also occupy an index, which you can set by using the `id` parameter.

A `label` can optionally be provided, which will appear at the center of the separator.

### AddShortcut

```csharp
public void AddShortcut(Shortcut shortcut, int id = -1, bool global = false, bool allowEcho = false)
```

引擎名：`add_shortcut`

Adds a [`Shortcut`](Shortcut.md).

An `id` can optionally be provided. If no `id` is provided, one will be created from the index.

If `allowEcho` is `true`, the shortcut can be activated with echo events.

### AddShortcut

```csharp
public void AddShortcut(Shortcut shortcut, int id, bool global)
```

引擎名：`add_shortcut`

Adds a [`Shortcut`](Shortcut.md).

An `id` can optionally be provided. If no `id` is provided, one will be created from the index.

If `allowEcho` is `true`, the shortcut can be activated with echo events.

### AddSubmenuItem

```csharp
public void AddSubmenuItem(string label, string submenu, int id = -1)
```

引擎名：`add_submenu_item`

Adds an item that will act as a submenu of the parent `PopupMenu` node when clicked. The `submenu` argument must be the name of an existing `PopupMenu` that has been added as a child to this node. This submenu will be shown when the item is clicked, hovered for long enough, or activated using the `ui_select` or `ui_right` input actions.

An `id` can optionally be provided. If no `id` is provided, one will be created from the index.

### AddSubmenuNodeItem

```csharp
public void AddSubmenuNodeItem(string label, PopupMenu submenu, int id = -1)
```

引擎名：`add_submenu_node_item`

Adds an item that will act as a submenu of the parent `PopupMenu` node when clicked. This submenu will be shown when the item is clicked, hovered for long enough, or activated using the `ui_select` or `ui_right` input actions.

`submenu` must be either child of this `PopupMenu` or has no parent node (in which case it will be automatically added as a child). If the `submenu` popup has another parent, this method will fail.

An `id` can optionally be provided. If no `id` is provided, one will be created from the index.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Removes all items from the `PopupMenu`. If `freeSubmenus` is `true`, the submenu nodes are automatically freed.

### Clear

```csharp
public void Clear(bool freeSubmenus = false)
```

引擎名：`clear`

Removes all items from the `PopupMenu`. If `freeSubmenus` is `true`, the submenu nodes are automatically freed.

### GetAllowSearch

```csharp
public bool GetAllowSearch()
```

引擎名：`get_allow_search`

### GetFocusedItem

```csharp
public int GetFocusedItem()
```

引擎名：`get_focused_item`

Returns the index of the currently focused item. Returns `-1` if no item is focused.

### GetItemAccelerator

```csharp
public Key GetItemAccelerator(int index)
```

引擎名：`get_item_accelerator`

Returns the accelerator of the item at the given `index`. An accelerator is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The return value is an integer which is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A). If no accelerator is defined for the specified `index`, `GetItemAccelerator()` returns `0` (corresponding to [`Key.None`](Key.md)).

### GetItemAutoTranslateMode

```csharp
public Node.AutoTranslateModeEnum GetItemAutoTranslateMode(int index)
```

引擎名：`get_item_auto_translate_mode`

Returns the auto translate mode of the item at the given `index`.

### GetItemCount

```csharp
public int GetItemCount()
```

引擎名：`get_item_count`

### GetItemIcon

```csharp
public Texture2D GetItemIcon(int index)
```

引擎名：`get_item_icon`

Returns the icon of the item at the given `index`.

### GetItemIconMaxWidth

```csharp
public int GetItemIconMaxWidth(int index)
```

引擎名：`get_item_icon_max_width`

Returns the maximum allowed width of the icon for the item at the given `index`.

### GetItemIconModulate

```csharp
public Color GetItemIconModulate(int index)
```

引擎名：`get_item_icon_modulate`

Returns a [`Color`](Color.md) modulating the item's icon at the given `index`.

### GetItemId

```csharp
public int GetItemId(int index)
```

引擎名：`get_item_id`

Returns the ID of the item at the given `index`.

### GetItemIndent

```csharp
public int GetItemIndent(int index)
```

引擎名：`get_item_indent`

Returns the horizontal offset of the item at the given `index`.

### GetItemIndex

```csharp
public int GetItemIndex(int id)
```

引擎名：`get_item_index`

Returns the index of the item containing the specified `id`. The index is automatically assigned to each item by the engine when added and represents the order items will be displayed.

### GetItemLanguage

```csharp
public string GetItemLanguage(int index)
```

引擎名：`get_item_language`

Returns item's text language code.

### GetItemMetadata

```csharp
public Variant GetItemMetadata(int index)
```

引擎名：`get_item_metadata`

Returns the metadata of the specified item, which might be of any type. You can set it with `SetItemMetadata()`, which provides a simple way of assigning context data to items.

### GetItemMultistate

```csharp
public int GetItemMultistate(int index)
```

引擎名：`get_item_multistate`

Returns the state of the item at the given `index`.

### GetItemMultistateMax

```csharp
public int GetItemMultistateMax(int index)
```

引擎名：`get_item_multistate_max`

Returns the max states of the item at the given `index`.

### GetItemShortcut

```csharp
public Shortcut GetItemShortcut(int index)
```

引擎名：`get_item_shortcut`

Returns the [`Shortcut`](Shortcut.md) associated with the item at the given `index`.

### GetItemSubmenu

```csharp
public string GetItemSubmenu(int index)
```

引擎名：`get_item_submenu`

Returns the submenu name of the item at the given `index`. See `AddSubmenuItem()` for more info on how to add a submenu.

### GetItemSubmenuNode

```csharp
public PopupMenu GetItemSubmenuNode(int index)
```

引擎名：`get_item_submenu_node`

Returns the submenu of the item at the given `index`, or `null` if no submenu was added. See `AddSubmenuNodeItem()` for more info on how to add a submenu.

### GetItemText

```csharp
public string GetItemText(int index)
```

引擎名：`get_item_text`

Returns the text of the item at the given `index`.

### GetItemTextDirection

```csharp
public Control.TextDirection GetItemTextDirection(int index)
```

引擎名：`get_item_text_direction`

Returns item's text base writing direction.

### GetItemTooltip

```csharp
public string GetItemTooltip(int index)
```

引擎名：`get_item_tooltip`

Returns the tooltip associated with the item at the given `index`.

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

### GetShrinkHeight

```csharp
public bool GetShrinkHeight()
```

引擎名：`get_shrink_height`

### GetShrinkWidth

```csharp
public bool GetShrinkWidth()
```

引擎名：`get_shrink_width`

### GetSubmenuPopupDelay

```csharp
public float GetSubmenuPopupDelay()
```

引擎名：`get_submenu_popup_delay`

### GetSystemMenu

```csharp
public NativeMenu.SystemMenus GetSystemMenu()
```

引擎名：`get_system_menu`

### GetSystemMenuRoot

```csharp
public string GetSystemMenuRoot()
```

### IsHideOnCheckableItemSelection

```csharp
public bool IsHideOnCheckableItemSelection()
```

引擎名：`is_hide_on_checkable_item_selection`

### IsHideOnItemSelection

```csharp
public bool IsHideOnItemSelection()
```

引擎名：`is_hide_on_item_selection`

### IsHideOnStateItemSelection

```csharp
public bool IsHideOnStateItemSelection()
```

引擎名：`is_hide_on_state_item_selection`

### IsItemCheckable

```csharp
public bool IsItemCheckable(int index)
```

引擎名：`is_item_checkable`

Returns `true` if the item at the given `index` is checkable in some way, i.e. if it has a checkbox or radio button.

**Note:** Checkable items just display a checkmark or radio button, but don't have any built-in checking behavior and must be checked/unchecked manually.

### IsItemChecked

```csharp
public bool IsItemChecked(int index)
```

引擎名：`is_item_checked`

Returns `true` if the item at the given `index` is checked.

### IsItemDisabled

```csharp
public bool IsItemDisabled(int index)
```

引擎名：`is_item_disabled`

Returns `true` if the item at the given `index` is disabled. When it is disabled it can't be selected, or its action invoked.

See `SetItemDisabled()` for more info on how to disable an item.

### IsItemRadioCheckable

```csharp
public bool IsItemRadioCheckable(int index)
```

引擎名：`is_item_radio_checkable`

Returns `true` if the item at the given `index` has radio button-style checkability.

**Note:** This is purely cosmetic; you must add the logic for checking/unchecking items in radio groups.

### IsItemSeparator

```csharp
public bool IsItemSeparator(int index)
```

引擎名：`is_item_separator`

Returns `true` if the item is a separator. If it is, it will be displayed as a line. See `AddSeparator()` for more info on how to add a separator.

### IsItemShortcutDisabled

```csharp
public bool IsItemShortcutDisabled(int index)
```

引擎名：`is_item_shortcut_disabled`

Returns `true` if the specified item's shortcut is disabled.

### IsNativeMenu

```csharp
public bool IsNativeMenu()
```

引擎名：`is_native_menu`

Returns `true` if the system native menu is supported and currently used by this `PopupMenu`.

### IsPreferNativeMenu

```csharp
public bool IsPreferNativeMenu()
```

引擎名：`is_prefer_native_menu`

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

### IsSystemMenu

```csharp
public bool IsSystemMenu()
```

引擎名：`is_system_menu`

Returns `true` if the menu is bound to the special system menu.

### RemoveItem

```csharp
public void RemoveItem(int index)
```

引擎名：`remove_item`

Removes the item at the given `index` from the menu.

**Note:** The indices of items after the removed item will be shifted by one.

### ScrollToItem

```csharp
public void ScrollToItem(int index)
```

引擎名：`scroll_to_item`

Moves the scroll view to make the item at the given `index` visible.

### SetAllowSearch

```csharp
public void SetAllowSearch(bool allow)
```

引擎名：`set_allow_search`

### SetFocusedItem

```csharp
public void SetFocusedItem(int index)
```

引擎名：`set_focused_item`

Sets the currently focused item as the given `index`.

Passing `-1` as the index makes so that no item is focused.

### SetHideOnCheckableItemSelection

```csharp
public void SetHideOnCheckableItemSelection(bool enable)
```

引擎名：`set_hide_on_checkable_item_selection`

### SetHideOnItemSelection

```csharp
public void SetHideOnItemSelection(bool enable)
```

引擎名：`set_hide_on_item_selection`

### SetHideOnStateItemSelection

```csharp
public void SetHideOnStateItemSelection(bool enable)
```

引擎名：`set_hide_on_state_item_selection`

### SetItemAccelerator

```csharp
public void SetItemAccelerator(int index, Key accel)
```

引擎名：`set_item_accelerator`

Sets the accelerator of the item at the given `index`. An accelerator is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. `accel` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

### SetItemAsCheckable

```csharp
public void SetItemAsCheckable(int index, bool enable)
```

引擎名：`set_item_as_checkable`

Sets whether the item at the given `index` has a checkbox. If `false`, sets the type of the item to plain text.

**Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually.

### SetItemAsRadioCheckable

```csharp
public void SetItemAsRadioCheckable(int index, bool enable)
```

引擎名：`set_item_as_radio_checkable`

Sets the type of the item at the given `index` to radio button. If `false`, sets the type of the item to plain text.

### SetItemAsSeparator

```csharp
public void SetItemAsSeparator(int index, bool enable)
```

引擎名：`set_item_as_separator`

Mark the item at the given `index` as a separator, which means that it would be displayed as a line. If `false`, sets the type of the item to plain text.

### SetItemAutoTranslateMode

```csharp
public void SetItemAutoTranslateMode(int index, Node.AutoTranslateModeEnum mode)
```

引擎名：`set_item_auto_translate_mode`

Sets the auto translate mode of the item at the given `index`.

Items use [`Node.AutoTranslateModeEnum.Inherit`](Node.md) by default, which uses the same auto translate mode as the `PopupMenu` itself.

### SetItemChecked

```csharp
public void SetItemChecked(int index, bool checked)
```

引擎名：`set_item_checked`

Sets the checkstate status of the item at the given `index`.

### SetItemCount

```csharp
public void SetItemCount(int count)
```

引擎名：`set_item_count`

### SetItemDisabled

```csharp
public void SetItemDisabled(int index, bool disabled)
```

引擎名：`set_item_disabled`

Enables/disables the item at the given `index`. When it is disabled, it can't be selected and its action can't be invoked.

### SetItemIcon

```csharp
public void SetItemIcon(int index, Texture2D icon)
```

引擎名：`set_item_icon`

Replaces the [`Texture2D`](Texture2D.md) icon of the item at the given `index`.

### SetItemIconMaxWidth

```csharp
public void SetItemIconMaxWidth(int index, int width)
```

引擎名：`set_item_icon_max_width`

Sets the maximum allowed width of the icon for the item at the given `index`. This limit is applied on top of the default size of the icon and on top of `icon_max_width`. The height is adjusted according to the icon's ratio.

### SetItemIconModulate

```csharp
public void SetItemIconModulate(int index, Color modulate)
```

引擎名：`set_item_icon_modulate`

Sets a modulating [`Color`](Color.md) of the item's icon at the given `index`.

### SetItemId

```csharp
public void SetItemId(int index, int id)
```

引擎名：`set_item_id`

Sets the `id` of the item at the given `index`.

The `id` is used in `IdPressed` and `IdFocused` signals.

### SetItemIndent

```csharp
public void SetItemIndent(int index, int indent)
```

引擎名：`set_item_indent`

Sets the horizontal offset of the item at the given `index`.

### SetItemIndex

```csharp
public void SetItemIndex(int index, int targetIndex)
```

引擎名：`set_item_index`

Changes the index of the item at index `index` to be at index `targetIndex`. This can be used to move an item above other items. The moved item will keep the same ID, even if it was generated from the original index.

**Note:** The indices of any items between index `index` and index `targetIndex` will be shifted by one.

### SetItemLanguage

```csharp
public void SetItemLanguage(int index, string language)
```

引擎名：`set_item_language`

Sets the language code of the text for the item at the given index to `language`. This is used for line-breaking and text shaping algorithms. If `language` is empty, the current locale is used.

### SetItemMetadata

```csharp
public void SetItemMetadata(int index, Variant metadata)
```

引擎名：`set_item_metadata`

Sets the metadata of an item, which may be of any type. You can later get it with `GetItemMetadata()`, which provides a simple way of assigning context data to items.

### SetItemMultistate

```csharp
public void SetItemMultistate(int index, int state)
```

引擎名：`set_item_multistate`

Sets the state of a multistate item. See `AddMultistateItem()` for details.

### SetItemMultistateMax

```csharp
public void SetItemMultistateMax(int index, int maxStates)
```

引擎名：`set_item_multistate_max`

Sets the max states of a multistate item. See `AddMultistateItem()` for details.

### SetItemShortcut

```csharp
public void SetItemShortcut(int index, Shortcut shortcut, bool global = false)
```

引擎名：`set_item_shortcut`

Sets a [`Shortcut`](Shortcut.md) for the item at the given `index`.

### SetItemShortcutDisabled

```csharp
public void SetItemShortcutDisabled(int index, bool disabled)
```

引擎名：`set_item_shortcut_disabled`

Disables the [`Shortcut`](Shortcut.md) of the item at the given `index`.

### SetItemSubmenu

```csharp
public void SetItemSubmenu(int index, string submenu)
```

引擎名：`set_item_submenu`

Sets the submenu of the item at the given `index`. The submenu is the name of a child `PopupMenu` node that would be shown when the item is clicked.

### SetItemSubmenuNode

```csharp
public void SetItemSubmenuNode(int index, PopupMenu submenu)
```

引擎名：`set_item_submenu_node`

Sets the submenu of the item at the given `index`. The submenu is a `PopupMenu` node that would be shown when the item is clicked. It must either be a child of this `PopupMenu` or has no parent (in which case it will be automatically added as a child). If the `submenu` popup has another parent, this method will fail.

### SetItemText

```csharp
public void SetItemText(int index, string text)
```

引擎名：`set_item_text`

Sets the text of the item at the given `index`.

### SetItemTextDirection

```csharp
public void SetItemTextDirection(int index, Control.TextDirection direction)
```

引擎名：`set_item_text_direction`

Sets item's text base writing direction.

### SetItemTooltip

```csharp
public void SetItemTooltip(int index, string tooltip)
```

引擎名：`set_item_tooltip`

Sets the `System.String` tooltip of the item at the given `index`.

### SetPreferNativeMenu

```csharp
public void SetPreferNativeMenu(bool enabled)
```

引擎名：`set_prefer_native_menu`

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

### SetShrinkHeight

```csharp
public void SetShrinkHeight(bool shrink)
```

引擎名：`set_shrink_height`

### SetShrinkWidth

```csharp
public void SetShrinkWidth(bool shrink)
```

引擎名：`set_shrink_width`

### SetSubmenuPopupDelay

```csharp
public void SetSubmenuPopupDelay(float seconds)
```

引擎名：`set_submenu_popup_delay`

### SetSystemMenu

```csharp
public void SetSystemMenu(NativeMenu.SystemMenus systemMenuId)
```

引擎名：`set_system_menu`

### SetSystemMenuRoot

```csharp
public void SetSystemMenuRoot(string special)
```

### ToggleItemChecked

```csharp
public void ToggleItemChecked(int index)
```

引擎名：`toggle_item_checked`

Toggles the check state of the item at the given `index`.

### ToggleItemMultistate

```csharp
public void ToggleItemMultistate(int index)
```

引擎名：`toggle_item_multistate`

Cycle to the next state of a multistate item. See `AddMultistateItem()` for details.

## 事件（信号）

### IdFocused

```csharp
public event PopupMenu.IdFocusedEventHandler IdFocused
```

引擎名：`id_focused`

Emitted when the user navigated to an item of some `id` using the `ProjectSettings.input/ui_up` or `ProjectSettings.input/ui_down` input action.

### IdPressed

```csharp
public event PopupMenu.IdPressedEventHandler IdPressed
```

引擎名：`id_pressed`

Emitted when an item of some `id` is pressed. Also emitted when its accelerator is activated on macOS.

**Note:** If `id` is negative (either explicitly or due to overflow), this will return the corresponding index instead.

### IndexPressed

```csharp
public event PopupMenu.IndexPressedEventHandler IndexPressed
```

引擎名：`index_pressed`

Emitted when an item of some `index` is pressed. Also emitted when its accelerator is activated on macOS.

### MenuChanged

```csharp
public event Action MenuChanged
```

引擎名：`menu_changed`

Emitted when any item is added, modified or removed.

## IdFocusedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`PopupMenu.IdFocused`](PopupMenu.md) event of a [`PopupMenu`](PopupMenu.md) class.

### 签名

#### Invoke

```csharp
delegate void IdFocusedEventHandler(long id)
```

## IdPressedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`PopupMenu.IdPressed`](PopupMenu.md) event of a [`PopupMenu`](PopupMenu.md) class.

### 签名

#### Invoke

```csharp
delegate void IdPressedEventHandler(long id)
```

## IndexPressedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`PopupMenu.IndexPressed`](PopupMenu.md) event of a [`PopupMenu`](PopupMenu.md) class.

### 签名

#### Invoke

```csharp
delegate void IndexPressedEventHandler(long index)
```
