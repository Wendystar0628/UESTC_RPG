<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MenuBar

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A horizontal menu bar that creates a menu for each [`PopupMenu`](PopupMenu.md) child. New items are created by adding [`PopupMenu`](PopupMenu.md)s to this node. Item title is determined by [`Window.Title`](Window.md), or node name if [`Window.Title`](Window.md) is empty. Item title can be overridden using `SetMenuTitle()`.

## 构造函数

### MenuBar

```csharp
public MenuBar()
```

## 属性

### Flat

```csharp
public bool Flat { get; set; }
```

引擎名：`flat`

Flat `MenuBar` don't display item decoration.

### Language

```csharp
public string Language { get; set; }
```

引擎名：`language`

Language code used for line-breaking and text shaping algorithms. If left empty, the current locale is used instead.

### PreferGlobalMenu

```csharp
public bool PreferGlobalMenu { get; set; }
```

引擎名：`prefer_global_menu`

If `true`, `MenuBar` will use system global menu when supported.

**Note:** If `true` and global menu is supported, this node is not displayed, has zero size, and all its child nodes except [`PopupMenu`](PopupMenu.md)s are inaccessible.

**Note:** This property overrides the value of the [`PopupMenu.PreferNativeMenu`](PopupMenu.md) property of the child nodes.

### StartIndex

```csharp
public int StartIndex { get; set; }
```

引擎名：`start_index`

Position order in the global menu to insert `MenuBar` items at. All menu items in the `MenuBar` are always inserted as a continuous range. Menus with lower `StartIndex` are inserted first. Menus with `StartIndex` equal to `-1` are inserted last.

### SwitchOnHover

```csharp
public bool SwitchOnHover { get; set; }
```

引擎名：`switch_on_hover`

If `true`, when the cursor hovers above menu item, it will close the current [`PopupMenu`](PopupMenu.md) and open the other one.

### TextDirection

```csharp
public Control.TextDirection TextDirection { get; set; }
```

引擎名：`text_direction`

Base text writing direction.

## 方法

### GetLanguage

```csharp
public string GetLanguage()
```

引擎名：`get_language`

### GetMenuCount

```csharp
public int GetMenuCount()
```

引擎名：`get_menu_count`

Returns number of menu items.

### GetMenuPopup

```csharp
public PopupMenu GetMenuPopup(int menu)
```

引擎名：`get_menu_popup`

Returns [`PopupMenu`](PopupMenu.md) associated with menu item.

### GetMenuTitle

```csharp
public string GetMenuTitle(int menu)
```

引擎名：`get_menu_title`

Returns menu item title.

### GetMenuTooltip

```csharp
public string GetMenuTooltip(int menu)
```

引擎名：`get_menu_tooltip`

Returns menu item tooltip.

### GetStartIndex

```csharp
public int GetStartIndex()
```

引擎名：`get_start_index`

### GetTextDirection

```csharp
public Control.TextDirection GetTextDirection()
```

引擎名：`get_text_direction`

### IsFlat

```csharp
public bool IsFlat()
```

引擎名：`is_flat`

### IsMenuDisabled

```csharp
public bool IsMenuDisabled(int menu)
```

引擎名：`is_menu_disabled`

Returns `true` if the menu item is disabled.

### IsMenuHidden

```csharp
public bool IsMenuHidden(int menu)
```

引擎名：`is_menu_hidden`

Returns `true` if the menu item is hidden.

### IsNativeMenu

```csharp
public bool IsNativeMenu()
```

引擎名：`is_native_menu`

Returns `true` if the current system's global menu is supported and used by this `MenuBar`.

### IsPreferGlobalMenu

```csharp
public bool IsPreferGlobalMenu()
```

引擎名：`is_prefer_global_menu`

### IsSwitchOnHover

```csharp
public bool IsSwitchOnHover()
```

引擎名：`is_switch_on_hover`

### SetDisableShortcuts

```csharp
public void SetDisableShortcuts(bool disabled)
```

引擎名：`set_disable_shortcuts`

If `true`, shortcuts are disabled and cannot be used to trigger the button.

### SetFlat

```csharp
public void SetFlat(bool enabled)
```

引擎名：`set_flat`

### SetLanguage

```csharp
public void SetLanguage(string language)
```

引擎名：`set_language`

### SetMenuDisabled

```csharp
public void SetMenuDisabled(int menu, bool disabled)
```

引擎名：`set_menu_disabled`

If `true`, menu item is disabled.

### SetMenuHidden

```csharp
public void SetMenuHidden(int menu, bool hidden)
```

引擎名：`set_menu_hidden`

If `true`, menu item is hidden.

### SetMenuTitle

```csharp
public void SetMenuTitle(int menu, string title)
```

引擎名：`set_menu_title`

Sets menu item title.

### SetMenuTooltip

```csharp
public void SetMenuTooltip(int menu, string tooltip)
```

引擎名：`set_menu_tooltip`

Sets menu item tooltip.

### SetPreferGlobalMenu

```csharp
public void SetPreferGlobalMenu(bool enabled)
```

引擎名：`set_prefer_global_menu`

### SetStartIndex

```csharp
public void SetStartIndex(int enabled)
```

引擎名：`set_start_index`

### SetSwitchOnHover

```csharp
public void SetSwitchOnHover(bool enable)
```

引擎名：`set_switch_on_hover`

### SetTextDirection

```csharp
public void SetTextDirection(Control.TextDirection direction)
```

引擎名：`set_text_direction`
