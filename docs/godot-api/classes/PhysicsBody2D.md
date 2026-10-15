<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsBody2D

命名空间 `Godot`；class；继承 [`CollisionObject2D`](CollisionObject2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`PhysicsBody2D` is an abstract base class for 2D game objects affected by physics. All 2D physics bodies inherit from it.

## 方法

### AddCollisionExceptionWith

```csharp
public void AddCollisionExceptionWith(Node body)
```

引擎名：`add_collision_exception_with`

Adds a body to the list of bodies that this body can't collide with.

### GetCollisionExceptions

```csharp
public Array<PhysicsBody2D> GetCollisionExceptions()
```

引擎名：`get_collision_exceptions`

Returns an array of nodes that were added as collision exceptions for this body.

### GetGravity

```csharp
public Vector2 GetGravity()
```

引擎名：`get_gravity`

Returns the gravity vector computed from all sources that can affect the body, including all gravity overrides from [`Area2D`](Area2D.md) nodes and the global world gravity.

### MoveAndCollide

```csharp
public KinematicCollision2D MoveAndCollide(Vector2 motion, bool testOnly = false, float safeMargin = 0.08f, bool recoveryAsCollision = false)
```

引擎名：`move_and_collide`

Moves the body along the vector `motion`. In order to be frame rate independent in [`Node._PhysicsProcess()`](Node.md) or [`Node._Process()`](Node.md), `motion` should be computed using `delta`.

Returns a [`KinematicCollision2D`](KinematicCollision2D.md), which contains information about the collision when stopped, or when touching another body along the motion.

If `testOnly` is `true`, the body does not move but the would-be collision information is given.

`safeMargin` is the extra margin used for collision recovery (see [`CharacterBody2D.SafeMargin`](CharacterBody2D.md) for more details).

If `recoveryAsCollision` is `true`, any depenetration from the recovery phase is also reported as a collision; this is used e.g. by [`CharacterBody2D`](CharacterBody2D.md) for improving floor detection during floor snapping.

### RemoveCollisionExceptionWith

```csharp
public void RemoveCollisionExceptionWith(Node body)
```

引擎名：`remove_collision_exception_with`

Removes a body from the list of bodies that this body can't collide with.

### TestMove

```csharp
public bool TestMove(Transform2D from, Vector2 motion, KinematicCollision2D collision = null, float safeMargin = 0.08f, bool recoveryAsCollision = false)
```

引擎名：`test_move`

Checks for collisions without moving the body. In order to be frame rate independent in [`Node._PhysicsProcess()`](Node.md) or [`Node._Process()`](Node.md), `motion` should be computed using `delta`.

Virtually sets the node's position, scale and rotation to that of the given [`Transform2D`](Transform2D.md), then tries to move the body along the vector `motion`. Returns `true` if a collision would stop the body from moving along the whole path.

`collision` is an optional object of type [`KinematicCollision2D`](KinematicCollision2D.md), which contains additional information about the collision when stopped, or when touching another body along the motion.

`safeMargin` is the extra margin used for collision recovery (see [`CharacterBody2D.SafeMargin`](CharacterBody2D.md) for more details).

If `recoveryAsCollision` is `true`, any depenetration from the recovery phase is also reported as a collision; this is useful for checking whether the body would *touch* any other bodies.
