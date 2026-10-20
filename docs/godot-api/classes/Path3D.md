<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Path3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Can have [`PathFollow3D`](PathFollow3D.md) child nodes moving along the [`Curve3D`](Curve3D.md). See [`PathFollow3D`](PathFollow3D.md) for more information on the usage.

Note that the path is considered as relative to the moved nodes (children of [`PathFollow3D`](PathFollow3D.md)). As such, the curve should usually start with a zero vector `(0, 0, 0)`.

## 构造函数

### Path3D

```csharp
public Path3D()
```

## 属性

### Curve

```csharp
public Curve3D Curve { get; set; }
```

引擎名：`curve`

A [`Curve3D`](Curve3D.md) describing the path.

### DebugCustomColor

```csharp
public Color DebugCustomColor { get; set; }
```

引擎名：`debug_custom_color`

The custom color used to draw the path in the editor. If set to `Color.BLACK` (as by default), the color set in `ProjectSettings.debug/shapes/paths/geometry_color` is used.

## 方法

### GetCurve

```csharp
public Curve3D GetCurve()
```

引擎名：`get_curve`

### GetDebugCustomColor

```csharp
public Color GetDebugCustomColor()
```

引擎名：`get_debug_custom_color`

### SetCurve

```csharp
public void SetCurve(Curve3D curve)
```

引擎名：`set_curve`

### SetDebugCustomColor

```csharp
public void SetDebugCustomColor(Color debugCustomColor)
```

引擎名：`set_debug_custom_color`

## 事件（信号）

### CurveChanged

```csharp
public event Action CurveChanged
```

引擎名：`curve_changed`

Emitted when the `Curve` changes.

### DebugColorChanged

```csharp
public event Action DebugColorChanged
```

引擎名：`debug_color_changed`

Emitted when the `DebugCustomColor` changes.
