<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ColorPickerButton

命名空间 `Godot`；class；继承 [`Button`](Button.md) → [`BaseButton`](BaseButton.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Encapsulates a [`ColorPicker`](ColorPicker.md), making it accessible by pressing a button. Pressing the button will toggle the [`ColorPicker`](ColorPicker.md)'s visibility.

See also [`BaseButton`](BaseButton.md) which contains common properties and methods associated with this node.

**Note:** By default, the button may not be wide enough for the color preview swatch to be visible. Make sure to set [`Control.CustomMinimumSize`](Control.md) to a big enough value to give the button enough space.

## 构造函数

### ColorPickerButton

```csharp
public ColorPickerButton()
```

## 属性

### Color

```csharp
public Color Color { get; set; }
```

引擎名：`color`

The currently selected color.

### EditAlpha

```csharp
public bool EditAlpha { get; set; }
```

引擎名：`edit_alpha`

If `true`, the alpha channel in the displayed [`ColorPicker`](ColorPicker.md) will be visible.

### EditIntensity

```csharp
public bool EditIntensity { get; set; }
```

引擎名：`edit_intensity`

If `true`, the intensity slider in the displayed [`ColorPicker`](ColorPicker.md) will be visible.

## 方法

### GetPickColor

```csharp
public Color GetPickColor()
```

引擎名：`get_pick_color`

### GetPicker

```csharp
public ColorPicker GetPicker()
```

引擎名：`get_picker`

Returns the [`ColorPicker`](ColorPicker.md) that this node toggles.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`CanvasItem.Visible`](CanvasItem.md) property.

### GetPopup

```csharp
public PopupPanel GetPopup()
```

引擎名：`get_popup`

Returns the control's [`PopupPanel`](PopupPanel.md) which allows you to connect to popup signals. This allows you to handle events when the ColorPicker is shown or hidden.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`Window.Visible`](Window.md) property.

### IsEditingAlpha

```csharp
public bool IsEditingAlpha()
```

引擎名：`is_editing_alpha`

### IsEditingIntensity

```csharp
public bool IsEditingIntensity()
```

引擎名：`is_editing_intensity`

### SetEditAlpha

```csharp
public void SetEditAlpha(bool show)
```

引擎名：`set_edit_alpha`

### SetEditIntensity

```csharp
public void SetEditIntensity(bool show)
```

引擎名：`set_edit_intensity`

### SetPickColor

```csharp
public void SetPickColor(Color color)
```

引擎名：`set_pick_color`

## 事件（信号）

### ColorChanged

```csharp
public event ColorPickerButton.ColorChangedEventHandler ColorChanged
```

引擎名：`color_changed`

Emitted when the color changes.

### PickerCreated

```csharp
public event Action PickerCreated
```

引擎名：`picker_created`

Emitted when the [`ColorPicker`](ColorPicker.md) is created (the button is pressed for the first time).

### PopupClosed

```csharp
public event Action PopupClosed
```

引擎名：`popup_closed`

Emitted when the [`ColorPicker`](ColorPicker.md) is closed.

## ColorChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`ColorPickerButton.ColorChanged`](ColorPickerButton.md) event of a [`ColorPickerButton`](ColorPickerButton.md) class.

### 签名

#### Invoke

```csharp
delegate void ColorChangedEventHandler(Color color)
```
