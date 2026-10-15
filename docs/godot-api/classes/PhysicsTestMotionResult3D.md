<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsTestMotionResult3D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Describes the motion and collision result from [`PhysicsServer3D.BodyTestMotion`](PhysicsServer3D.md).

## 构造函数

### PhysicsTestMotionResult3D

```csharp
public PhysicsTestMotionResult3D()
```

## 方法

### GetCollider

```csharp
public GodotObject GetCollider(int collisionIndex = 0)
```

引擎名：`get_collider`

Returns the colliding body's attached [`GodotObject`](GodotObject.md) given a collision index (the deepest collision by default), if a collision occurred.

### GetColliderId

```csharp
public ulong GetColliderId(int collisionIndex = 0)
```

引擎名：`get_collider_id`

Returns the unique instance ID of the colliding body's attached [`GodotObject`](GodotObject.md) given a collision index (the deepest collision by default), if a collision occurred. See [`GodotObject.GetInstanceId`](GodotObject.md).

### GetColliderRid

```csharp
public Rid GetColliderRid(int collisionIndex = 0)
```

引擎名：`get_collider_rid`

Returns the colliding body's [`Rid`](Rid.md) used by the [`PhysicsServer3D`](PhysicsServer3D.md) given a collision index (the deepest collision by default), if a collision occurred.

### GetColliderShape

```csharp
public int GetColliderShape(int collisionIndex = 0)
```

引擎名：`get_collider_shape`

Returns the colliding body's shape index given a collision index (the deepest collision by default), if a collision occurred. See [`CollisionObject3D`](CollisionObject3D.md).

### GetColliderVelocity

```csharp
public Vector3 GetColliderVelocity(int collisionIndex = 0)
```

引擎名：`get_collider_velocity`

Returns the colliding body's velocity given a collision index (the deepest collision by default), if a collision occurred.

### GetCollisionCount

```csharp
public int GetCollisionCount()
```

引擎名：`get_collision_count`

Returns the number of detected collisions.

### GetCollisionDepth

```csharp
public float GetCollisionDepth(int collisionIndex = 0)
```

引擎名：`get_collision_depth`

Returns the length of overlap along the collision normal given a collision index (the deepest collision by default), if a collision occurred.

### GetCollisionLocalShape

```csharp
public int GetCollisionLocalShape(int collisionIndex = 0)
```

引擎名：`get_collision_local_shape`

Returns the moving object's colliding shape given a collision index (the deepest collision by default), if a collision occurred.

### GetCollisionNormal

```csharp
public Vector3 GetCollisionNormal(int collisionIndex = 0)
```

引擎名：`get_collision_normal`

Returns the colliding body's shape's normal at the point of collision given a collision index (the deepest collision by default), if a collision occurred.

### GetCollisionPoint

```csharp
public Vector3 GetCollisionPoint(int collisionIndex = 0)
```

引擎名：`get_collision_point`

Returns the point of collision in global coordinates given a collision index (the deepest collision by default), if a collision occurred.

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
