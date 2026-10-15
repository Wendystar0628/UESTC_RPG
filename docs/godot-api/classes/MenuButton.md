<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MenuButton

命名空间 `Godot`；class；继承 [`Button`](Button.md) → [`BaseButton`](BaseButton.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A button that brings up a [`PopupMenu`](PopupMenu.md) when clicked. To create new items inside this [`PopupMenu`](PopupMenu.md), use `get_popup().add_item("My Item Name")`. You can also create them directly from Godot editor's inspector.

See also [`BaseButton`](BaseButton.md) which contains common properties and methods associated with this node.

## 构造函数

### MenuButton

```csharp
public MenuButton()
```

## 属性

### ItemCount

```csharp
public int ItemCount { get; set; }
```

引擎名：`item_count`

The number of items currently in the list.

### SwitchOnHover

```csharp
public bool SwitchOnHover { get; set; }
```

引擎名：`switch_on_hover`

If `true`, when the cursor hovers above another `MenuButton` within the same parent which also has `SwitchOnHover` enabled, it will close the current `MenuButton` and open the other one.

## 方法

### GetItemCount

```csharp
public int GetItemCount()
```

引擎名：`get_item_count`

### GetPopup

```csharp
public PopupMenu GetPopup()
```

引擎名：`get_popup`

Returns the [`PopupMenu`](PopupMenu.md) contained in this button.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`Window.Visible`](Window.md) property.

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

### SetItemCount

```csharp
public void SetItemCount(int count)
```

引擎名：`set_item_count`

### SetSwitchOnHover

```csharp
public void SetSwitchOnHover(bool enable)
```

引擎名：`set_switch_on_hover`

### ShowPopup

```csharp
public void ShowPopup()
```

引擎名：`show_popup`

Adjusts popup position and sizing for the `MenuButton`, then shows the [`PopupMenu`](PopupMenu.md). Prefer this over using `get_popup().popup()`.

## 事件（信号）

### AboutToPopup

```csharp
public event Action AboutToPopup
```

引擎名：`about_to_popup`

Emitted when the [`PopupMenu`](PopupMenu.md) of this MenuButton is about to show.
