<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsBody3D

命名空间 `Godot`；class；继承 [`CollisionObject3D`](CollisionObject3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`PhysicsBody3D` is an abstract base class for 3D game objects affected by physics. All 3D physics bodies inherit from it.

**Warning:** With a non-uniform scale, this node will likely not behave as expected. It is advised to keep its scale the same on all axes and adjust its collision shape(s) instead.

## 属性

### AxisLockAngularX

```csharp
public bool AxisLockAngularX { get; set; }
```

引擎名：`axis_lock_angular_x`

Lock the body's rotation in the X axis.

### AxisLockAngularY

```csharp
public bool AxisLockAngularY { get; set; }
```

引擎名：`axis_lock_angular_y`

Lock the body's rotation in the Y axis.

### AxisLockAngularZ

```csharp
public bool AxisLockAngularZ { get; set; }
```

引擎名：`axis_lock_angular_z`

Lock the body's rotation in the Z axis.

### AxisLockLinearX

```csharp
public bool AxisLockLinearX { get; set; }
```

引擎名：`axis_lock_linear_x`

Lock the body's linear movement in the X axis.

### AxisLockLinearY

```csharp
public bool AxisLockLinearY { get; set; }
```

引擎名：`axis_lock_linear_y`

Lock the body's linear movement in the Y axis.

### AxisLockLinearZ

```csharp
public bool AxisLockLinearZ { get; set; }
```

引擎名：`axis_lock_linear_z`

Lock the body's linear movement in the Z axis.

## 方法

### AddCollisionExceptionWith

```csharp
public void AddCollisionExceptionWith(Node body)
```

引擎名：`add_collision_exception_with`

Adds a body to the list of bodies that this body can't collide with.

### GetAxisLock

```csharp
public bool GetAxisLock(PhysicsServer3D.BodyAxis axis)
```

引擎名：`get_axis_lock`

Returns `true` if the specified linear or rotational `axis` is locked.

### GetCollisionExceptions

```csharp
public Array<PhysicsBody3D> GetCollisionExceptions()
```

引擎名：`get_collision_exceptions`

Returns an array of nodes that were added as collision exceptions for this body.

### GetGravity

```csharp
public Vector3 GetGravity()
```

引擎名：`get_gravity`

Returns the gravity vector computed from all sources that can affect the body, including all gravity overrides from [`Area3D`](Area3D.md) nodes and the global world gravity.

### MoveAndCollide

```csharp
public KinematicCollision3D MoveAndCollide(Vector3 motion, bool testOnly = false, float safeMargin = 0.001f, bool recoveryAsCollision = false, int maxCollisions = 1)
```

引擎名：`move_and_collide`

Moves the body along the vector `motion`. In order to be frame rate independent in [`Node._PhysicsProcess()`](Node.md) or [`Node._Process()`](Node.md), `motion` should be computed using `delta`.

The body will stop if it collides. Returns a [`KinematicCollision3D`](KinematicCollision3D.md), which contains information about the collision when stopped, or when touching another body along the motion.

If `testOnly` is `true`, the body does not move but the would-be collision information is given.

`safeMargin` is the extra margin used for collision recovery (see [`CharacterBody3D.SafeMargin`](CharacterBody3D.md) for more details).

If `recoveryAsCollision` is `true`, any depenetration from the recovery phase is also reported as a collision; this is used e.g. by [`CharacterBody3D`](CharacterBody3D.md) for improving floor detection during floor snapping.

`maxCollisions` allows to retrieve more than one collision result.

### RemoveCollisionExceptionWith

```csharp
public void RemoveCollisionExceptionWith(Node body)
```

引擎名：`remove_collision_exception_with`

Removes a body from the list of bodies that this body can't collide with.

### SetAxisLock

```csharp
public void SetAxisLock(PhysicsServer3D.BodyAxis axis, bool lock)
```

引擎名：`set_axis_lock`

Locks or unlocks the specified linear or rotational `axis` depending on the value of `lock`.

### TestMove

```csharp
public bool TestMove(Transform3D from, Vector3 motion, KinematicCollision3D collision = null, float safeMargin = 0.001f, bool recoveryAsCollision = false, int maxCollisions = 1)
```

引擎名：`test_move`

Checks for collisions without moving the body. In order to be frame rate independent in [`Node._PhysicsProcess()`](Node.md) or [`Node._Process()`](Node.md), `motion` should be computed using `delta`.

Virtually sets the node's position, scale and rotation to that of the given [`Transform3D`](Transform3D.md), then tries to move the body along the vector `motion`. Returns `true` if a collision would stop the body from moving along the whole path.

`collision` is an optional object of type [`KinematicCollision3D`](KinematicCollision3D.md), which contains additional information about the collision when stopped, or when touching another body along the motion.

`safeMargin` is the extra margin used for collision recovery (see [`CharacterBody3D.SafeMargin`](CharacterBody3D.md) for more details).

If `recoveryAsCollision` is `true`, any depenetration from the recovery phase is also reported as a collision; this is useful for checking whether the body would *touch* any other bodies.

`maxCollisions` allows to retrieve more than one collision result.
