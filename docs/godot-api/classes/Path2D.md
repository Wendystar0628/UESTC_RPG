<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Path2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Can have [`PathFollow2D`](PathFollow2D.md) child nodes moving along the [`Curve2D`](Curve2D.md). See [`PathFollow2D`](PathFollow2D.md) for more information on usage.

**Note:** The path is considered as relative to the moved nodes (children of [`PathFollow2D`](PathFollow2D.md)). As such, the curve should usually start with a zero vector (`(0, 0)`).

## 构造函数

### Path2D

```csharp
public Path2D()
```

## 属性

### Curve

```csharp
public Curve2D Curve { get; set; }
```

引擎名：`curve`

A [`Curve2D`](Curve2D.md) describing the path.

## 方法

### GetCurve

```csharp
public Curve2D GetCurve()
```

引擎名：`get_curve`

### SetCurve

```csharp
public void SetCurve(Curve2D curve)
```

引擎名：`set_curve`
