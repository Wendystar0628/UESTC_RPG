<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# KinematicCollision2D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Holds collision data from the movement of a [`PhysicsBody2D`](PhysicsBody2D.md), usually from [`PhysicsBody2D.MoveAndCollide`](PhysicsBody2D.md). When a [`PhysicsBody2D`](PhysicsBody2D.md) is moved, it stops if it detects a collision with another body. If a collision is detected, a `KinematicCollision2D` object is returned.

The collision data includes the colliding object, the remaining motion, and the collision position. This data can be used to determine a custom response to the collision.

## 构造函数

### KinematicCollision2D

```csharp
public KinematicCollision2D()
```

## 方法

### GetAngle

```csharp
public float GetAngle(Vector2? upDirection = null)
```

引擎名：`get_angle`

Returns the collision angle according to `upDirection`, which is `Vector2.Up` by default. This value is always positive.

参数：
- `upDirection`：If the parameter is null, then the default value is `new Vector2(0.0f, -1.0f)`.

### GetCollider

```csharp
public GodotObject GetCollider()
```

引擎名：`get_collider`

Returns the colliding body's attached [`GodotObject`](GodotObject.md).

### GetColliderId

```csharp
public ulong GetColliderId()
```

引擎名：`get_collider_id`

Returns the unique instance ID of the colliding body's attached [`GodotObject`](GodotObject.md). See [`GodotObject.GetInstanceId`](GodotObject.md).

### GetColliderRid

```csharp
public Rid GetColliderRid()
```

引擎名：`get_collider_rid`

Returns the colliding body's [`Rid`](Rid.md) used by the [`PhysicsServer2D`](PhysicsServer2D.md).

### GetColliderShape

```csharp
public GodotObject GetColliderShape()
```

引擎名：`get_collider_shape`

Returns the colliding body's shape.

### GetColliderShapeIndex

```csharp
public int GetColliderShapeIndex()
```

引擎名：`get_collider_shape_index`

Returns the colliding body's shape index. See [`CollisionObject2D`](CollisionObject2D.md).

### GetColliderVelocity

```csharp
public Vector2 GetColliderVelocity()
```

引擎名：`get_collider_velocity`

Returns the colliding body's velocity.

### GetDepth

```csharp
public float GetDepth()
```

引擎名：`get_depth`

Returns the colliding body's length of overlap along the collision normal.

### GetLocalShape

```csharp
public GodotObject GetLocalShape()
```

引擎名：`get_local_shape`

Returns the moving object's colliding shape.

### GetNormal

```csharp
public Vector2 GetNormal()
```

引擎名：`get_normal`

Returns the colliding body's shape's normal at the point of collision.

### GetPosition

```csharp
public Vector2 GetPosition()
```

引擎名：`get_position`

Returns the point of collision in global coordinates.

### GetRemainder

```csharp
public Vector2 GetRemainder()
```

引擎名：`get_remainder`

Returns the moving object's remaining movement vector.

### GetTravel

```csharp
public Vector2 GetTravel()
```

引擎名：`get_travel`

Returns the moving object's travel before collision.
