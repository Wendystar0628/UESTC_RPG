<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ShapeCast2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Shape casting allows to detect collision objects by sweeping its `Shape` along the cast direction determined by `TargetPosition`. This is similar to [`RayCast2D`](RayCast2D.md), but it allows for sweeping a region of space, rather than just a straight line. `ShapeCast2D` can detect multiple collision objects. It is useful for things like wide laser beams or snapping a simple shape to a floor.

Immediate collision overlaps can be done with the `TargetPosition` set to `Vector2(0, 0)` and by calling `ForceShapecastUpdate()` within the same physics frame. This helps to overcome some limitations of [`Area2D`](Area2D.md) when used as an instantaneous detection area, as collision information isn't immediately available to it.

**Note:** Shape casting is more computationally expensive than ray casting.

## 构造函数

### ShapeCast2D

```csharp
public ShapeCast2D()
```

## 属性

### CollideWithAreas

```csharp
public bool CollideWithAreas { get; set; }
```

引擎名：`collide_with_areas`

If `true`, collisions with [`Area2D`](Area2D.md)s will be reported.

### CollideWithBodies

```csharp
public bool CollideWithBodies { get; set; }
```

引擎名：`collide_with_bodies`

If `true`, collisions with [`PhysicsBody2D`](PhysicsBody2D.md)s will be reported.

### CollisionMask

```csharp
public uint CollisionMask { get; set; }
```

引擎名：`collision_mask`

The shape's collision mask. Only objects in at least one collision layer enabled in the mask will be detected. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### CollisionResult

```csharp
public Array CollisionResult { get; }
```

引擎名：`collision_result`

Returns the complete collision information from the collision sweep. The data returned is the same as in the [`PhysicsDirectSpaceState2D.GetRestInfo`](PhysicsDirectSpaceState2D.md) method.

### Enabled

```csharp
public bool Enabled { get; set; }
```

引擎名：`enabled`

If `true`, collisions will be reported.

### ExcludeParent

```csharp
public bool ExcludeParent { get; set; }
```

引擎名：`exclude_parent`

If `true`, the parent node will be excluded from collision detection.

### Margin

```csharp
public float Margin { get; set; }
```

引擎名：`margin`

The collision margin for the shape. A larger margin helps detecting collisions more consistently, at the cost of precision.

### MaxResults

```csharp
public int MaxResults { get; set; }
```

引擎名：`max_results`

The number of intersections can be limited with this parameter, to reduce the processing time.

### Shape

```csharp
public Shape2D Shape { get; set; }
```

引擎名：`shape`

The shape to be used for collision queries.

### TargetPosition

```csharp
public Vector2 TargetPosition { get; set; }
```

引擎名：`target_position`

The shape's destination point, relative to this node's [`Node2D.Position`](Node2D.md).

## 方法

### AddException

```csharp
public void AddException(CollisionObject2D node)
```

引擎名：`add_exception`

Adds a collision exception so the shape does not report collisions with the specified node.

### AddExceptionRid

```csharp
public void AddExceptionRid(Rid rid)
```

引擎名：`add_exception_rid`

Adds a collision exception so the shape does not report collisions with the specified [`Rid`](Rid.md).

### ClearExceptions

```csharp
public void ClearExceptions()
```

引擎名：`clear_exceptions`

Removes all collision exceptions for this shape.

### ForceShapecastUpdate

```csharp
public void ForceShapecastUpdate()
```

引擎名：`force_shapecast_update`

Updates the collision information for the shape immediately, without waiting for the next `_physics_process` call. Use this method, for example, when the shape or its parent has changed state.

**Note:** Setting `Enabled` to `true` is not required for this to work.

### GetClosestCollisionSafeFraction

```csharp
public float GetClosestCollisionSafeFraction()
```

引擎名：`get_closest_collision_safe_fraction`

Returns the fraction from this cast's origin to its `TargetPosition` of how far the shape can move without triggering a collision, as a value between `0.0` and `1.0`.

### GetClosestCollisionUnsafeFraction

```csharp
public float GetClosestCollisionUnsafeFraction()
```

引擎名：`get_closest_collision_unsafe_fraction`

Returns the fraction from this cast's origin to its `TargetPosition` of how far the shape must move to trigger a collision, as a value between `0.0` and `1.0`.

In ideal conditions this would be the same as `GetClosestCollisionSafeFraction()`, however shape casting is calculated in discrete steps, so the precise point of collision can occur between two calculated positions.

### GetCollider

```csharp
public GodotObject GetCollider(int index)
```

引擎名：`get_collider`

Returns the collided [`GodotObject`](GodotObject.md) of one of the multiple collisions at `index`, or `null` if no object is intersecting the shape (i.e. `IsColliding()` returns `false`).

