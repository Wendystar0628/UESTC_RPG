<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# FoldableContainer

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A container that can be expanded/collapsed, with a title that can be filled with controls, such as buttons. This is also called an accordion.

The title can be positioned at the top or bottom of the container. The container can be expanded or collapsed by clicking the title or by pressing `ui_accept` when focused. Child control nodes are hidden when the container is collapsed. Ignores non-control children.

A FoldableContainer can be grouped with other FoldableContainers so that only one of them can be opened at a time; see `FoldableGroup` and [`FoldableGroup`](FoldableGroup.md).

## 构造函数

### FoldableContainer

```csharp
public FoldableContainer()
```

## 属性

### FoldableGroup

```csharp
public FoldableGroup FoldableGroup { get; set; }
```

引擎名：`foldable_group`

The [`FoldableGroup`](FoldableGroup.md) associated with the container. When multiple `FoldableContainer` nodes share the same group, only one of them is allowed to be unfolded.

### Folded

```csharp
public bool Folded { get; set; }
```

引擎名：`folded`

If `true`, the container will become folded and will hide all its children.

### Language

```csharp
public string Language { get; set; }
```

引擎名：`language`

Language code used for text shaping algorithms. If left empty, the current locale is used instead.

### Title

```csharp
public string Title { get; set; }
```

引擎名：`title`

The container's title text.

### TitleAlignment

```csharp
public HorizontalAlignment TitleAlignment { get; set; }
```

引擎名：`title_alignment`

Title's horizontal text alignment.

### TitlePosition

```csharp
public FoldableContainer.TitlePositionEnum TitlePosition { get; set; }
```

引擎名：`title_position`

Title's position.

### TitleTextDirection

```csharp
public Control.TextDirection TitleTextDirection { get; set; }
```

引擎名：`title_text_direction`

Title text writing direction.

### TitleTextOverrunBehavior

```csharp
public TextServer.OverrunBehavior TitleTextOverrunBehavior { get; set; }
```

引擎名：`title_text_overrun_behavior`

Defines the behavior of the title when the text is longer than the available space.

## 方法

### AddTitleBarControl

```csharp
public void AddTitleBarControl(Control control)
```

引擎名：`add_title_bar_control`

Adds a [`Control`](Control.md) that will be placed next to the container's title, obscuring the clickable area. Prime usage is adding [`Button`](Button.md) nodes, but it can be any [`Control`](Control.md).

The control will be added as a child of this container and removed from previous parent if necessary. The controls will be placed aligned to the right, with the first added control being the leftmost one.

### Expand

```csharp
public void Expand()
```

引擎名：`expand`

Expands the container and emits `FoldingChanged`.

### Fold

```csharp
public void Fold()
```

引擎名：`fold`

Folds the container and emits `FoldingChanged`.

### GetFoldableGroup

```csharp
public FoldableGroup GetFoldableGroup()
```

引擎名：`get_foldable_group`

### GetLanguage

```csharp
public string GetLanguage()
```

引擎名：`get_language`

### GetTitle

```csharp
public string GetTitle()
```

引擎名：`get_title`

### GetTitleAlignment

```csharp
public HorizontalAlignment GetTitleAlignment()
```

引擎名：`get_title_alignment`

### GetTitlePosition

```csharp
public FoldableContainer.TitlePositionEnum GetTitlePosition()
```

引擎名：`get_title_position`

### GetTitleTextDirection

```csharp
public Control.TextDirection GetTitleTextDirection()
```

引擎名：`get_title_text_direction`

### GetTitleTextOverrunBehavior

```csharp
public TextServer.OverrunBehavior GetTitleTextOverrunBehavior()
```

引擎名：`get_title_text_overrun_behavior`

### IsFolded

```csharp
public bool IsFolded()
```

引擎名：`is_folded`

### RemoveTitleBarControl

```csharp
public void RemoveTitleBarControl(Control control)
```

引擎名：`remove_title_bar_control`

Removes a [`Control`](Control.md) added with `AddTitleBarControl()`. The node is not freed automatically, you need to use [`Node.QueueFree`](Node.md).

### SetFoldableGroup

```csharp
public void SetFoldableGroup(FoldableGroup buttonGroup)
```

引擎名：`set_foldable_group`

### SetFolded

```csharp
public void SetFolded(bool folded)
```

引擎名：`set_folded`

### SetLanguage

```csharp
public void SetLanguage(string language)
```

引擎名：`set_language`

### SetTitle

```csharp
public void SetTitle(string text)
```

引擎名：`set_title`

### SetTitleAlignment

```csharp
public void SetTitleAlignment(HorizontalAlignment alignment)
```

引擎名：`set_title_alignment`

### SetTitlePosition

```csharp
public void SetTitlePosition(FoldableContainer.TitlePositionEnum titlePosition)
```

引擎名：`set_title_position`

### SetTitleTextDirection

```csharp
public void SetTitleTextDirection(Control.TextDirection textDirection)
```

引擎名：`set_title_text_direction`

### SetTitleTextOverrunBehavior

```csharp
public void SetTitleTextOverrunBehavior(TextServer.OverrunBehavior overrunBehavior)
```

引擎名：`set_title_text_overrun_behavior`

## 事件（信号）

### FoldingChanged

```csharp
public event FoldableContainer.FoldingChangedEventHandler FoldingChanged
```

引擎名：`folding_changed`

Emitted when the container is folded/expanded.

## FoldingChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`FoldableContainer.FoldingChanged`](FoldableContainer.md) event of a [`FoldableContainer`](FoldableContainer.md) class.

### 签名

#### Invoke

```csharp
delegate void FoldingChangedEventHandler(bool isFolded)
```

## TitlePositionEnum

命名空间 `Godot`；enum

### 枚举值

- `Top = 0`：Makes the title appear at the top of the container.
- `Bottom = 1`：Makes the title appear at the bottom of the container.
