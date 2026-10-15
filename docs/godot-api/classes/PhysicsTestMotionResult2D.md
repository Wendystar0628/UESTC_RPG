<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsTestMotionResult2D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Describes the motion and collision result from [`PhysicsServer2D.BodyTestMotion`](PhysicsServer2D.md).

## 构造函数

### PhysicsTestMotionResult2D

```csharp
public PhysicsTestMotionResult2D()
```

## 方法

### GetCollider

```csharp
public GodotObject GetCollider()
```

引擎名：`get_collider`

Returns the colliding body's attached [`GodotObject`](GodotObject.md), if a collision occurred.

### GetColliderId

```csharp
public ulong GetColliderId()
```

引擎名：`get_collider_id`

Returns the unique instance ID of the colliding body's attached [`GodotObject`](GodotObject.md), if a collision occurred. See [`GodotObject.GetInstanceId`](GodotObject.md).

### GetColliderRid

```csharp
public Rid GetColliderRid()
```

引擎名：`get_collider_rid`

Returns the colliding body's [`Rid`](Rid.md) used by the [`PhysicsServer2D`](PhysicsServer2D.md), if a collision occurred.

### GetColliderShape

```csharp
public int GetColliderShape()
```

引擎名：`get_collider_shape`

Returns the colliding body's shape index, if a collision occurred. See [`CollisionObject2D`](CollisionObject2D.md).

### GetColliderVelocity

```csharp
public Vector2 GetColliderVelocity()
```

引擎名：`get_collider_velocity`

Returns the colliding body's velocity, if a collision occurred.

### GetCollisionDepth

```csharp
public float GetCollisionDepth()
```

引擎名：`get_collision_depth`

Returns the length of overlap along the collision normal, if a collision occurred.

### GetCollisionLocalShape

```csharp
public int GetCollisionLocalShape()
```

引擎名：`get_collision_local_shape`

Returns the moving object's colliding shape, if a collision occurred.

### GetCollisionNormal

```csharp
public Vector2 GetCollisionNormal()
```

引擎名：`get_collision_normal`

Returns the colliding body's shape's normal at the point of collision, if a collision occurred.

### GetCollisionPoint

```csharp
public Vector2 GetCollisionPoint()
```

引擎名：`get_collision_point`

Returns the point of collision in global coordinates, if a collision occurred.

### GetCollisionSafeFraction

```csharp
public float GetCollisionSafeFraction()
```

引擎名：`get_collision_safe_fraction`

Returns the maximum fraction of the motion that can occur without a collision, between `0` and `1`.

### GetCollisionUnsafeFraction

```csharp
public float GetCollisionUnsafeFraction()
```

引擎名：`get_collision_unsafe_fraction`

Returns the minimum fraction of the motion needed to collide, if a collision occurred, between `0` and `1`.

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
