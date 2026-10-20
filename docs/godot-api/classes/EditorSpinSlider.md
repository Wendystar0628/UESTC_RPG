<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorSpinSlider

命名空间 `Godot`；class；继承 [`Range`](Range.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This [`Control`](Control.md) node is used in the editor's Inspector dock to allow editing of numeric values. Can be used with [`EditorInspectorPlugin`](EditorInspectorPlugin.md) to recreate the same behavior.

If the [`Range.Step`](Range.md) value is `1`, the `EditorSpinSlider` will display up/down arrows, similar to [`SpinBox`](SpinBox.md). If the [`Range.Step`](Range.md) value is not `1`, a slider will be displayed instead.

## 构造函数

### EditorSpinSlider

```csharp
public EditorSpinSlider()
```

## 属性

### ControlState

```csharp
public EditorSpinSlider.ControlStateEnum ControlState { get; set; }
```

引擎名：`control_state`

The state in which the control used to manipulate the value will be.

### DeferredDragMode

```csharp
public bool DeferredDragMode { get; set; }
```

引擎名：`deferred_drag_mode`

If `true`, changing via dragging is applied only at the end of the input (for example, when the user releases a mouse button).

### EditingInteger

```csharp
public bool EditingInteger { get; set; }
```

引擎名：`editing_integer`

If `true`, the `EditorSpinSlider` is considered to be editing an integer value. If `false`, the `EditorSpinSlider` is considered to be editing a floating-point value. This is used to determine whether a slider should be drawn by default. The slider is only drawn for floats; integers use up-down arrows similar to [`SpinBox`](SpinBox.md) instead, unless `ControlState` is set to [`EditorSpinSlider.ControlStateEnum.PreferSlider`](EditorSpinSlider.md). It will also use `EditorSettings.interface/inspector/integer_drag_speed` instead of `EditorSettings.interface/inspector/float_drag_speed` if the slider is available.

### Flat

```csharp
public bool Flat { get; set; }
```

引擎名：`flat`

If `true`, the slider will not draw background.

### HideSlider

```csharp
public bool HideSlider { get; set; }
```

引擎名：`hide_slider`

If `true`, the slider and up/down arrows are hidden.

### Label

```csharp
public string Label { get; set; }
```

引擎名：`label`

The text that displays to the left of the value.

### ReadOnly

```csharp
public bool ReadOnly { get; set; }
```

引擎名：`read_only`

If `true`, the slider can't be interacted with.

### Suffix

```csharp
public string Suffix { get; set; }
```

引擎名：`suffix`

The suffix to display after the value (in a faded color). This should generally be a plural word. You may have to use an abbreviation if the suffix is too long to be displayed.

## 方法

### GetControlState

```csharp
public EditorSpinSlider.ControlStateEnum GetControlState()
```

引擎名：`get_control_state`

### GetLabel

```csharp
public string GetLabel()
```

引擎名：`get_label`

### GetSuffix

```csharp
public string GetSuffix()
```

引擎名：`get_suffix`

### IsDeferredDragModeEnabled

```csharp
public bool IsDeferredDragModeEnabled()
```

引擎名：`is_deferred_drag_mode_enabled`

### IsEditingInteger

```csharp
public bool IsEditingInteger()
```

引擎名：`is_editing_integer`

### IsFlat

```csharp
public bool IsFlat()
```

引擎名：`is_flat`

### IsHidingSlider

```csharp
public bool IsHidingSlider()
```

引擎名：`is_hiding_slider`

### IsReadOnly

```csharp
public bool IsReadOnly()
```

引擎名：`is_read_only`

### SetControlState

```csharp
public void SetControlState(EditorSpinSlider.ControlStateEnum state)
```

引擎名：`set_control_state`

### SetDeferredDragModeEnabled

```csharp
public void SetDeferredDragModeEnabled(bool enabled = true)
```

引擎名：`set_deferred_drag_mode_enabled`

### SetEditingInteger

```csharp
public void SetEditingInteger(bool editingInteger)
```

引擎名：`set_editing_integer`

### SetFlat

```csharp
public void SetFlat(bool flat)
```

引擎名：`set_flat`

### SetHideSlider

```csharp
public void SetHideSlider(bool hideSlider)
```

引擎名：`set_hide_slider`

### SetLabel

```csharp
public void SetLabel(string label)
```

引擎名：`set_label`

### SetReadOnly

```csharp
public void SetReadOnly(bool readOnly)
```

引擎名：`set_read_only`

### SetSuffix

```csharp
public void SetSuffix(string suffix)
```

引擎名：`set_suffix`

## 事件（信号）

### Grabbed

```csharp
public event Action Grabbed
```

引擎名：`grabbed`

Emitted when the spinner/slider is grabbed.

### Ungrabbed

```csharp
public event Action Ungrabbed
```

引擎名：`ungrabbed`

Emitted when the spinner/slider is ungrabbed.

### UpdownPressed

```csharp
public event Action UpdownPressed
```

引擎名：`updown_pressed`

Emitted when the updown button is pressed.

### ValueFocusEntered

```csharp
public event Action ValueFocusEntered
```

引擎名：`value_focus_entered`

Emitted when the value form gains focus.

### ValueFocusExited

```csharp
public event Action ValueFocusExited
```

引擎名：`value_focus_exited`

Emitted when the value form loses focus.

## ControlStateEnum

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `Default = 0`：The type of control used will depend on the value of [`EditorSpinSlider.EditingInteger`](EditorSpinSlider.md).
- `PreferSlider = 1`：A slider will always be used, even if [`EditorSpinSlider.EditingInteger`](EditorSpinSlider.md) is enabled.
- `Hide = 2`：Neither the up-down arrows nor the slider will be shown.
