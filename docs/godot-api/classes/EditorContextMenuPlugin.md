<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorContextMenuPlugin

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

`EditorContextMenuPlugin` allows for the addition of custom options in the editor's context menu.

Currently, context menus are supported for three commonly used areas: the file system, scene tree, and editor script list panel.

## 构造函数

### EditorContextMenuPlugin

```csharp
public EditorContextMenuPlugin()
```

## 方法

### AddContextMenuItem

```csharp
public void AddContextMenuItem(string name, Callable callback, Texture2D icon = null)
```

引擎名：`add_context_menu_item`

Add custom option to the context menu of the plugin's specified slot. When the option is activated, `callback` will be called. Callback should take single [`Collections.Array`](Collections.Array.md) argument; array contents depend on context menu slot.

```csharp
func _popup_menu(paths):
add_context_menu_item("File Custom options", handle, ICON)

```

If you want to assign shortcut to the menu item, use `AddContextMenuItemFromShortcut()` instead.

### AddContextMenuItemFromShortcut

```csharp
public void AddContextMenuItemFromShortcut(string name, Shortcut shortcut, Texture2D icon = null)
```

引擎名：`add_context_menu_item_from_shortcut`

Add custom option to the context menu of the plugin's specified slot. The option will have the `shortcut` assigned and reuse its callback. The shortcut has to be registered beforehand with `AddMenuShortcut()`.

```csharp
func _init():
add_menu_shortcut(SHORTCUT, handle)

func _popup_menu(paths):
add_context_menu_item_from_shortcut("File Custom options", SHORTCUT, ICON)

```

### AddContextSubmenuItem

```csharp
public void AddContextSubmenuItem(string name, PopupMenu menu, Texture2D icon = null)
```

引擎名：`add_context_submenu_item`

Add a submenu to the context menu of the plugin's specified slot. The submenu is not automatically handled, you need to connect to its signals yourself. Also the submenu is freed on every popup, so provide a new [`PopupMenu`](PopupMenu.md) every time.

```csharp
func _popup_menu(paths):
var popup_menu = PopupMenu.new()
popup_menu.add_item("Blue")
popup_menu.add_item("White")
popup_menu.id_pressed.connect(_on_color_submenu_option)

add_context_submenu_item("Set Node Color", popup_menu)

```

### AddMenuShortcut

```csharp
public void AddMenuShortcut(Shortcut shortcut, Callable callback)
```

引擎名：`add_menu_shortcut`

Registers a shortcut associated with the plugin's context menu. This method should be called once (e.g. in plugin's [`GodotObject`](GodotObject.md)). `callback` will be called when user presses the specified `shortcut` while the menu's context is in effect (e.g. FileSystem dock is focused). Callback should take single [`Collections.Array`](Collections.Array.md) argument; array contents depend on context menu slot.

```csharp
func _init():
add_menu_shortcut(SHORTCUT, handle)

```

### _PopupMenu

```csharp
public virtual void _PopupMenu(string[] paths)
```

引擎名：`_popup_menu`

Called when creating a context menu, custom options can be added by using the `AddContextMenuItem()` or `AddContextMenuItemFromShortcut()` functions. `paths` contains currently selected paths (depending on menu), which can be used to conditionally add options.

## ContextMenuSlot

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `SceneTree = 0`：Context menu of Scene dock.
- `Filesystem = 1`：Context menu of FileSystem dock.
- `ScriptEditor = 2`：Context menu of Script editor's script tabs.
- `FilesystemCreate = 3`：The "Create..." submenu of FileSystem dock's context menu, or the "New" section of the main context menu when empty space is clicked.
- `ScriptEditorCode = 4`：Context menu of Script editor's code editor.
- `SceneTabs = 5`：Context menu of scene tabs.
- `Slot2DEditor = 6`：Context menu of 2D editor's basic right-click menu.
- `InspectorProperty = 7`：Context menu of the inspectors right-click menu.
