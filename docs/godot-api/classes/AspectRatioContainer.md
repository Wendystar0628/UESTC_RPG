<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AspectRatioContainer

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A container type that arranges its child controls in a way that preserves their proportions automatically when the container is resized. Useful when a container has a dynamic size and the child nodes must adjust their sizes accordingly without losing their aspect ratios.

## 构造函数

### AspectRatioContainer

```csharp
public AspectRatioContainer()
```

## 属性

### AlignmentHorizontal

```csharp
public AspectRatioContainer.AlignmentMode AlignmentHorizontal { get; set; }
```

引擎名：`alignment_horizontal`

Specifies the horizontal relative position of child controls.

### AlignmentVertical

```csharp
public AspectRatioContainer.AlignmentMode AlignmentVertical { get; set; }
```

引擎名：`alignment_vertical`

Specifies the vertical relative position of child controls.

### Ratio

```csharp
public float Ratio { get; set; }
```

引擎名：`ratio`

The aspect ratio to enforce on child controls. This is the width divided by the height. The ratio depends on the `StretchMode`.

### StretchMode

```csharp
public AspectRatioContainer.StretchModeEnum StretchMode { get; set; }
```

引擎名：`stretch_mode`

The stretch mode used to align child controls.

## 方法

### GetAlignmentHorizontal

```csharp
public AspectRatioContainer.AlignmentMode GetAlignmentHorizontal()
```

引擎名：`get_alignment_horizontal`

### GetAlignmentVertical

```csharp
public AspectRatioContainer.AlignmentMode GetAlignmentVertical()
```

引擎名：`get_alignment_vertical`

### GetRatio

```csharp
public float GetRatio()
```

引擎名：`get_ratio`

### GetStretchMode

```csharp
public AspectRatioContainer.StretchModeEnum GetStretchMode()
```

引擎名：`get_stretch_mode`

### SetAlignmentHorizontal

```csharp
public void SetAlignmentHorizontal(AspectRatioContainer.AlignmentMode alignmentHorizontal)
```

引擎名：`set_alignment_horizontal`

### SetAlignmentVertical

```csharp
public void SetAlignmentVertical(AspectRatioContainer.AlignmentMode alignmentVertical)
```

引擎名：`set_alignment_vertical`

### SetRatio

```csharp
public void SetRatio(float ratio)
```

引擎名：`set_ratio`

### SetStretchMode

```csharp
public void SetStretchMode(AspectRatioContainer.StretchModeEnum stretchMode)
```

引擎名：`set_stretch_mode`

## AlignmentMode

命名空间 `Godot`；enum

### 枚举值

- `Begin = 0`：Aligns child controls with the beginning (left or top) of the container.
- `Center = 1`：Aligns child controls with the center of the container.
- `End = 2`：Aligns child controls with the end (right or bottom) of the container.

## StretchModeEnum

命名空间 `Godot`；enum

### 枚举值

- `WidthControlsHeight = 0`：The height of child controls is automatically adjusted based on the width of the container.
- `HeightControlsWidth = 1`：The width of child controls is automatically adjusted based on the height of the container.
- `Fit = 2`：The bounding rectangle of child controls is automatically adjusted to fit inside the container while keeping the aspect ratio.
- `Cover = 3`：The width and height of child controls is automatically adjusted to make their bounding rectangle cover the entire area of the container while keeping the aspect ratio.
