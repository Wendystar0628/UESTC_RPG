<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ConfirmationDialog

命名空间 `Godot`；class；继承 [`AcceptDialog`](AcceptDialog.md) → [`Window`](Window.md) → [`Viewport`](Viewport.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A dialog used for confirmation of actions. This window is similar to [`AcceptDialog`](AcceptDialog.md), but pressing its Cancel button can have a different outcome from pressing the OK button. The order of the two buttons varies depending on the host OS.

To get cancel action, you can use:

```csharp
GetCancelButton().Pressed += OnCanceled;

```

**Note:** [`AcceptDialog`](AcceptDialog.md) is invisible by default. To make it visible, call one of the `popup_*` methods from [`Window`](Window.md) on the node, such as [`Window.PopupCenteredClamped`](Window.md).

## 构造函数

### ConfirmationDialog

```csharp
public ConfirmationDialog()
```

## 属性

### CancelButtonText

```csharp
public string CancelButtonText { get; set; }
```

引擎名：`cancel_button_text`

The text displayed by the cancel button (see `GetCancelButton()`).

## 方法

### GetCancelButton

```csharp
public Button GetCancelButton()
```

引擎名：`get_cancel_button`

Returns the cancel button.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`CanvasItem.Visible`](CanvasItem.md) property.

### GetCancelButtonText

```csharp
public string GetCancelButtonText()
```

引擎名：`get_cancel_button_text`

### SetCancelButtonText

```csharp
public void SetCancelButtonText(string text)
```

引擎名：`set_cancel_button_text`
