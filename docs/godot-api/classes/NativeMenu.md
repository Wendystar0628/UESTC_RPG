<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NativeMenu

命名空间 `Godot`；static class

`NativeMenu` handles low-level access to the OS native global menu bar and popup menus.

**Note:** This is low-level API, consider using [`MenuBar`](MenuBar.md) with [`MenuBar.PreferGlobalMenu`](MenuBar.md) set to `true`, and [`PopupMenu`](PopupMenu.md) with [`PopupMenu.PreferNativeMenu`](PopupMenu.md) set to `true`.

To create a menu, use `CreateMenu()`, add menu items using `add_*_item` methods. To remove a menu, use `FreeMenu()`.

```csharp
var menu

func _menu_callback(item_id):
if item_id == "ITEM_CUT":
cut()
elif item_id == "ITEM_COPY":
copy()
elif item_id == "ITEM_PASTE":
paste()

func _enter_tree():
# Create new menu and add items:
menu = NativeMenu.create_menu()
NativeMenu.add_item(menu, "Cut", _menu_callback, Callable(), "ITEM_CUT")
NativeMenu.add_item(menu, "Copy", _menu_callback, Callable(), "ITEM_COPY")
NativeMenu.add_separator(menu)
NativeMenu.add_item(menu, "Paste", _menu_callback, Callable(), "ITEM_PASTE")

func _on_button_pressed():
# Show popup menu at mouse position:
NativeMenu.popup(menu, DisplayServer.mouse_get_position())

func _exit_tree():
# Remove menu when it's no longer needed:
NativeMenu.free_menu(menu)

```

## 属性

### Singleton

```csharp
public static NativeMenuInstance Singleton { get; }
```

## 方法

### AddCheckItem

