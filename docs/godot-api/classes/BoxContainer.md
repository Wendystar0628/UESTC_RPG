<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# BoxContainer

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A container that arranges its child controls horizontally or vertically, rearranging them automatically when their minimum size changes.

## 构造函数

### BoxContainer

```csharp
public BoxContainer()
```

## 属性

### Alignment

```csharp
public BoxContainer.AlignmentMode Alignment { get; set; }
```

引擎名：`alignment`

The alignment of the container's children (must be one of [`BoxContainer.AlignmentMode.Begin`](BoxContainer.md), [`BoxContainer.AlignmentMode.Center`](BoxContainer.md), or [`BoxContainer.AlignmentMode.End`](BoxContainer.md)).

### Vertical

```csharp
public bool Vertical { get; set; }
```

引擎名：`vertical`

If `true`, the `BoxContainer` will arrange its children vertically, rather than horizontally.

Can't be changed when using [`HBoxContainer`](HBoxContainer.md) and [`VBoxContainer`](VBoxContainer.md).

## 方法

### AddSpacer

```csharp
public Control AddSpacer(bool begin)
```

引擎名：`add_spacer`

Adds a [`Control`](Control.md) node to the box as a spacer. If `begin` is `true`, it will insert the [`Control`](Control.md) node in front of all other children.

### GetAlignment

```csharp
public BoxContainer.AlignmentMode GetAlignment()
```

引擎名：`get_alignment`

### IsVertical

```csharp
public bool IsVertical()
```

引擎名：`is_vertical`

### SetAlignment

```csharp
public void SetAlignment(BoxContainer.AlignmentMode alignment)
```

引擎名：`set_alignment`

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
