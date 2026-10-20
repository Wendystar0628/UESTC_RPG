<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AcceptDialog

命名空间 `Godot`；class；继承 [`Window`](Window.md) → [`Viewport`](Viewport.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

The default use of `AcceptDialog` is to allow it to only be accepted or closed, with the same result. However, the `Confirmed` and `Canceled` signals allow to make the two actions different, and the `AddButton()` method allows to add custom buttons and actions.

**Note:** `AcceptDialog` is invisible by default. To make it visible, call one of the `popup_*` methods from [`Window`](Window.md) on the node, such as [`Window.PopupCenteredClamped`](Window.md).

## 构造函数

### AcceptDialog

```csharp
public AcceptDialog()
```

## 属性

### DialogAutowrap

```csharp
public bool DialogAutowrap { get; set; }
```

引擎名：`dialog_autowrap`

Sets autowrapping for the text in the dialog.

### DialogCloseOnEscape

```csharp
public bool DialogCloseOnEscape { get; set; }
```

引擎名：`dialog_close_on_escape`

If `true`, the dialog will be hidden when the `ui_close_dialog` action is pressed (by default, this action is bound to Escape, or Cmd + W on macOS).

### DialogHideOnOk

```csharp
public bool DialogHideOnOk { get; set; }
```

引擎名：`dialog_hide_on_ok`

If `true`, the dialog is hidden when the OK button is pressed. You can set it to `false` if you want to do e.g. input validation when receiving the `Confirmed` signal, and handle hiding the dialog in your own logic.

**Note:** Some nodes derived from this class can have a different default value, and potentially their own built-in logic overriding this setting. For example [`FileDialog`](FileDialog.md) defaults to `false`, and has its own input validation code that is called when you press OK, which eventually hides the dialog if the input is valid. As such, this property can't be used in [`FileDialog`](FileDialog.md) to disable hiding the dialog when pressing OK.

### DialogText

```csharp
public string DialogText { get; set; }
```

引擎名：`dialog_text`

The text displayed by the dialog.

### OkButtonText

```csharp
public string OkButtonText { get; set; }
```

引擎名：`ok_button_text`

The text displayed by the OK button (see `GetOkButton()`). If empty, a default text will be used.

## 方法

### AddButton

```csharp
public Button AddButton(string text, bool right = false, string action = "")
```

引擎名：`add_button`

Adds a button with label `text` and a custom `action` to the dialog and returns the created button.

If `action` is not empty, pressing the button will emit the `CustomAction` signal with the specified action string.

If `true`, `right` will place the button to the right of any sibling buttons.

You can use `RemoveButton()` method to remove a button created with this method from the dialog.

### AddCancelButton

```csharp
public Button AddCancelButton(string name)
```

引擎名：`add_cancel_button`

Adds a button with label `name` and a cancel action to the dialog and returns the created button.

You can use `RemoveButton()` method to remove a button created with this method from the dialog.

### GetCloseOnEscape

```csharp
public bool GetCloseOnEscape()
```

引擎名：`get_close_on_escape`

### GetHideOnOk

```csharp
public bool GetHideOnOk()
```

引擎名：`get_hide_on_ok`

### GetLabel

```csharp
public Label GetLabel()
```

引擎名：`get_label`

Returns the label used for built-in text.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`CanvasItem.Visible`](CanvasItem.md) property.

### GetOkButton

```csharp
public Button GetOkButton()
```

引擎名：`get_ok_button`

Returns the OK [`Button`](Button.md) instance.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`CanvasItem.Visible`](CanvasItem.md) property.

### GetOkButtonText

```csharp
public string GetOkButtonText()
```

引擎名：`get_ok_button_text`

### GetText

```csharp
public string GetText()
```

引擎名：`get_text`

### HasAutowrap

```csharp
public bool HasAutowrap()
```

引擎名：`has_autowrap`

### RegisterTextEnter

```csharp
public void RegisterTextEnter(Control _UnnamedArg0)
```

引擎名：`register_text_enter`

Registers a [`LineEdit`](LineEdit.md) in the dialog. When the enter key is pressed, the dialog will be accepted.

### RegisterTextEnter

```csharp
public void RegisterTextEnter(LineEdit lineEdit)
```

引擎名：`register_text_enter`

Registers a [`LineEdit`](LineEdit.md) in the dialog. When the enter key is pressed, the dialog will be accepted.

### RemoveButton

```csharp
public void RemoveButton(Button button)
```

引擎名：`remove_button`

Removes the `button` from the dialog. Does NOT free the `button`. The `button` must be a [`Button`](Button.md) added with `AddButton()` or `AddCancelButton()` method. After removal, pressing the `button` will no longer emit this dialog's `CustomAction` or `Canceled` signals.

### RemoveButton

```csharp
public void RemoveButton(Control quadrantSize)
```

引擎名：`remove_button`

Removes the `button` from the dialog. Does NOT free the `button`. The `button` must be a [`Button`](Button.md) added with `AddButton()` or `AddCancelButton()` method. After removal, pressing the `button` will no longer emit this dialog's `CustomAction` or `Canceled` signals.

### SetAutowrap

```csharp
public void SetAutowrap(bool autowrap)
```

引擎名：`set_autowrap`

### SetCloseOnEscape

```csharp
public void SetCloseOnEscape(bool enabled)
```

引擎名：`set_close_on_escape`

### SetHideOnOk

```csharp
public void SetHideOnOk(bool enabled)
```

引擎名：`set_hide_on_ok`

### SetOkButtonText

```csharp
public void SetOkButtonText(string text)
```

引擎名：`set_ok_button_text`

### SetText

```csharp
public void SetText(string text)
```

引擎名：`set_text`

## 事件（信号）

### Canceled

```csharp
public event Action Canceled
```

引擎名：`canceled`

Emitted when the dialog is closed or the button created with `AddCancelButton()` is pressed.

### Confirmed

```csharp
public event Action Confirmed
```

引擎名：`confirmed`

Emitted when the dialog is accepted, i.e. the OK button is pressed.

### CustomAction

```csharp
public event AcceptDialog.CustomActionEventHandler CustomAction
```

引擎名：`custom_action`

Emitted when a custom button with an action is pressed. See `AddButton()`.

## CustomActionEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AcceptDialog.CustomAction`](AcceptDialog.md) event of a [`AcceptDialog`](AcceptDialog.md) class.

### 签名

#### Invoke

```csharp
delegate void CustomActionEventHandler(StringName action)
```
