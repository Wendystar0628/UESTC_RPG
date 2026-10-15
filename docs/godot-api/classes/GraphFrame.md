<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GraphFrame

命名空间 `Godot`；class；继承 [`GraphElement`](GraphElement.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

GraphFrame is a special [`GraphElement`](GraphElement.md) to which other [`GraphElement`](GraphElement.md)s can be attached. It can be configured to automatically resize to enclose all attached [`GraphElement`](GraphElement.md)s. If the frame is moved, all the attached [`GraphElement`](GraphElement.md)s inside it will be moved as well.

A GraphFrame is always kept behind the connection layer and other [`GraphElement`](GraphElement.md)s inside a [`GraphEdit`](GraphEdit.md).

## 构造函数

### GraphFrame

```csharp
public GraphFrame()
```

## 属性

### AutoshrinkEnabled

```csharp
public bool AutoshrinkEnabled { get; set; }
```

引擎名：`autoshrink_enabled`

If `true`, the frame's rect will be adjusted automatically to enclose all attached [`GraphElement`](GraphElement.md)s.

### AutoshrinkMargin

```csharp
public int AutoshrinkMargin { get; set; }
```

引擎名：`autoshrink_margin`

The margin around the attached nodes that is used to calculate the size of the frame when `AutoshrinkEnabled` is `true`.

### DragMargin

```csharp
public int DragMargin { get; set; }
```

引擎名：`drag_margin`

The margin inside the frame that can be used to drag the frame.

### TintColor

```csharp
public Color TintColor { get; set; }
```

引擎名：`tint_color`

The color of the frame when `TintColorEnabled` is `true`.

### TintColorEnabled

```csharp
public bool TintColorEnabled { get; set; }
```

引擎名：`tint_color_enabled`

If `true`, the tint color will be used to tint the frame.

### Title

```csharp
public string Title { get; set; }
```

引擎名：`title`

Title of the frame.

## 方法

### GetAutoshrinkMargin

```csharp
public int GetAutoshrinkMargin()
```

引擎名：`get_autoshrink_margin`

### GetDragMargin

```csharp
public int GetDragMargin()
```

引擎名：`get_drag_margin`

### GetTintColor

```csharp
public Color GetTintColor()
```

引擎名：`get_tint_color`

### GetTitle

```csharp
public string GetTitle()
```

引擎名：`get_title`

### GetTitlebarHBox

```csharp
public HBoxContainer GetTitlebarHBox()
```

Returns the [`HBoxContainer`](HBoxContainer.md) used for the title bar, only containing a [`Label`](Label.md) for displaying the title by default.

This can be used to add custom controls to the title bar such as option or close buttons.

### IsAutoshrinkEnabled

```csharp
public bool IsAutoshrinkEnabled()
```

引擎名：`is_autoshrink_enabled`

### IsTintColorEnabled

```csharp
public bool IsTintColorEnabled()
```

引擎名：`is_tint_color_enabled`

### SetAutoshrinkEnabled

```csharp
public void SetAutoshrinkEnabled(bool shrink)
```

引擎名：`set_autoshrink_enabled`

### SetAutoshrinkMargin

```csharp
public void SetAutoshrinkMargin(int autoshrinkMargin)
```

引擎名：`set_autoshrink_margin`

### SetDragMargin

```csharp
public void SetDragMargin(int dragMargin)
```

引擎名：`set_drag_margin`

### SetTintColor

```csharp
public void SetTintColor(Color color)
```

引擎名：`set_tint_color`

### SetTintColorEnabled

```csharp
public void SetTintColorEnabled(bool enable)
```

引擎名：`set_tint_color_enabled`

### SetTitle

```csharp
public void SetTitle(string title)
```

引擎名：`set_title`

## 事件（信号）

### AutoshrinkChanged

```csharp
public event Action AutoshrinkChanged
```

引擎名：`autoshrink_changed`

Emitted when `AutoshrinkEnabled` or `AutoshrinkMargin` changes.
