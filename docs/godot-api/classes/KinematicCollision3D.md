<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# KinematicCollision3D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Holds collision data from the movement of a [`PhysicsBody3D`](PhysicsBody3D.md), usually from [`PhysicsBody3D.MoveAndCollide`](PhysicsBody3D.md). When a [`PhysicsBody3D`](PhysicsBody3D.md) is moved, it stops if it detects a collision with another body. If a collision is detected, a `KinematicCollision3D` object is returned.

The collision data includes the colliding object, the remaining motion, and the collision position. This data can be used to determine a custom response to the collision.

## 构造函数

### KinematicCollision3D

```csharp
public KinematicCollision3D()
```

## 方法

### GetAngle

```csharp
public float GetAngle(int collisionIndex = 0, Vector3? upDirection = null)
```

引擎名：`get_angle`

Returns the collision angle according to `upDirection`, which is `Vector3.Up` by default. This value is always positive.

参数：
- `upDirection`：If the parameter is null, then the default value is `new Vector3(0.0f, 1.0f, 0.0f)`.

### GetCollider

```csharp
public GodotObject GetCollider(int collisionIndex = 0)
```

引擎名：`get_collider`

Returns the colliding body's attached [`GodotObject`](GodotObject.md) given a collision index (the deepest collision by default).

### GetColliderId

```csharp
public ulong GetColliderId(int collisionIndex = 0)
```

引擎名：`get_collider_id`

Returns the unique instance ID of the colliding body's attached [`GodotObject`](GodotObject.md) given a collision index (the deepest collision by default). See [`GodotObject.GetInstanceId`](GodotObject.md).

### GetColliderRid

```csharp
public Rid GetColliderRid(int collisionIndex = 0)
```

引擎名：`get_collider_rid`

Returns the colliding body's [`Rid`](Rid.md) used by the [`PhysicsServer3D`](PhysicsServer3D.md) given a collision index (the deepest collision by default).

### GetColliderShape

```csharp
public GodotObject GetColliderShape(int collisionIndex = 0)
```

引擎名：`get_collider_shape`

Returns the colliding body's shape given a collision index (the deepest collision by default).

### GetColliderShapeIndex

```csharp
public int GetColliderShapeIndex(int collisionIndex = 0)
```

引擎名：`get_collider_shape_index`

Returns the colliding body's shape index given a collision index (the deepest collision by default). See [`CollisionObject3D`](CollisionObject3D.md).

### GetColliderVelocity

```csharp
public Vector3 GetColliderVelocity(int collisionIndex = 0)
```

引擎名：`get_collider_velocity`

Returns the colliding body's velocity given a collision index (the deepest collision by default).

### GetCollisionCount

```csharp
public int GetCollisionCount()
```

引擎名：`get_collision_count`

Returns the number of detected collisions.

### GetDepth

```csharp
public float GetDepth()
```

引擎名：`get_depth`

Returns the colliding body's length of overlap along the collision normal.

### GetLocalShape

```csharp
public GodotObject GetLocalShape(int collisionIndex = 0)
```

引擎名：`get_local_shape`

Returns the moving object's colliding shape given a collision index (the deepest collision by default).

### GetNormal

```csharp
public Vector3 GetNormal(int collisionIndex = 0)
```

引擎名：`get_normal`

Returns the colliding body's shape's normal at the point of collision given a collision index (the deepest collision by default).

### GetPosition

```csharp
public Vector3 GetPosition(int collisionIndex = 0)
```

引擎名：`get_position`

Returns the point of collision in global coordinates given a collision index (the deepest collision by default).

### GetRemainder

```csharp
public Vector3 GetRemainder()
```

引擎名：`get_remainder`

Returns the moving object's remaining movement vector.

### GetTravel

```csharp
public Vector3 GetTravel()
```

引擎名：`get_travel`

Returns the moving object's travel before collision.
