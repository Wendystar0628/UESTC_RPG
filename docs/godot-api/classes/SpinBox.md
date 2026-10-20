<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SpinBox

命名空间 `Godot`；class；继承 [`Range`](Range.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`SpinBox` is a numerical input text field. It allows entering integers and floating-point numbers. The `SpinBox` also has up and down buttons that can be clicked to increase or decrease the value. The value can also be changed by dragging the mouse up or down over the `SpinBox`'s arrows.

Additionally, mathematical expressions can be entered. These are evaluated when the user presses Enter while editing the `SpinBox`'s text field. This uses the [`Expression`](Expression.md) class to parse and evaluate the expression. The result of the expression is then set as the value of the `SpinBox`. Some examples of valid expressions are `5 + 2 * 3`, `pow(2, 4)`, and `PI + sin(0.5)`. Expressions are case-sensitive.

**Example:** Create a `SpinBox`, disable its context menu and set its text alignment to right.

```csharp
var spinBox = new SpinBox();
AddChild(spinBox);
var lineEdit = spinBox.GetLineEdit();
lineEdit.ContextMenuEnabled = false;
spinBox.AlignHorizontal = LineEdit.HorizontalAlignEnum.Right;

```

See [`Range`](Range.md) class for more options over the `SpinBox`.

**Note:** With the `SpinBox`'s context menu disabled, you can right-click the bottom half of the spinbox to set the value to its minimum, while right-clicking the top half sets the value to its maximum.

**Note:** `SpinBox` relies on an underlying [`LineEdit`](LineEdit.md) node. To theme a `SpinBox`'s background, add theme items for [`LineEdit`](LineEdit.md) and customize them. The [`LineEdit`](LineEdit.md) has the `SpinBoxInnerLineEdit` theme variation, so that you can give it a distinct appearance from regular [`LineEdit`](LineEdit.md)s.

**Note:** If you want to implement drag and drop for the underlying [`LineEdit`](LineEdit.md), you can use [`Control.SetDragForwarding`](Control.md) on the node returned by `GetLineEdit()`.

## 构造函数

### SpinBox

```csharp
public SpinBox()
```

## 属性

### Alignment

```csharp
public HorizontalAlignment Alignment { get; set; }
```

引擎名：`alignment`

Changes the alignment of the underlying [`LineEdit`](LineEdit.md).

### CustomArrowRound

```csharp
public bool CustomArrowRound { get; set; }
```

引擎名：`custom_arrow_round`

If `true`, the value will be rounded to a multiple of `CustomArrowStep` when interacting with the arrow buttons. Otherwise, increments the value by `CustomArrowStep` and then rounds it according to [`Range.Step`](Range.md).

### CustomArrowStep

```csharp
public double CustomArrowStep { get; set; }
```

引擎名：`custom_arrow_step`

If not `0`, sets the step when interacting with the arrow buttons of the `SpinBox`.

**Note:** [`Range.Value`](Range.md) will still be rounded to a multiple of [`Range.Step`](Range.md).

### Editable

```csharp
public bool Editable { get; set; }
```

引擎名：`editable`

If `true`, the `SpinBox` will be editable. Otherwise, it will be read only.

### Prefix

```csharp
public string Prefix { get; set; }
```

引擎名：`prefix`

Adds the specified prefix string before the numerical value of the `SpinBox`.

### SelectAllOnFocus

```csharp
public bool SelectAllOnFocus { get; set; }
```

引擎名：`select_all_on_focus`

If `true`, the `SpinBox` will select the whole text when the [`LineEdit`](LineEdit.md) gains focus. Clicking the up and down arrows won't trigger this behavior.

### Suffix

```csharp
public string Suffix { get; set; }
```

引擎名：`suffix`

Adds the specified suffix string after the numerical value of the `SpinBox`.

### UpdateOnTextChanged

```csharp
public bool UpdateOnTextChanged { get; set; }
```

引擎名：`update_on_text_changed`

Sets the value of the [`Range`](Range.md) for this `SpinBox` when the [`LineEdit`](LineEdit.md) text is *changed* instead of *submitted*. See [`LineEdit.TextChanged`](LineEdit.md) and [`LineEdit.TextSubmitted`](LineEdit.md).

**Note:** If set to `true`, this will interfere with entering mathematical expressions in the `SpinBox`. The `SpinBox` will try to evaluate the expression as you type, which means symbols like a trailing `+` are removed immediately by the expression being evaluated.

## 方法

### Apply

```csharp
public void Apply()
```

引擎名：`apply`

Applies the current value of this `SpinBox`. This is equivalent to pressing Enter while editing the [`LineEdit`](LineEdit.md) used by the `SpinBox`. This will cause [`LineEdit.TextSubmitted`](LineEdit.md) to be emitted and its currently contained expression to be evaluated.

### GetCustomArrowStep

```csharp
public double GetCustomArrowStep()
```

引擎名：`get_custom_arrow_step`

### GetHorizontalAlignment

```csharp
public HorizontalAlignment GetHorizontalAlignment()
```

引擎名：`get_horizontal_alignment`

### GetLineEdit

```csharp
public LineEdit GetLineEdit()
```

引擎名：`get_line_edit`

Returns the [`LineEdit`](LineEdit.md) instance from this `SpinBox`. You can use it to access properties and methods of [`LineEdit`](LineEdit.md).

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`CanvasItem.Visible`](CanvasItem.md) property.

### GetPrefix

```csharp
public string GetPrefix()
```

引擎名：`get_prefix`

### GetSuffix

```csharp
public string GetSuffix()
```

引擎名：`get_suffix`

### GetUpdateOnTextChanged

```csharp
public bool GetUpdateOnTextChanged()
```

引擎名：`get_update_on_text_changed`

### IsCustomArrowRounding

```csharp
public bool IsCustomArrowRounding()
```

引擎名：`is_custom_arrow_rounding`

### IsEditable

```csharp
public bool IsEditable()
```

引擎名：`is_editable`

### IsSelectAllOnFocus

```csharp
public bool IsSelectAllOnFocus()
```

引擎名：`is_select_all_on_focus`

### SetCustomArrowRound

```csharp
public void SetCustomArrowRound(bool round)
```

引擎名：`set_custom_arrow_round`

### SetCustomArrowStep

```csharp
public void SetCustomArrowStep(double arrowStep)
```

引擎名：`set_custom_arrow_step`

### SetEditable

```csharp
public void SetEditable(bool enabled)
```

引擎名：`set_editable`

### SetHorizontalAlignment

```csharp
public void SetHorizontalAlignment(HorizontalAlignment alignment)
```

引擎名：`set_horizontal_alignment`

### SetPrefix

```csharp
public void SetPrefix(string prefix)
```

引擎名：`set_prefix`

### SetSelectAllOnFocus

```csharp
public void SetSelectAllOnFocus(bool enabled)
```

引擎名：`set_select_all_on_focus`

### SetSuffix

```csharp
public void SetSuffix(string suffix)
```

引擎名：`set_suffix`

### SetUpdateOnTextChanged

```csharp
public void SetUpdateOnTextChanged(bool enabled)
```

引擎名：`set_update_on_text_changed`
