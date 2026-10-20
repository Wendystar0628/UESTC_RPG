<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CollisionShape2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A node that provides a [`Shape2D`](Shape2D.md) to a [`CollisionObject2D`](CollisionObject2D.md) parent and allows it to be edited. This can give a detection shape to an [`Area2D`](Area2D.md) or turn a [`PhysicsBody2D`](PhysicsBody2D.md) into a solid object.

## 构造函数

### CollisionShape2D

```csharp
public CollisionShape2D()
```

## 属性

### DebugColor

```csharp
public Color DebugColor { get; set; }
```

引擎名：`debug_color`

The collision shape color that is displayed in the editor, or in the running project if **Debug > Visible Collision Shapes** is checked at the top of the editor.

**Note:** The default value is `ProjectSettings.debug/shapes/collision/shape_color`. The `Color(0, 0, 0, 0)` value documented here is a placeholder, and not the actual default debug color.

### Disabled

```csharp
public bool Disabled { get; set; }
```

引擎名：`disabled`

A disabled collision shape has no effect in the world. This property should be changed with [`GodotObject.SetDeferred`](GodotObject.md).

### OneWayCollision

```csharp
public bool OneWayCollision { get; set; }
```

引擎名：`one_way_collision`

Sets whether this collision shape should only detect collision on one side (top or bottom).

**Note:** This property has no effect if this `CollisionShape2D` is a child of an [`Area2D`](Area2D.md) node.

**Note:** The one way collision direction can be configured by setting `OneWayCollisionDirection`.

### OneWayCollisionDirection

```csharp
public Vector2 OneWayCollisionDirection { get; set; }
```

引擎名：`one_way_collision_direction`

The direction used for one-way collision.

### OneWayCollisionMargin

```csharp
public float OneWayCollisionMargin { get; set; }
```

引擎名：`one_way_collision_margin`

The margin used for one-way collision (in pixels). Higher values will make the shape thicker, and work better for colliders that enter the shape at a high velocity.

### Shape

```csharp
public Shape2D Shape { get; set; }
```

引擎名：`shape`

The actual shape owned by this collision shape.

## 方法

### GetDebugColor

```csharp
public Color GetDebugColor()
```

引擎名：`get_debug_color`

### GetOneWayCollisionDirection

```csharp
public Vector2 GetOneWayCollisionDirection()
```

引擎名：`get_one_way_collision_direction`

### GetOneWayCollisionMargin

```csharp
public float GetOneWayCollisionMargin()
```

引擎名：`get_one_way_collision_margin`

### GetShape

```csharp
public Shape2D GetShape()
```

引擎名：`get_shape`

### IsDisabled

```csharp
public bool IsDisabled()
```

引擎名：`is_disabled`

### IsOneWayCollisionEnabled

```csharp
public bool IsOneWayCollisionEnabled()
```

引擎名：`is_one_way_collision_enabled`

### SetDebugColor

```csharp
public void SetDebugColor(Color color)
```

引擎名：`set_debug_color`

### SetDisabled

```csharp
public void SetDisabled(bool disabled)
```

引擎名：`set_disabled`

### SetOneWayCollision

```csharp
public void SetOneWayCollision(bool enabled)
```

引擎名：`set_one_way_collision`

### SetOneWayCollisionDirection

```csharp
public void SetOneWayCollisionDirection(Vector2 direction)
```

引擎名：`set_one_way_collision_direction`

### SetOneWayCollisionMargin

```csharp
public void SetOneWayCollisionMargin(float margin)
```

引擎名：`set_one_way_collision_margin`

### SetShape

```csharp
public void SetShape(Shape2D shape)
```

引擎名：`set_shape`