```csharp
public static int AddCheckItem(Rid rid, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`add_check_item`

Adds a new checkable item with text `label` to the global menu `rid`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddIconCheckItem

```csharp
public static int AddIconCheckItem(Rid rid, Texture2D icon, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`add_icon_check_item`

Adds a new checkable item with text `label` and icon `icon` to the global menu `rid`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddIconItem

```csharp
public static int AddIconItem(Rid rid, Texture2D icon, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`add_icon_item`

Adds a new item with text `label` and icon `icon` to the global menu `rid`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddIconRadioCheckItem

```csharp
public static int AddIconRadioCheckItem(Rid rid, Texture2D icon, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`add_icon_radio_check_item`

Adds a new radio-checkable item with text `label` and icon `icon` to the global menu `rid`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** Radio-checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See `SetItemChecked()` for more info on how to control it.

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddItem

```csharp
public static int AddItem(Rid rid, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`add_item`

Adds a new item with text `label` to the global menu `rid`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddMultistateItem

```csharp
public static int AddMultistateItem(Rid rid, string label, int maxStates, int defaultState, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`add_multistate_item`

Adds a new item with text `label` to the global menu `rid`.

Contrarily to normal binary items, multistate items can have more than two states, as defined by `maxStates`. Each press or activate of the item will increase the state by one. The default value is defined by `defaultState`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** By default, there's no indication of the current item state, it should be changed manually.

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddRadioCheckItem

```csharp
public static int AddRadioCheckItem(Rid rid, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`add_radio_check_item`

Adds a new radio-checkable item with text `label` to the global menu `rid`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** Radio-checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See `SetItemChecked()` for more info on how to control it.

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddSeparator

```csharp
public static int AddSeparator(Rid rid, int index = -1)
```

引擎名：`add_separator`

Adds a separator between items to the global menu `rid`. Separators also occupy an index.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

**Note:** This method is implemented on macOS and Windows.

### AddSubmenuItem

```csharp
public static int AddSubmenuItem(Rid rid, string label, Rid submenuRid, Variant tag = default, int index = -1)
```

引擎名：`add_submenu_item`

Adds an item that will act as a submenu of the global menu `rid`. The `submenuRid` argument is the RID of the global menu that will be shown when the item is clicked.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

**Note:** This method is implemented on macOS and Windows.

### Clear

```csharp
public static void Clear(Rid rid)
```

引擎名：`clear`

Removes all items from the global menu `rid`.

**Note:** This method is implemented on macOS and Windows.

### CreateMenu

```csharp
public static Rid CreateMenu()
```

引擎名：`create_menu`

Creates a new global menu object.

**Note:** This method is implemented on macOS and Windows.

### FindItemIndexWithSubmenu

```csharp
public static int FindItemIndexWithSubmenu(Rid rid, Rid submenuRid)
```

引擎名：`find_item_index_with_submenu`

Returns the index of the item with the submenu specified by `submenuRid`. Indices are automatically assigned to each item by the engine.

**Note:** This method is implemented on macOS and Windows.

### FindItemIndexWithTag

```csharp
public static int FindItemIndexWithTag(Rid rid, Variant tag)
```

引擎名：`find_item_index_with_tag`

Returns the index of the item with the specified `tag`. Indices are automatically assigned to each item by the engine.

**Note:** This method is implemented on macOS and Windows.

### FindItemIndexWithText

```csharp
public static int FindItemIndexWithText(Rid rid, string text)
```

引擎名：`find_item_index_with_text`

Returns the index of the item with the specified `text`. Indices are automatically assigned to each item by the engine.

**Note:** This method is implemented on macOS and Windows.

### FreeMenu

```csharp
public static void FreeMenu(Rid rid)
```

引擎名：`free_menu`

Frees a global menu object created by this `NativeMenu`.

**Note:** This method is implemented on macOS and Windows.

### GetItemAccelerator

```csharp
public static Key GetItemAccelerator(Rid rid, int idx)
```

引擎名：`get_item_accelerator`

Returns the accelerator of the item at index `idx`. Accelerators are special combinations of keys that activate the item, no matter which control is focused.

**Note:** This method is implemented only on macOS.

### GetItemCallback

```csharp
public static Callable GetItemCallback(Rid rid, int idx)
```

引擎名：`get_item_callback`

Returns the callback of the item at index `idx`.

**Note:** This method is implemented on macOS and Windows.

### GetItemCount

```csharp
public static int GetItemCount(Rid rid)
```

引擎名：`get_item_count`

Returns number of items in the global menu `rid`.

**Note:** This method is implemented on macOS and Windows.

### GetItemIcon

```csharp
public static Texture2D GetItemIcon(Rid rid, int idx)
```

引擎名：`get_item_icon`

Returns the icon of the item at index `idx`.

**Note:** This method is implemented on macOS and Windows.

### GetItemIndentationLevel

```csharp
public static int GetItemIndentationLevel(Rid rid, int idx)
```

引擎名：`get_item_indentation_level`

Returns the horizontal offset of the item at the given `idx`.

**Note:** This method is implemented only on macOS.

### GetItemKeyCallback

```csharp
public static Callable GetItemKeyCallback(Rid rid, int idx)
```

引擎名：`get_item_key_callback`

Returns the callback of the item accelerator at index `idx`.

**Note:** This method is implemented only on macOS.

### GetItemMaxStates

```csharp
public static int GetItemMaxStates(Rid rid, int idx)
```

引擎名：`get_item_max_states`

Returns number of states of a multistate item. See `AddMultistateItem()` for details.

**Note:** This method is implemented on macOS and Windows.

### GetItemState

```csharp
public static int GetItemState(Rid rid, int idx)
```

引擎名：`get_item_state`

Returns the state of a multistate item. See `AddMultistateItem()` for details.

**Note:** This method is implemented on macOS and Windows.

### GetItemSubmenu

```csharp
public static Rid GetItemSubmenu(Rid rid, int idx)
```

引擎名：`get_item_submenu`

Returns the submenu ID of the item at index `idx`. See `AddSubmenuItem()` for more info on how to add a submenu.

**Note:** This method is implemented on macOS and Windows.

### GetItemTag

```csharp
public static Variant GetItemTag(Rid rid, int idx)
```

引擎名：`get_item_tag`

Returns the metadata of the specified item, which might be of any type. You can set it with `SetItemTag()`, which provides a simple way of assigning context data to items.

**Note:** This method is implemented on macOS and Windows.

### GetItemText

```csharp
public static string GetItemText(Rid rid, int idx)
```

引擎名：`get_item_text`

Returns the text of the item at index `idx`.

**Note:** This method is implemented on macOS and Windows.

### GetItemTooltip

```csharp
public static string GetItemTooltip(Rid rid, int idx)
```

引擎名：`get_item_tooltip`

Returns the tooltip associated with the specified index `idx`.

**Note:** This method is implemented only on macOS.

### GetMinimumWidth

```csharp
public static float GetMinimumWidth(Rid rid)
```

引擎名：`get_minimum_width`

Returns global menu minimum width.

**Note:** This method is implemented only on macOS.

### GetPopupCloseCallback

```csharp
public static Callable GetPopupCloseCallback(Rid rid)
```

引擎名：`get_popup_close_callback`

Returns global menu close callback.

**Note:** This method is implemented on macOS and Windows.

### GetPopupOpenCallback

```csharp
public static Callable GetPopupOpenCallback(Rid rid)
```

引擎名：`get_popup_open_callback`

Returns global menu open callback.

**Note:** This method is implemented only on macOS.

### GetSize

```csharp
public static Vector2 GetSize(Rid rid)
```

引擎名：`get_size`

Returns global menu size.

**Note:** This method is implemented on macOS and Windows.

### GetSystemMenu

```csharp
public static Rid GetSystemMenu(NativeMenu.SystemMenus menuId)
```

引擎名：`get_system_menu`

Returns RID of a special system menu.

**Note:** This method is implemented only on macOS.

### GetSystemMenuName

```csharp
public static string GetSystemMenuName(NativeMenu.SystemMenus menuId)
```

引擎名：`get_system_menu_name`

Returns readable name of a special system menu.

**Note:** This method is implemented only on macOS.

### GetSystemMenuText

```csharp
public static string GetSystemMenuText(NativeMenu.SystemMenus menuId)
```

引擎名：`get_system_menu_text`

Returns the text of the system menu item.

**Note:** This method is implemented on macOS.

### HasFeature

```csharp
public static bool HasFeature(NativeMenu.Feature feature)
```

引擎名：`has_feature`

Returns `true` if the specified `feature` is supported by the current `NativeMenu`, `false` otherwise.

**Note:** This method is implemented on macOS and Windows.

### HasMenu

```csharp
public static bool HasMenu(Rid rid)
```

引擎名：`has_menu`

Returns `true` if `rid` is valid global menu.

**Note:** This method is implemented on macOS and Windows.

### HasSystemMenu

```csharp
public static bool HasSystemMenu(NativeMenu.SystemMenus menuId)
```

引擎名：`has_system_menu`

Returns `true` if a special system menu is supported.

**Note:** This method is implemented only on macOS.

### IsItemCheckable

```csharp
public static bool IsItemCheckable(Rid rid, int idx)
```

引擎名：`is_item_checkable`

Returns `true` if the item at index `idx` is checkable in some way, i.e. if it has a checkbox or radio button.

**Note:** This method is implemented on macOS and Windows.

### IsItemChecked

```csharp
public static bool IsItemChecked(Rid rid, int idx)
```

引擎名：`is_item_checked`

Returns `true` if the item at index `idx` is checked.

**Note:** This method is implemented on macOS and Windows.

### IsItemDisabled

```csharp
public static bool IsItemDisabled(Rid rid, int idx)
```

引擎名：`is_item_disabled`

Returns `true` if the item at index `idx` is disabled. When it is disabled it can't be selected, or its action invoked.

See `SetItemDisabled()` for more info on how to disable an item.

**Note:** This method is implemented on macOS and Windows.

### IsItemHidden

```csharp
public static bool IsItemHidden(Rid rid, int idx)
```

引擎名：`is_item_hidden`

Returns `true` if the item at index `idx` is hidden.

See `SetItemHidden()` for more info on how to hide an item.

**Note:** This method is implemented only on macOS.

### IsItemRadioCheckable

```csharp
public static bool IsItemRadioCheckable(Rid rid, int idx)
```

引擎名：`is_item_radio_checkable`

Returns `true` if the item at index `idx` has radio button-style checkability.

**Note:** This is purely cosmetic; you must add the logic for checking/unchecking items in radio groups.

**Note:** This method is implemented on macOS and Windows.

### IsOpened

```csharp
public static bool IsOpened(Rid rid)
```

引擎名：`is_opened`

Returns `true` if the menu is currently opened.

**Note:** This method is implemented only on macOS.

### IsSystemMenu

```csharp
public static bool IsSystemMenu(Rid rid)
```

引擎名：`is_system_menu`

Return `true` is global menu is a special system menu.

**Note:** This method is implemented only on macOS.

### Popup

```csharp
public static void Popup(Rid rid, Vector2I position)
```

引擎名：`popup`

Shows the global menu at `position` in the screen coordinates.

**Note:** This method is implemented on macOS and Windows.

### RemoveItem

```csharp
public static void RemoveItem(Rid rid, int idx)
```

引擎名：`remove_item`

Removes the item at index `idx` from the global menu `rid`.

**Note:** The indices of items after the removed item will be shifted by one.

**Note:** This method is implemented on macOS and Windows.

### SetInterfaceDirection

```csharp
public static void SetInterfaceDirection(Rid rid, bool isRtl)
```

引擎名：`set_interface_direction`

Sets the menu text layout direction from right-to-left if `isRtl` is `true`.

**Note:** This method is implemented on macOS and Windows.

### SetItemAccelerator

```csharp
public static void SetItemAccelerator(Rid rid, int idx, Key keycode)
```

引擎名：`set_item_accelerator`

Sets the accelerator of the item at index `idx`. `keycode` can be a single [`Key`](Key.md), or a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** This method is implemented only on macOS.

### SetItemCallback

```csharp
public static void SetItemCallback(Rid rid, int idx, Callable callback)
```

引擎名：`set_item_callback`

Sets the callback of the item at index `idx`. Callback is emitted when an item is pressed.

**Note:** The `callback` Callable needs to accept exactly one Variant parameter, the parameter passed to the Callable will be the value passed to the `tag` parameter when the menu item was created.

**Note:** This method is implemented on macOS and Windows.

### SetItemCheckable

```csharp
public static void SetItemCheckable(Rid rid, int idx, bool checkable)
```

引擎名：`set_item_checkable`

Sets whether the item at index `idx` has a checkbox. If `false`, sets the type of the item to plain text.

**Note:** This method is implemented on macOS and Windows.

### SetItemChecked

```csharp
public static void SetItemChecked(Rid rid, int idx, bool checked)
```

引擎名：`set_item_checked`

Sets the checkstate status of the item at index `idx`.

**Note:** This method is implemented on macOS and Windows.

### SetItemDisabled

```csharp
public static void SetItemDisabled(Rid rid, int idx, bool disabled)
```

引擎名：`set_item_disabled`

Enables/disables the item at index `idx`. When it is disabled, it can't be selected and its action can't be invoked.

**Note:** This method is implemented on macOS and Windows.

### SetItemHidden

```csharp
public static void SetItemHidden(Rid rid, int idx, bool hidden)
```

引擎名：`set_item_hidden`

Hides/shows the item at index `idx`. When it is hidden, an item does not appear in a menu and its action cannot be invoked.

**Note:** This method is implemented only on macOS.

### SetItemHoverCallbacks

```csharp
public static void SetItemHoverCallbacks(Rid rid, int idx, Callable callback)
```

引擎名：`set_item_hover_callbacks`

Sets the callback of the item at index `idx`. The callback is emitted when an item is hovered.

**Note:** The `callback` Callable needs to accept exactly one Variant parameter, the parameter passed to the Callable will be the value passed to the `tag` parameter when the menu item was created.

**Note:** This method is implemented only on macOS.

### SetItemIcon

```csharp
public static void SetItemIcon(Rid rid, int idx, Texture2D icon)
```

引擎名：`set_item_icon`

Replaces the [`Texture2D`](Texture2D.md) icon of the specified `idx`.

**Note:** This method is implemented on macOS and Windows.

**Note:** This method is not supported by macOS Dock menu items.

### SetItemIndentationLevel

```csharp
public static void SetItemIndentationLevel(Rid rid, int idx, int level)
```

引擎名：`set_item_indentation_level`

Sets the horizontal offset of the item at the given `idx`.

**Note:** This method is implemented only on macOS.

### SetItemIndex

```csharp
public static int SetItemIndex(Rid rid, int idx, int targetIdx)
```

引擎名：`set_item_index`

Changes the index of the item at index `idx` to be at index `targetIdx`. This can be used to move an item above other items.

Returns the new index of the moved item, it's not guaranteed to be the same as `targetIdx`.

**Note:** The indices of any items between index `idx` and index `targetIdx` will be shifted by one.

**Note:** This method is implemented on macOS and Windows.

### SetItemKeyCallback

```csharp
public static void SetItemKeyCallback(Rid rid, int idx, Callable keyCallback)
```

引擎名：`set_item_key_callback`

Sets the callback of the item at index `idx`. Callback is emitted when its accelerator is activated.

**Note:** The `keyCallback` Callable needs to accept exactly one Variant parameter, the parameter passed to the Callable will be the value passed to the `tag` parameter when the menu item was created.

**Note:** This method is implemented only on macOS.

### SetItemMaxStates

```csharp
public static void SetItemMaxStates(Rid rid, int idx, int maxStates)
```

引擎名：`set_item_max_states`

Sets number of state of a multistate item. See `AddMultistateItem()` for details.

**Note:** This method is implemented on macOS and Windows.

### SetItemRadioCheckable

```csharp
public static void SetItemRadioCheckable(Rid rid, int idx, bool checkable)
```

引擎名：`set_item_radio_checkable`

Sets the type of the item at the specified index `idx` to radio button. If `false`, sets the type of the item to plain text.

**Note:** This is purely cosmetic; you must add the logic for checking/unchecking items in radio groups.

**Note:** This method is implemented on macOS and Windows.

### SetItemState

```csharp
public static void SetItemState(Rid rid, int idx, int state)
```

引擎名：`set_item_state`

Sets the state of a multistate item. See `AddMultistateItem()` for details.

**Note:** This method is implemented on macOS and Windows.

### SetItemSubmenu

```csharp
public static void SetItemSubmenu(Rid rid, int idx, Rid submenuRid)
```

引擎名：`set_item_submenu`

Sets the submenu RID of the item at index `idx`. The submenu is a global menu that would be shown when the item is clicked.

**Note:** This method is implemented on macOS and Windows.

### SetItemTag

```csharp
public static void SetItemTag(Rid rid, int idx, Variant tag)
```

引擎名：`set_item_tag`

Sets the metadata of an item, which may be of any type. You can later get it with `GetItemTag()`, which provides a simple way of assigning context data to items.

**Note:** This method is implemented on macOS and Windows.

### SetItemText

```csharp
public static void SetItemText(Rid rid, int idx, string text)
```

引擎名：`set_item_text`

Sets the text of the item at index `idx`.

**Note:** This method is implemented on macOS and Windows.

### SetItemTooltip

```csharp
public static void SetItemTooltip(Rid rid, int idx, string tooltip)
```

引擎名：`set_item_tooltip`

Sets the `System.String` tooltip of the item at the specified index `idx`.

**Note:** This method is implemented only on macOS.

### SetMinimumWidth

```csharp
public static void SetMinimumWidth(Rid rid, float width)
```

引擎名：`set_minimum_width`

Sets the minimum width of the global menu.

**Note:** This method is implemented only on macOS.

### SetPopupCloseCallback

```csharp
public static void SetPopupCloseCallback(Rid rid, Callable callback)
```

引擎名：`set_popup_close_callback`

Registers callable to emit when the menu is about to show.

**Note:** The OS can simulate menu opening to track menu item changes and global shortcuts, in which case the corresponding close callback is not triggered. Use `IsOpened()` to check if the menu is currently opened.

**Note:** This method is implemented on macOS and Windows.

### SetPopupOpenCallback

```csharp
public static void SetPopupOpenCallback(Rid rid, Callable callback)
```

引擎名：`set_popup_open_callback`

Registers callable to emit after the menu is closed.

**Note:** This method is implemented only on macOS.

### SetSystemMenuText

```csharp
public static void SetSystemMenuText(NativeMenu.SystemMenus menuId, string name)
```

引擎名：`set_system_menu_text`

Sets the text of the system menu item.

**Note:** This method is implemented on macOS.

## Feature

命名空间 `Godot`；enum

### 枚举值

- `GlobalMenu = 0`：[`NativeMenu`](NativeMenu.md) supports native global main menu.
- `PopupMenu = 1`：[`NativeMenu`](NativeMenu.md) supports native popup menus.
- `OpenCloseCallback = 2`：[`NativeMenu`](NativeMenu.md) supports menu open and close callbacks.
- `HoverCallback = 3`：[`NativeMenu`](NativeMenu.md) supports menu item hover callback.
- `KeyCallback = 4`：[`NativeMenu`](NativeMenu.md) supports menu item accelerator/key callback.

## SystemMenus

命名空间 `Godot`；enum

### 枚举值

- `InvalidMenuId = 0`：Invalid special system menu ID.
- `MainMenuId = 1`：Global main menu ID.
- `ApplicationMenuId = 2`：Application (first menu after "Apple" menu on macOS) menu ID.
- `WindowMenuId = 3`："Window" menu ID (on macOS this menu includes standard window control items and a list of open windows).
- `HelpMenuId = 4`："Help" menu ID (on macOS this menu includes help search bar).
- `DockMenuId = 5`：Dock icon right-click menu ID (on macOS this menu include standard application control items and a list of open windows).
