<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CanvasModulate

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`CanvasModulate` applies a color tint to all nodes on a canvas. Only one can be used to tint a canvas, but [`CanvasLayer`](CanvasLayer.md)s can be used to render things independently.

## 构造函数

### CanvasModulate

```csharp
public CanvasModulate()
```

## 属性

### Color

```csharp
public Color Color { get; set; }
```

引擎名：`color`

The tint color to apply.

## 方法

### GetColor

```csharp
public Color GetColor()
```

引擎名：`get_color`

### SetColor

```csharp
public void SetColor(Color color)
```

引擎名：`set_color`
