<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CollisionPolygon2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A node that provides a polygon shape to a [`CollisionObject2D`](CollisionObject2D.md) parent and allows it to be edited. The polygon can be concave or convex. This can give a detection shape to an [`Area2D`](Area2D.md), turn a [`PhysicsBody2D`](PhysicsBody2D.md) into a solid object, or give a hollow shape to a [`StaticBody2D`](StaticBody2D.md).

**Warning:** A non-uniformly scaled `CollisionPolygon2D` will likely not behave as expected. Make sure to keep its scale the same on all axes and adjust its polygon instead.

## 构造函数

### CollisionPolygon2D

```csharp
public CollisionPolygon2D()
```

## 属性

### BuildMode

```csharp
public CollisionPolygon2D.BuildModeEnum BuildMode { get; set; }
```

引擎名：`build_mode`

Collision build mode.

### Disabled

```csharp
public bool Disabled { get; set; }
```

引擎名：`disabled`

If `true`, no collisions will be detected. This property should be changed with [`GodotObject.SetDeferred`](GodotObject.md).

### OneWayCollision

```csharp
public bool OneWayCollision { get; set; }
```

引擎名：`one_way_collision`

If `true`, only edges that face up, relative to `CollisionPolygon2D`'s rotation, will collide with other objects.

**Note:** This property has no effect if this `CollisionPolygon2D` is a child of an [`Area2D`](Area2D.md) node.

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

The margin used for one-way collision (in pixels). Higher values will make the shape thicker, and work better for colliders that enter the polygon at a high velocity.

### Polygon

```csharp
public Vector2[] Polygon { get; set; }
```

引擎名：`polygon`

The polygon's list of vertices. Each point will be connected to the next, and the final point will be connected to the first.

**Note:** The returned vertices are in the local coordinate space of the given `CollisionPolygon2D`.

## 方法

### GetBuildMode

```csharp
public CollisionPolygon2D.BuildModeEnum GetBuildMode()
```

引擎名：`get_build_mode`

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

### IsOneWayCollisionEnabled

```csharp
public bool IsOneWayCollisionEnabled()
```

引擎名：`is_one_way_collision_enabled`

### SetBuildMode

```csharp
public void SetBuildMode(CollisionPolygon2D.BuildModeEnum buildMode)
```

引擎名：`set_build_mode`

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

## BuildModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Solids = 0`：Collisions will include the polygon and its contained area.
- `Segments = 1`：Collisions will only include the polygon edges.
