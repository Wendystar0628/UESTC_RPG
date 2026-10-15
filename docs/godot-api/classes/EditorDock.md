<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorDock

命名空间 `Godot`；class；继承 [`MarginContainer`](MarginContainer.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

EditorDock is a [`Container`](Container.md) node that can be docked in one of the editor's dock slots. Docks are added by plugins to provide space for controls related to an [`EditorPlugin`](EditorPlugin.md). The editor comes with a few built-in docks, such as the Scene dock, FileSystem dock, etc.

You can add a dock by using [`EditorPlugin.AddDock`](EditorPlugin.md). The dock can be customized by changing its properties.

```csharp
@tool
extends EditorPlugin

# Dock reference.
var dock

# Plugin initialization.
func _enter_tree():
dock = EditorDock.new()
dock.title = "My Dock"
dock.dock_icon = preload("./dock_icon.png")
dock.default_slot = EditorDock.DOCK_SLOT_RIGHT_UL
var dock_content = preload("./dock_content.tscn").instantiate()
dock.add_child(dock_content)
add_dock(dock)

# Plugin clean-up.
func _exit_tree():
remove_dock(dock)
dock.queue_free()
dock = null

```

## 构造函数

### EditorDock

```csharp
public EditorDock()
```

## 属性

### AvailableLayouts

```csharp
public EditorDock.DockLayout AvailableLayouts { get; set; }
```

引擎名：`available_layouts`

The available layouts for this dock, as a bitmask. By default, the dock allows vertical and floating layouts.

### Closable

```csharp
public bool Closable { get; set; }
```

引擎名：`closable`

If `true`, the dock can be closed with the Close button in the context popup. Docks with `Global` enabled are always closable.

### DefaultSlot

```csharp
public EditorDock.DockSlot DefaultSlot { get; set; }
```

引擎名：`default_slot`

The default dock slot used when adding the dock with [`EditorPlugin.AddDock`](EditorPlugin.md).

After the dock is added, it can be moved to a different slot and the editor will automatically remember its position between sessions. If you remove and re-add the dock, it will be reset to default.

### DockIcon

```csharp
public Texture2D DockIcon { get; set; }
```

引擎名：`dock_icon`

The icon for the dock, as a texture. If specified, it will override `IconName`.

### DockShortcut

```csharp
public Shortcut DockShortcut { get; set; }
```

引擎名：`dock_shortcut`

The shortcut used to open the dock.

### ForceShowIcon

```csharp
public bool ForceShowIcon { get; set; }
```

引擎名：`force_show_icon`

If `true`, the dock will always display an icon, regardless of `EditorSettings.interface/editor/docks/dock_tab_style` or `EditorSettings.interface/editor/docks/bottom_dock_tab_style`.

### Global

```csharp
public bool Global { get; set; }
```

引擎名：`global`

If `true`, the dock appears in the **Editor > Editor Docks** menu and can be closed. Non-global docks can still be closed using `Close()` or when `Closable` is `true`.

### IconName

```csharp
public StringName IconName { get; set; }
```

引擎名：`icon_name`

The icon for the dock, as a name from the `EditorIcons` theme type in the editor theme. You can find the list of available icons [here](https://godot-editor-icons.github.io/).

### LayoutKey

```csharp
public string LayoutKey { get; set; }
```

引擎名：`layout_key`

The key representing this dock in the editor's layout file. If empty, the dock's displayed name will be used instead.

### Title

```csharp
public string Title { get; set; }
```

引擎名：`title`

The title of the dock's tab. If empty, the dock's [`Node.Name`](Node.md) will be used. If the name is auto-generated (contains `@`), the first child's name will be used instead.

### TitleColor

```csharp
public Color TitleColor { get; set; }
```

引擎名：`title_color`

The color of the dock tab's title. If its alpha is `0.0`, the default font color will be used.

### Transient

```csharp
public bool Transient { get; set; }
```

引擎名：`transient`

If `true`, the dock is not automatically opened or closed when loading an editor layout, only moved. It also can't be opened using a shortcut. This is meant for docks that are opened and closed in specific cases, such as when selecting a [`TileMap`](TileMap.md) or [`AnimationTree`](AnimationTree.md) node.

## 方法

### Close

```csharp
public void Close()
```

引擎名：`close`

Closes the dock, making its tab hidden.

### GetAvailableLayouts

```csharp
public EditorDock.DockLayout GetAvailableLayouts()
```

引擎名：`get_available_layouts`

### GetDefaultSlot

```csharp
public EditorDock.DockSlot GetDefaultSlot()
```

引擎名：`get_default_slot`

### GetDockIcon

```csharp
public Texture2D GetDockIcon()
```

引擎名：`get_dock_icon`

### GetDockShortcut

```csharp
public Shortcut GetDockShortcut()
```

引擎名：`get_dock_shortcut`

### GetForceShowIcon

```csharp
public bool GetForceShowIcon()
```

引擎名：`get_force_show_icon`

### GetIconName

```csharp
public StringName GetIconName()
```

引擎名：`get_icon_name`

### GetLayoutKey

```csharp
public string GetLayoutKey()
```

引擎名：`get_layout_key`

### GetTitle

```csharp
public string GetTitle()
```

引擎名：`get_title`

### GetTitleColor

```csharp
public Color GetTitleColor()
```

引擎名：`get_title_color`

### IsClosable

```csharp
public bool IsClosable()
```

引擎名：`is_closable`

### IsGlobal

```csharp
public bool IsGlobal()
```

引擎名：`is_global`

### IsTransient

```csharp
public bool IsTransient()
```

引擎名：`is_transient`

### MakeVisible

```csharp
public void MakeVisible()
```

引擎名：`make_visible`

Focuses the dock's tab (or window if it's floating). If the dock was closed, it will be opened. If it's a bottom dock, makes the bottom panel visible.

### Open

```csharp
public void Open()
```

引擎名：`open`

Opens the dock. It will appear in the last used dock slot. If the dock has no default slot, it will be opened floating.

**Note:** This does not focus the dock. If you want to open and focus the dock, use `MakeVisible()`.

### SetAvailableLayouts

```csharp
public void SetAvailableLayouts(EditorDock.DockLayout layouts)
```

引擎名：`set_available_layouts`

### SetClosable

```csharp
public void SetClosable(bool closable)
```

引擎名：`set_closable`

### SetDefaultSlot

```csharp
public void SetDefaultSlot(EditorDock.DockSlot slot)
```

引擎名：`set_default_slot`

### SetDockIcon

```csharp
public void SetDockIcon(Texture2D icon)
```

引擎名：`set_dock_icon`

### SetDockShortcut

```csharp
public void SetDockShortcut(Shortcut shortcut)
```

引擎名：`set_dock_shortcut`

### SetForceShowIcon

```csharp
public void SetForceShowIcon(bool force)
```

引擎名：`set_force_show_icon`

### SetGlobal

```csharp
public void SetGlobal(bool global)
```

引擎名：`set_global`

### SetIconName

```csharp
public void SetIconName(StringName iconName)
```

引擎名：`set_icon_name`

### SetLayoutKey

```csharp
public void SetLayoutKey(string layoutKey)
```

引擎名：`set_layout_key`

### SetTitle

```csharp
public void SetTitle(string title)
```

引擎名：`set_title`

### SetTitleColor

```csharp
public void SetTitleColor(Color color)
```

引擎名：`set_title_color`

### SetTransient

```csharp
public void SetTransient(bool transient)
```

引擎名：`set_transient`

### _LoadLayoutFromConfig

```csharp
public virtual void _LoadLayoutFromConfig(ConfigFile config, string section)
```

引擎名：`_load_layout_from_config`

Implement this method to handle loading this dock's layout. It's equivalent to [`EditorPlugin._SetWindowLayout()`](EditorPlugin.md). `section` is a unique section based on `LayoutKey`.

### _SaveLayoutToConfig

```csharp
public virtual void _SaveLayoutToConfig(ConfigFile config, string section)
```

引擎名：`_save_layout_to_config`

Implement this method to handle saving this dock's layout. It's equivalent to [`EditorPlugin._GetWindowLayout()`](EditorPlugin.md). `section` is a unique section based on `LayoutKey`.

### _UpdateLayout

```csharp
public virtual void _UpdateLayout(int layout)
```

引擎名：`_update_layout`

Implement this method to handle the layout switching for this dock. `layout` is one of the [`EditorDock.DockLayout`](EditorDock.md) constants.

```csharp
func _update_layout(layout):
box_container.vertical = (layout == DOCK_LAYOUT_VERTICAL)

```

## 事件（信号）

### Closed

```csharp
public event Action Closed
```

引擎名：`closed`

Emitted when the dock is closed with the Close button in the context popup, before it's removed from its parent. See `Closable`.

### Opened

```csharp
public event Action Opened
```

引擎名：`opened`

Emitted when the dock is opened via the Editor > Editor Docks menu, before it's made visible.

## DockLayout

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `Vertical = 1`：Allows placing the dock in the vertical dock slots on either side of the editor.
- `Horizontal = 2`：Allows placing the dock in the horizontal dock slots at the bottom.
- `Floating = 4`：Allows making the dock floating (opened as a separate window).
- `All = 7`：Allows placing the dock in all available slots.

## DockSlot

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `None = -1`：The dock is closed.
- `LeftUl = 0`：Dock slot, left side, upper-left (empty in default layout).
- `LeftBl = 1`：Dock slot, left side, bottom-left (empty in default layout).
- `LeftUr = 2`：Dock slot, left side, upper-right (in default layout includes Scene and Import docks).
- `LeftBr = 3`：Dock slot, left side, bottom-right (in default layout includes FileSystem and History docks).
- `RightUl = 4`：Dock slot, right side, upper-left (in default layout includes Inspector, Signal, and Group docks).
- `RightBl = 5`：Dock slot, right side, bottom-left (empty in default layout).
- `RightUr = 6`：Dock slot, right side, upper-right (empty in default layout).
- `RightBr = 7`：Dock slot, right side, bottom-right (empty in default layout).
- `Bottom = 8`：Bottom panel.
- `BottomL = 9`：Dock slot at the bottom, below bottom panel, on the left side.
- `BottomR = 10`：Dock slot at the bottom, below bottom panel, on the right side.
- `Max = 11`：Represents the size of the `EditorDock.DockSlot` enum.
