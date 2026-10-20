<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TouchScreenButton

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

TouchScreenButton allows you to create on-screen buttons for touch devices. It's intended for gameplay use, such as a unit you have to touch to move. Unlike [`Button`](Button.md), TouchScreenButton supports multitouch out of the box. Several TouchScreenButtons can be pressed at the same time with touch input.

This node inherits from [`Node2D`](Node2D.md). Unlike with [`Control`](Control.md) nodes, you cannot set anchors on it. If you want to create menus or user interfaces, you may want to use [`Button`](Button.md) nodes instead. To make button nodes react to touch events, you can enable `ProjectSettings.input_devices/pointing/emulate_mouse_from_touch` in the Project Settings.

You can configure TouchScreenButton to be visible only on touch devices, helping you develop your game both for desktop and mobile devices.

## 构造函数

### TouchScreenButton

```csharp
public TouchScreenButton()
```

## 属性

### Action

```csharp
public string Action { get; set; }
```

引擎名：`action`

The button's action. Actions can be handled with [`InputEventAction`](InputEventAction.md).

### Bitmask

```csharp
public Bitmap Bitmask { get; set; }
```

引擎名：`bitmask`

The button's bitmask.

### PassbyPress

```csharp
public bool PassbyPress { get; set; }
```

引擎名：`passby_press`

If `true`, the `Pressed` and `Released` signals are emitted whenever a pressed finger goes in and out of the button, even if the pressure started outside the active area of the button.

**Note:** This is a "pass-by" (not "bypass") press mode.

### Shape

```csharp
public Shape2D Shape { get; set; }
```

引擎名：`shape`

The button's shape.

### ShapeCentered

```csharp
public bool ShapeCentered { get; set; }
```

引擎名：`shape_centered`

If `true`, the button's shape is centered in the provided texture. If no texture is used, this property has no effect.

### ShapeVisible

```csharp
public bool ShapeVisible { get; set; }
```

引擎名：`shape_visible`

If `true`, the button's shape is visible in the editor.

### TextureNormal

```csharp
public Texture2D TextureNormal { get; set; }
```

引擎名：`texture_normal`

The button's texture for the normal state.

### TexturePressed

```csharp
public Texture2D TexturePressed { get; set; }
```

引擎名：`texture_pressed`

The button's texture for the pressed state.

### VisibilityMode

```csharp
public TouchScreenButton.VisibilityModeEnum VisibilityMode { get; set; }
```

引擎名：`visibility_mode`

The button's visibility mode.

## 方法

### GetAction

```csharp
public string GetAction()
```

引擎名：`get_action`

### GetBitmask

```csharp
public Bitmap GetBitmask()
```

引擎名：`get_bitmask`

### GetShape

```csharp
public Shape2D GetShape()
```

引擎名：`get_shape`

### GetTextureNormal

```csharp
public Texture2D GetTextureNormal()
```

引擎名：`get_texture_normal`

### GetTexturePressed

```csharp
public Texture2D GetTexturePressed()
```

引擎名：`get_texture_pressed`

### GetVisibilityMode

```csharp
public TouchScreenButton.VisibilityModeEnum GetVisibilityMode()
```

引擎名：`get_visibility_mode`

### IsPassbyPressEnabled

```csharp
public bool IsPassbyPressEnabled()
```

引擎名：`is_passby_press_enabled`

### IsPressed

```csharp
public bool IsPressed()
```

引擎名：`is_pressed`

Returns `true` if this button is currently pressed.

### IsShapeCentered

```csharp
public bool IsShapeCentered()
```

引擎名：`is_shape_centered`

### IsShapeVisible

```csharp
public bool IsShapeVisible()
```

引擎名：`is_shape_visible`

### SetAction

```csharp
public void SetAction(string action)
```

引擎名：`set_action`

### SetBitmask

```csharp
public void SetBitmask(Bitmap bitmask)
```

引擎名：`set_bitmask`

### SetPassbyPress

```csharp
public void SetPassbyPress(bool enabled)
```

引擎名：`set_passby_press`

### SetShape

```csharp
public void SetShape(Shape2D shape)
```

引擎名：`set_shape`

### SetShapeCentered

```csharp
public void SetShapeCentered(bool bool)
```

引擎名：`set_shape_centered`

### SetShapeVisible

```csharp
public void SetShapeVisible(bool bool)
```

引擎名：`set_shape_visible`

### SetTextureNormal

```csharp
public void SetTextureNormal(Texture2D texture)
```

引擎名：`set_texture_normal`

### SetTexturePressed

```csharp
public void SetTexturePressed(Texture2D texture)
```

引擎名：`set_texture_pressed`

### SetVisibilityMode

```csharp
public void SetVisibilityMode(TouchScreenButton.VisibilityModeEnum mode)
```

引擎名：`set_visibility_mode`

## 事件（信号）

### Pressed

```csharp
public event Action Pressed
```

引擎名：`pressed`

Emitted when the button is pressed (down).

### Released

```csharp
public event Action Released
```

引擎名：`released`

Emitted when the button is released (up).

## VisibilityModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Always = 0`：Always visible.
- `TouchscreenOnly = 1`：Visible on touch screens only.
