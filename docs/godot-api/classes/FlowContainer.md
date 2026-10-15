<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# FlowContainer

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A container that arranges its child controls horizontally or vertically and wraps them around at the borders. This is similar to how text in a book wraps around when no more words can fit on a line.

## 构造函数

### FlowContainer

```csharp
public FlowContainer()
```

## 属性

### Alignment

```csharp
public FlowContainer.AlignmentMode Alignment { get; set; }
```

引擎名：`alignment`

The alignment of the container's children (must be one of [`FlowContainer.AlignmentMode.Begin`](FlowContainer.md), [`FlowContainer.AlignmentMode.Center`](FlowContainer.md), or [`FlowContainer.AlignmentMode.End`](FlowContainer.md)).

### LastWrapAlignment

```csharp
public FlowContainer.LastWrapAlignmentMode LastWrapAlignment { get; set; }
```

引擎名：`last_wrap_alignment`

The wrap behavior of the last, partially filled row or column (must be one of [`FlowContainer.LastWrapAlignmentMode.Inherit`](FlowContainer.md), [`FlowContainer.LastWrapAlignmentMode.Begin`](FlowContainer.md), [`FlowContainer.LastWrapAlignmentMode.Center`](FlowContainer.md), or [`FlowContainer.LastWrapAlignmentMode.End`](FlowContainer.md)).

### ReverseFill

```csharp
public bool ReverseFill { get; set; }
```

引擎名：`reverse_fill`

If `true`, reverses fill direction. Horizontal `FlowContainer`s will fill rows bottom to top, vertical `FlowContainer`s will fill columns right to left.

When using a vertical `FlowContainer` with a right to left [`Control.LayoutDirection`](Control.md), columns will fill left to right instead.

### Vertical

```csharp
public bool Vertical { get; set; }
```

引擎名：`vertical`

If `true`, the `FlowContainer` will arrange its children vertically, rather than horizontally.

Can't be changed when using [`HFlowContainer`](HFlowContainer.md) and [`VFlowContainer`](VFlowContainer.md).

## 方法

### GetAlignment

```csharp
public FlowContainer.AlignmentMode GetAlignment()
```

引擎名：`get_alignment`

### GetLastWrapAlignment

```csharp
public FlowContainer.LastWrapAlignmentMode GetLastWrapAlignment()
```

引擎名：`get_last_wrap_alignment`

### GetLineCount

```csharp
public int GetLineCount()
```

引擎名：`get_line_count`

Returns the current line count.

### IsReverseFill

```csharp
public bool IsReverseFill()
```

引擎名：`is_reverse_fill`

### IsVertical

```csharp
public bool IsVertical()
```

引擎名：`is_vertical`

### SetAlignment

```csharp
public void SetAlignment(FlowContainer.AlignmentMode alignment)
```

引擎名：`set_alignment`

### SetLastWrapAlignment

```csharp
public void SetLastWrapAlignment(FlowContainer.LastWrapAlignmentMode lastWrapAlignment)
```

引擎名：`set_last_wrap_alignment`

### SetReverseFill

```csharp
public void SetReverseFill(bool reverseFill)
```

引擎名：`set_reverse_fill`

### SetVertical

```csharp
public void SetVertical(bool vertical)
```

引擎名：`set_vertical`

## AlignmentMode

命名空间 `Godot`；enum

### 枚举值

- `Begin = 0`：The child controls will be arranged at the beginning of the container, i.e.
- `Center = 1`：The child controls will be centered in the container.
- `End = 2`：The child controls will be arranged at the end of the container, i.e.

## LastWrapAlignmentMode

命名空间 `Godot`；enum

### 枚举值

- `Inherit = 0`：The last partially filled row or column will wrap aligned to the previous row or column in accordance with [`FlowContainer.Alignment`](FlowContainer.md).
- `Begin = 1`：The last partially filled row or column will wrap aligned to the beginning of the previous row or column.
- `Center = 2`：The last partially filled row or column will wrap aligned to the center of the previous row or column.
- `End = 3`：The last partially filled row or column will wrap aligned to the end of the previous row or column.
