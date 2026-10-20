<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NativeMenuInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

[`NativeMenu`](NativeMenu.md) handles low-level access to the OS native global menu bar and popup menus.

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

## 方法

### AddCheckItem

```csharp
public int AddCheckItem(Rid rid, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

Adds a new checkable item with text `label` to the global menu `rid`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddIconCheckItem

```csharp
public int AddIconCheckItem(Rid rid, Texture2D icon, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

Adds a new checkable item with text `label` and icon `icon` to the global menu `rid`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddIconItem

```csharp
public int AddIconItem(Rid rid, Texture2D icon, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

Adds a new item with text `label` and icon `icon` to the global menu `rid`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddIconRadioCheckItem

```csharp
public int AddIconRadioCheckItem(Rid rid, Texture2D icon, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

Adds a new radio-checkable item with text `label` and icon `icon` to the global menu `rid`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** Radio-checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See `SetItemChecked()` for more info on how to control it.

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddItem

```csharp
public int AddItem(Rid rid, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

Adds a new item with text `label` to the global menu `rid`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddMultistateItem

```csharp
public int AddMultistateItem(Rid rid, string label, int maxStates, int defaultState, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

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
public int AddRadioCheckItem(Rid rid, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

Adds a new radio-checkable item with text `label` to the global menu `rid`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** Radio-checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See `SetItemChecked()` for more info on how to control it.

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented on macOS and Windows.

**Note:** On Windows, `accelerator` and `keyCallback` are ignored.

### AddSeparator

```csharp
public int AddSeparator(Rid rid, int index = -1)
```

Adds a separator between items to the global menu `rid`. Separators also occupy an index.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

**Note:** This method is implemented on macOS and Windows.

### AddSubmenuItem

```csharp
public int AddSubmenuItem(Rid rid, string label, Rid submenuRid, Variant tag = default, int index = -1)
```

Adds an item that will act as a submenu of the global menu `rid`. The `submenuRid` argument is the RID of the global menu that will be shown when the item is clicked.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

**Note:** This method is implemented on macOS and Windows.

### Clear

```csharp
public void Clear(Rid rid)
```

Removes all items from the global menu `rid`.

**Note:** This method is implemented on macOS and Windows.

### CreateMenu

```csharp
public Rid CreateMenu()
```

Creates a new global menu object.

**Note:** This method is implemented on macOS and Windows.

### FindItemIndexWithSubmenu

```csharp
public int FindItemIndexWithSubmenu(Rid rid, Rid submenuRid)
```

Returns the index of the item with the submenu specified by `submenuRid`. Indices are automatically assigned to each item by the engine.

**Note:** This method is implemented on macOS and Windows.

### FindItemIndexWithTag

```csharp
public int FindItemIndexWithTag(Rid rid, Variant tag)
```

Returns the index of the item with the specified `tag`. Indices are automatically assigned to each item by the engine.

**Note:** This method is implemented on macOS and Windows.

### FindItemIndexWithText

```csharp
public int FindItemIndexWithText(Rid rid, string text)
```

Returns the index of the item with the specified `text`. Indices are automatically assigned to each item by the engine.

**Note:** This method is implemented on macOS and Windows.

### FreeMenu

```csharp
public void FreeMenu(Rid rid)
```

Frees a global menu object created by this [`NativeMenu`](NativeMenu.md).

**Note:** This method is implemented on macOS and Windows.

### GetItemAccelerator

```csharp
public Key GetItemAccelerator(Rid rid, int idx)
```

Returns the accelerator of the item at index `idx`. Accelerators are special combinations of keys that activate the item, no matter which control is focused.

**Note:** This method is implemented only on macOS.

### GetItemCallback

```csharp
public Callable GetItemCallback(Rid rid, int idx)
```

Returns the callback of the item at index `idx`.

**Note:** This method is implemented on macOS and Windows.

### GetItemCount

```csharp
public int GetItemCount(Rid rid)
```

Returns number of items in the global menu `rid`.

**Note:** This method is implemented on macOS and Windows.

### GetItemIcon

```csharp
public Texture2D GetItemIcon(Rid rid, int idx)
```

Returns the icon of the item at index `idx`.

**Note:** This method is implemented on macOS and Windows.

### GetItemIndentationLevel

```csharp
public int GetItemIndentationLevel(Rid rid, int idx)
```

Returns the horizontal offset of the item at the given `idx`.

**Note:** This method is implemented only on macOS.

### GetItemKeyCallback

```csharp
public Callable GetItemKeyCallback(Rid rid, int idx)
```

Returns the callback of the item accelerator at index `idx`.

**Note:** This method is implemented only on macOS.

### GetItemMaxStates

```csharp
public int GetItemMaxStates(Rid rid, int idx)
```

Returns number of states of a multistate item. See `AddMultistateItem()` for details.

**Note:** This method is implemented on macOS and Windows.

### GetItemState

```csharp
public int GetItemState(Rid rid, int idx)
```

Returns the state of a multistate item. See `AddMultistateItem()` for details.

**Note:** This method is implemented on macOS and Windows.

### GetItemSubmenu

```csharp
public Rid GetItemSubmenu(Rid rid, int idx)
```

Returns the submenu ID of the item at index `idx`. See `AddSubmenuItem()` for more info on how to add a submenu.

**Note:** This method is implemented on macOS and Windows.

### GetItemTag

```csharp
public Variant GetItemTag(Rid rid, int idx)
```

Returns the metadata of the specified item, which might be of any type. You can set it with `SetItemTag()`, which provides a simple way of assigning context data to items.

**Note:** This method is implemented on macOS and Windows.

### GetItemText

```csharp
public string GetItemText(Rid rid, int idx)
```

Returns the text of the item at index `idx`.

**Note:** This method is implemented on macOS and Windows.

### GetItemTooltip

```csharp
public string GetItemTooltip(Rid rid, int idx)
```

Returns the tooltip associated with the specified index `idx`.

**Note:** This method is implemented only on macOS.

### GetMinimumWidth

```csharp
public float GetMinimumWidth(Rid rid)
```

Returns global menu minimum width.

**Note:** This method is implemented only on macOS.

### GetPopupCloseCallback

```csharp
public Callable GetPopupCloseCallback(Rid rid)
```

Returns global menu close callback.

**Note:** This method is implemented on macOS and Windows.

### GetPopupOpenCallback

```csharp
public Callable GetPopupOpenCallback(Rid rid)
```

Returns global menu open callback.

**Note:** This method is implemented only on macOS.

### GetSize

```csharp
public Vector2 GetSize(Rid rid)
```

Returns global menu size.

**Note:** This method is implemented on macOS and Windows.

### GetSystemMenu

```csharp
public Rid GetSystemMenu(NativeMenu.SystemMenus menuId)
```

Returns RID of a special system menu.

**Note:** This method is implemented only on macOS.

### GetSystemMenuName

```csharp
public string GetSystemMenuName(NativeMenu.SystemMenus menuId)
```

Returns readable name of a special system menu.

**Note:** This method is implemented only on macOS.

### GetSystemMenuText

```csharp
public string GetSystemMenuText(NativeMenu.SystemMenus menuId)
```

Returns the text of the system menu item.

**Note:** This method is implemented on macOS.

### HasFeature

```csharp
public bool HasFeature(NativeMenu.Feature feature)
```

Returns `true` if the specified `feature` is supported by the current [`NativeMenu`](NativeMenu.md), `false` otherwise.

**Note:** This method is implemented on macOS and Windows.

### HasMenu

```csharp
public bool HasMenu(Rid rid)
```

Returns `true` if `rid` is valid global menu.

**Note:** This method is implemented on macOS and Windows.

### HasSystemMenu

```csharp
public bool HasSystemMenu(NativeMenu.SystemMenus menuId)
```

Returns `true` if a special system menu is supported.

**Note:** This method is implemented only on macOS.

### IsItemCheckable

```csharp
public bool IsItemCheckable(Rid rid, int idx)
```

Returns `true` if the item at index `idx` is checkable in some way, i.e. if it has a checkbox or radio button.

**Note:** This method is implemented on macOS and Windows.

### IsItemChecked

```csharp
public bool IsItemChecked(Rid rid, int idx)
```

Returns `true` if the item at index `idx` is checked.

**Note:** This method is implemented on macOS and Windows.

### IsItemDisabled

```csharp
public bool IsItemDisabled(Rid rid, int idx)
```

Returns `true` if the item at index `idx` is disabled. When it is disabled it can't be selected, or its action invoked.

See `SetItemDisabled()` for more info on how to disable an item.

**Note:** This method is implemented on macOS and Windows.

### IsItemHidden

```csharp
public bool IsItemHidden(Rid rid, int idx)
```

Returns `true` if the item at index `idx` is hidden.

See `SetItemHidden()` for more info on how to hide an item.

**Note:** This method is implemented only on macOS.

### IsItemRadioCheckable

```csharp
public bool IsItemRadioCheckable(Rid rid, int idx)
```

Returns `true` if the item at index `idx` has radio button-style checkability.

**Note:** This is purely cosmetic; you must add the logic for checking/unchecking items in radio groups.

**Note:** This method is implemented on macOS and Windows.

### IsOpened

```csharp
public bool IsOpened(Rid rid)
```

Returns `true` if the menu is currently opened.

**Note:** This method is implemented only on macOS.

### IsSystemMenu

```csharp
public bool IsSystemMenu(Rid rid)
```

Return `true` is global menu is a special system menu.

**Note:** This method is implemented only on macOS.

### Popup

```csharp
public void Popup(Rid rid, Vector2I position)
```

Shows the global menu at `position` in the screen coordinates.

**Note:** This method is implemented on macOS and Windows.

### RemoveItem

```csharp
public void RemoveItem(Rid rid, int idx)
```

Removes the item at index `idx` from the global menu `rid`.

**Note:** The indices of items after the removed item will be shifted by one.

**Note:** This method is implemented on macOS and Windows.

### SetInterfaceDirection

```csharp
public void SetInterfaceDirection(Rid rid, bool isRtl)
```

Sets the menu text layout direction from right-to-left if `isRtl` is `true`.

**Note:** This method is implemented on macOS and Windows.

### SetItemAccelerator

```csharp
public void SetItemAccelerator(Rid rid, int idx, Key keycode)
```

Sets the accelerator of the item at index `idx`. `keycode` can be a single [`Key`](Key.md), or a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** This method is implemented only on macOS.

### SetItemCallback

```csharp
public void SetItemCallback(Rid rid, int idx, Callable callback)
```

Sets the callback of the item at index `idx`. Callback is emitted when an item is pressed.

**Note:** The `callback` Callable needs to accept exactly one Variant parameter, the parameter passed to the Callable will be the value passed to the `tag` parameter when the menu item was created.

**Note:** This method is implemented on macOS and Windows.

### SetItemCheckable

```csharp
public void SetItemCheckable(Rid rid, int idx, bool checkable)
```

Sets whether the item at index `idx` has a checkbox. If `false`, sets the type of the item to plain text.

**Note:** This method is implemented on macOS and Windows.

### SetItemChecked

```csharp
public void SetItemChecked(Rid rid, int idx, bool checked)
```

Sets the checkstate status of the item at index `idx`.

**Note:** This method is implemented on macOS and Windows.

### SetItemDisabled

```csharp
public void SetItemDisabled(Rid rid, int idx, bool disabled)
```

Enables/disables the item at index `idx`. When it is disabled, it can't be selected and its action can't be invoked.

**Note:** This method is implemented on macOS and Windows.

### SetItemHidden

```csharp
public void SetItemHidden(Rid rid, int idx, bool hidden)
```

Hides/shows the item at index `idx`. When it is hidden, an item does not appear in a menu and its action cannot be invoked.

**Note:** This method is implemented only on macOS.

### SetItemHoverCallbacks

```csharp
public void SetItemHoverCallbacks(Rid rid, int idx, Callable callback)
```

Sets the callback of the item at index `idx`. The callback is emitted when an item is hovered.

**Note:** The `callback` Callable needs to accept exactly one Variant parameter, the parameter passed to the Callable will be the value passed to the `tag` parameter when the menu item was created.

**Note:** This method is implemented only on macOS.

### SetItemIcon

```csharp
public void SetItemIcon(Rid rid, int idx, Texture2D icon)
```

Replaces the [`Texture2D`](Texture2D.md) icon of the specified `idx`.

**Note:** This method is implemented on macOS and Windows.

**Note:** This method is not supported by macOS Dock menu items.

### SetItemIndentationLevel

```csharp
public void SetItemIndentationLevel(Rid rid, int idx, int level)
```

Sets the horizontal offset of the item at the given `idx`.

**Note:** This method is implemented only on macOS.

### SetItemIndex

```csharp
public int SetItemIndex(Rid rid, int idx, int targetIdx)
```

Changes the index of the item at index `idx` to be at index `targetIdx`. This can be used to move an item above other items.

Returns the new index of the moved item, it's not guaranteed to be the same as `targetIdx`.

**Note:** The indices of any items between index `idx` and index `targetIdx` will be shifted by one.

**Note:** This method is implemented on macOS and Windows.

### SetItemKeyCallback

```csharp
public void SetItemKeyCallback(Rid rid, int idx, Callable keyCallback)
```

Sets the callback of the item at index `idx`. Callback is emitted when its accelerator is activated.

**Note:** The `keyCallback` Callable needs to accept exactly one Variant parameter, the parameter passed to the Callable will be the value passed to the `tag` parameter when the menu item was created.

**Note:** This method is implemented only on macOS.

### SetItemMaxStates

```csharp
public void SetItemMaxStates(Rid rid, int idx, int maxStates)
```

Sets number of state of a multistate item. See `AddMultistateItem()` for details.

**Note:** This method is implemented on macOS and Windows.

### SetItemRadioCheckable

```csharp
public void SetItemRadioCheckable(Rid rid, int idx, bool checkable)
```

Sets the type of the item at the specified index `idx` to radio button. If `false`, sets the type of the item to plain text.

**Note:** This is purely cosmetic; you must add the logic for checking/unchecking items in radio groups.

**Note:** This method is implemented on macOS and Windows.

### SetItemState

```csharp
public void SetItemState(Rid rid, int idx, int state)
```

Sets the state of a multistate item. See `AddMultistateItem()` for details.

**Note:** This method is implemented on macOS and Windows.

### SetItemSubmenu

```csharp
public void SetItemSubmenu(Rid rid, int idx, Rid submenuRid)
```

Sets the submenu RID of the item at index `idx`. The submenu is a global menu that would be shown when the item is clicked.

**Note:** This method is implemented on macOS and Windows.

### SetItemTag

```csharp
public void SetItemTag(Rid rid, int idx, Variant tag)
```

Sets the metadata of an item, which may be of any type. You can later get it with `GetItemTag()`, which provides a simple way of assigning context data to items.

**Note:** This method is implemented on macOS and Windows.

### SetItemText

```csharp
public void SetItemText(Rid rid, int idx, string text)
```

Sets the text of the item at index `idx`.

**Note:** This method is implemented on macOS and Windows.

### SetItemTooltip

```csharp
public void SetItemTooltip(Rid rid, int idx, string tooltip)
```

Sets the `System.String` tooltip of the item at the specified index `idx`.

**Note:** This method is implemented only on macOS.

### SetMinimumWidth

```csharp
public void SetMinimumWidth(Rid rid, float width)
```

Sets the minimum width of the global menu.

**Note:** This method is implemented only on macOS.

### SetPopupCloseCallback

```csharp
public void SetPopupCloseCallback(Rid rid, Callable callback)
```

Registers callable to emit when the menu is about to show.

**Note:** The OS can simulate menu opening to track menu item changes and global shortcuts, in which case the corresponding close callback is not triggered. Use `IsOpened()` to check if the menu is currently opened.

**Note:** This method is implemented on macOS and Windows.

### SetPopupOpenCallback

```csharp
public void SetPopupOpenCallback(Rid rid, Callable callback)
```

Registers callable to emit after the menu is closed.

**Note:** This method is implemented only on macOS.

### SetSystemMenuText

```csharp
public void SetSystemMenuText(NativeMenu.SystemMenus menuId, string name)
```

Sets the text of the system menu item.

**Note:** This method is implemented on macOS.