### GetColliderRid

```csharp
public Rid GetColliderRid(int index)
```

引擎名：`get_collider_rid`

Returns the [`Rid`](Rid.md) of the collided object of one of the multiple collisions at `index`.

### GetColliderShape

```csharp
public int GetColliderShape(int index)
```

引擎名：`get_collider_shape`

Returns the shape ID of the colliding shape of one of the multiple collisions at `index`, or `0` if no object is intersecting the shape (i.e. `IsColliding()` returns `false`).

### GetCollisionCount

```csharp
public int GetCollisionCount()
```

引擎名：`get_collision_count`

The number of collisions detected at the point of impact. Use this to iterate over multiple collisions as provided by `GetCollider()`, `GetColliderShape()`, `GetCollisionPoint()`, and `GetCollisionNormal()` methods.

### GetCollisionMask

```csharp
public uint GetCollisionMask()
```

引擎名：`get_collision_mask`

### GetCollisionMaskValue

```csharp
public bool GetCollisionMaskValue(int layerNumber)
```

引擎名：`get_collision_mask_value`

Returns whether or not the specified layer of the `CollisionMask` is enabled, given a `layerNumber` between 1 and 32.

### GetCollisionNormal

```csharp
public Vector2 GetCollisionNormal(int index)
```

引擎名：`get_collision_normal`

Returns the normal of one of the multiple collisions at `index` of the intersecting object.

### GetCollisionPoint

```csharp
public Vector2 GetCollisionPoint(int index)
```

引擎名：`get_collision_point`

Returns the collision point of one of the multiple collisions at `index` where the shape intersects the colliding object.

**Note:** This point is in the **global** coordinate system.

### GetCollisionResult

```csharp
public Array GetCollisionResult()
```

引擎名：`get_collision_result`

### GetExcludeParentBody

```csharp
public bool GetExcludeParentBody()
```

引擎名：`get_exclude_parent_body`

### GetMargin

```csharp
public float GetMargin()
```

引擎名：`get_margin`

### GetMaxResults

```csharp
public int GetMaxResults()
```

引擎名：`get_max_results`

### GetShape

```csharp
public Shape2D GetShape()
```

引擎名：`get_shape`

### GetTargetPosition

```csharp
public Vector2 GetTargetPosition()
```

引擎名：`get_target_position`

### IsCollideWithAreasEnabled

```csharp
public bool IsCollideWithAreasEnabled()
```

引擎名：`is_collide_with_areas_enabled`

### IsCollideWithBodiesEnabled

```csharp
public bool IsCollideWithBodiesEnabled()
```

引擎名：`is_collide_with_bodies_enabled`

### IsColliding

```csharp
public bool IsColliding()
```

引擎名：`is_colliding`

Returns whether any object is intersecting with the shape's vector (considering the vector length).

### IsEnabled

```csharp
public bool IsEnabled()
```

引擎名：`is_enabled`

### RemoveException

```csharp
public void RemoveException(CollisionObject2D node)
```

引擎名：`remove_exception`

Removes a collision exception so the shape does report collisions with the specified node.

### RemoveExceptionRid

```csharp
public void RemoveExceptionRid(Rid rid)
```

引擎名：`remove_exception_rid`

Removes a collision exception so the shape does report collisions with the specified [`Rid`](Rid.md).

### SetCollideWithAreas

```csharp
public void SetCollideWithAreas(bool enable)
```

引擎名：`set_collide_with_areas`

### SetCollideWithBodies

```csharp
public void SetCollideWithBodies(bool enable)
```

引擎名：`set_collide_with_bodies`

### SetCollisionMask

```csharp
public void SetCollisionMask(uint mask)
```

引擎名：`set_collision_mask`

### SetCollisionMaskValue

```csharp
public void SetCollisionMaskValue(int layerNumber, bool value)
```

引擎名：`set_collision_mask_value`

Based on `value`, enables or disables the specified layer in the `CollisionMask`, given a `layerNumber` between 1 and 32.

### SetEnabled

```csharp
public void SetEnabled(bool enabled)
```

引擎名：`set_enabled`

### SetExcludeParentBody

```csharp
public void SetExcludeParentBody(bool mask)
```

引擎名：`set_exclude_parent_body`

### SetMargin

```csharp
public void SetMargin(float margin)
```

引擎名：`set_margin`

### SetMaxResults

```csharp
public void SetMaxResults(int maxResults)
```

引擎名：`set_max_results`

### SetShape

```csharp
public void SetShape(Shape2D shape)
```

引擎名：`set_shape`

### SetTargetPosition

```csharp
public void SetTargetPosition(Vector2 localPoint)
```

引擎名：`set_target_position`
