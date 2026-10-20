<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CollisionPolygon3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A node that provides a thickened polygon shape (a prism) to a [`CollisionObject3D`](CollisionObject3D.md) parent and allows it to be edited. The polygon can be concave or convex. This can give a detection shape to an [`Area3D`](Area3D.md) or turn a [`PhysicsBody3D`](PhysicsBody3D.md) into a solid object.

**Warning:** A non-uniformly scaled [`CollisionShape3D`](CollisionShape3D.md) will likely not behave as expected. Make sure to keep its scale the same on all axes and adjust its shape resource instead.

## 构造函数

### CollisionPolygon3D

```csharp
public CollisionPolygon3D()
```

## 属性

### DebugColor

```csharp
public Color DebugColor { get; set; }
```

引擎名：`debug_color`

The collision shape color that is displayed in the editor, or in the running project if **Debug > Visible Collision Shapes** is checked at the top of the editor.

**Note:** The default value is `ProjectSettings.debug/shapes/collision/shape_color`. The `Color(0, 0, 0, 0)` value documented here is a placeholder, and not the actual default debug color.

### DebugFill

```csharp
public bool DebugFill { get; set; }
```

引擎名：`debug_fill`

If `true`, when the shape is displayed, it will show a solid fill color in addition to its wireframe.

### Depth

```csharp
public float Depth { get; set; }
```

引擎名：`depth`

Length that the resulting collision extends in either direction perpendicular to its 2D polygon.

### Disabled

```csharp
public bool Disabled { get; set; }
```

引擎名：`disabled`

If `true`, no collision will be produced. This property should be changed with [`GodotObject.SetDeferred`](GodotObject.md).

### Margin

```csharp
public float Margin { get; set; }
```

引擎名：`margin`

The collision margin for the generated [`Shape3D`](Shape3D.md). See [`Shape3D.Margin`](Shape3D.md) for more details.

### Polygon

```csharp
public Vector2[] Polygon { get; set; }
```

引擎名：`polygon`

Array of vertices which define the 2D polygon in the local XY plane.

## 方法

### GetDebugColor

```csharp
public Color GetDebugColor()
```

引擎名：`get_debug_color`

### GetDepth

```csharp
public float GetDepth()
```

引擎名：`get_depth`

### GetEnableDebugFill

```csharp
public bool GetEnableDebugFill()
```

引擎名：`get_enable_debug_fill`

### GetMargin

```csharp
public float GetMargin()
```

引擎名：`get_margin`

### GetPolygon

```csharp
public Vector2[] GetPolygon()
```

引擎名：`get_polygon`

### IsDisabled

```csharp
public bool IsDisabled()
```

引擎名：`is_disabled`

### SetDebugColor

```csharp
public void SetDebugColor(Color color)
```

引擎名：`set_debug_color`

### SetDepth

```csharp
public void SetDepth(float depth)
```

引擎名：`set_depth`

### SetDisabled

```csharp
public void SetDisabled(bool disabled)
```

引擎名：`set_disabled`

### SetEnableDebugFill

```csharp
public void SetEnableDebugFill(bool enable)
```

引擎名：`set_enable_debug_fill`

### SetMargin

```csharp
public void SetMargin(float margin)
```

引擎名：`set_margin`

### SetPolygon

```csharp
public void SetPolygon(ReadOnlySpan<Vector2> polygon)
```

引擎名：`set_polygon`

### SetPolygon

```csharp
public void SetPolygon(Vector2[] polygon)
```

引擎名：`set_polygon`
