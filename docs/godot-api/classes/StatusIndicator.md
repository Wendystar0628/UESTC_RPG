<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StatusIndicator

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

## 构造函数

### StatusIndicator

```csharp
public StatusIndicator()
```

## 属性

### Icon

```csharp
public Texture2D Icon { get; set; }
```

引擎名：`icon`

Status indicator icon.

### Menu

```csharp
public NodePath Menu { get; set; }
```

引擎名：`menu`

Status indicator native popup menu. If this is set, the `Pressed` signal is not emitted.

**Note:** Native popup is only supported if [`NativeMenu`](NativeMenu.md) supports [`NativeMenu.Feature.PopupMenu`](NativeMenu.md) feature.

### Tooltip

```csharp
public string Tooltip { get; set; }
```

引擎名：`tooltip`

Status indicator tooltip.

### Visible

```csharp
public bool Visible { get; set; }
```

引擎名：`visible`

If `true`, the status indicator is visible.

## 方法

### GetIcon

```csharp
public Texture2D GetIcon()
```

引擎名：`get_icon`

### GetMenu

```csharp
public NodePath GetMenu()
```

引擎名：`get_menu`

### GetRect

```csharp
public Rect2 GetRect()
```

引擎名：`get_rect`

Returns the status indicator rectangle in screen coordinates. If this status indicator is not visible, returns an empty [`Rect2`](Rect2.md).

### GetTooltip

```csharp
public string GetTooltip()
```

引擎名：`get_tooltip`

### IsVisible

```csharp
public bool IsVisible()
```

引擎名：`is_visible`

### SetIcon

```csharp
public void SetIcon(Texture2D texture)
```

引擎名：`set_icon`

### SetMenu

```csharp
public void SetMenu(NodePath menu)
```

引擎名：`set_menu`

### SetTooltip

```csharp
public void SetTooltip(string tooltip)
```

引擎名：`set_tooltip`

### SetVisible

```csharp
public void SetVisible(bool visible)
```

引擎名：`set_visible`

## 事件（信号）

### Pressed

```csharp
public event StatusIndicator.PressedEventHandler Pressed
```

引擎名：`pressed`

Emitted when the status indicator is pressed.

## PressedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`StatusIndicator.Pressed`](StatusIndicator.md) event of a [`StatusIndicator`](StatusIndicator.md) class.

### 签名

#### Invoke

```csharp
delegate void PressedEventHandler(long mouseButton, Vector2I mousePosition)
```
