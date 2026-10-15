<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CollisionShape3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A node that provides a [`Shape3D`](Shape3D.md) to a [`CollisionObject3D`](CollisionObject3D.md) parent and allows it to be edited. This can give a detection shape to an [`Area3D`](Area3D.md) or turn a [`PhysicsBody3D`](PhysicsBody3D.md) into a solid object.

**Warning:** A non-uniformly scaled `CollisionShape3D` will likely not behave as expected. Make sure to keep its scale the same on all axes and adjust its `Shape` resource instead.

## 构造函数

### CollisionShape3D

```csharp
public CollisionShape3D()
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

### Disabled

```csharp
public bool Disabled { get; set; }
```

引擎名：`disabled`

A disabled collision shape has no effect in the world. This property should be changed with [`GodotObject.SetDeferred`](GodotObject.md).

### Shape

```csharp
public Shape3D Shape { get; set; }
```

引擎名：`shape`

The actual shape owned by this collision shape.

## 方法

### GetDebugColor

```csharp
public Color GetDebugColor()
```

引擎名：`get_debug_color`

### GetEnableDebugFill

```csharp
public bool GetEnableDebugFill()
```

引擎名：`get_enable_debug_fill`

### GetShape

```csharp
public Shape3D GetShape()
```

引擎名：`get_shape`

### IsDisabled

```csharp
public bool IsDisabled()
```

引擎名：`is_disabled`

### MakeConvexFromSiblings

```csharp
public void MakeConvexFromSiblings()
```

引擎名：`make_convex_from_siblings`

Sets the collision shape's shape to the addition of all its convexed [`MeshInstance3D`](MeshInstance3D.md) siblings geometry.

### ResourceChanged

```csharp
public void ResourceChanged(Resource resource)
```

引擎名：`resource_changed`

This method does nothing.

### SetDebugColor

```csharp
public void SetDebugColor(Color color)
```

引擎名：`set_debug_color`

### SetDisabled

```csharp
public void SetDisabled(bool enable)
```

引擎名：`set_disabled`

### SetEnableDebugFill

```csharp
public void SetEnableDebugFill(bool enable)
```

引擎名：`set_enable_debug_fill`

### SetShape

```csharp
public void SetShape(Shape3D shape)
```

引擎名：`set_shape`
