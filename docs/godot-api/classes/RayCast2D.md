<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RayCast2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A raycast represents a ray from its origin to its `TargetPosition` that finds the closest object along its path, if it intersects any.

`RayCast2D` can ignore some objects by adding them to an exception list, by making its detection reporting ignore [`Area2D`](Area2D.md)s (`CollideWithAreas`) or [`PhysicsBody2D`](PhysicsBody2D.md)s (`CollideWithBodies`), or by configuring physics layers.

`RayCast2D` calculates intersection every physics frame, and it holds the result until the next physics frame. For an immediate raycast, or if you want to configure a `RayCast2D` multiple times within the same physics frame, use `ForceRaycastUpdate()`.

To sweep over a region of 2D space, you can approximate the region with multiple `RayCast2D`s or use [`ShapeCast2D`](ShapeCast2D.md).

## 构造函数

### RayCast2D

```csharp
public RayCast2D()
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

The ray's collision mask. Only objects in at least one collision layer enabled in the mask will be detected. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

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

If `true`, this raycast will not report collisions with its parent node. This property only has an effect if the parent node is a [`CollisionObject2D`](CollisionObject2D.md). See also [`Node.GetParent`](Node.md) and `AddException()`.

### HitFromInside

```csharp
public bool HitFromInside { get; set; }
```

引擎名：`hit_from_inside`

If `true`, the ray will detect a hit when starting inside shapes. In this case the collision normal will be `Vector2(0, 0)`. Does not affect concave polygon shapes.

### TargetPosition

```csharp
public Vector2 TargetPosition { get; set; }
```

引擎名：`target_position`

The ray's destination point, relative to this raycast's [`Node2D.Position`](Node2D.md).

## 方法

### AddException

```csharp
public void AddException(CollisionObject2D node)
```

引擎名：`add_exception`

Adds a collision exception so the ray does not report collisions with the specified `node`.

### AddExceptionRid

```csharp
public void AddExceptionRid(Rid rid)
```

引擎名：`add_exception_rid`

Adds a collision exception so the ray does not report collisions with the specified [`Rid`](Rid.md).

### ClearExceptions

```csharp
public void ClearExceptions()
```

引擎名：`clear_exceptions`

Removes all collision exceptions for this ray.

### ForceRaycastUpdate

```csharp
public void ForceRaycastUpdate()
```

引擎名：`force_raycast_update`

Updates the collision information for the ray immediately, without waiting for the next `_physics_process` call. Use this method, for example, when the ray or its parent has changed state.

**Note:** `Enabled` does not need to be `true` for this to work.

### GetCollider

```csharp
public GodotObject GetCollider()
```

引擎名：`get_collider`

Returns the first object that the ray intersects, or `null` if no object is intersecting the ray (i.e. `IsColliding()` returns `false`).

**Note:** This object is not guaranteed to be a [`CollisionObject2D`](CollisionObject2D.md). For example, if the ray intersects a [`TileMapLayer`](TileMapLayer.md), the method will return a [`TileMapLayer`](TileMapLayer.md) instance.

### GetColliderRid

```csharp
public Rid GetColliderRid()
```

引擎名：`get_collider_rid`

Returns the [`Rid`](Rid.md) of the first object that the ray intersects, or an empty [`Rid`](Rid.md) if no object is intersecting the ray (i.e. `IsColliding()` returns `false`).

### GetColliderShape

```csharp
public int GetColliderShape()
```

引擎名：`get_collider_shape`

Returns the shape ID of the first object that the ray intersects, or `0` if no object is intersecting the ray (i.e. `IsColliding()` returns `false`).

To get the intersected shape node, for a [`CollisionObject2D`](CollisionObject2D.md) target, use:

```csharp
var target = (CollisionObject2D)GetCollider(); // A CollisionObject2D.
var shapeId = GetColliderShape(); // The shape index in the collider.
var ownerId = target.ShapeFindOwner(shapeId); // The owner ID in the collider.
var shape = target.ShapeOwnerGetOwner(ownerId);

```

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
public Vector2 GetCollisionNormal()
```

引擎名：`get_collision_normal`

Returns the normal of the intersecting object's shape at the collision point, or `Vector2(0, 0)` if the ray starts inside the shape and `HitFromInside` is `true`.

**Note:** Check that `IsColliding()` returns `true` before calling this method to ensure the returned normal is valid and up-to-date.

### GetCollisionPoint

```csharp
public Vector2 GetCollisionPoint()
```

引擎名：`get_collision_point`

Returns the collision point at which the ray intersects the closest object, in the global coordinate system. If `HitFromInside` is `true` and the ray starts inside of a collision shape, this function will return the origin point of the ray.

**Note:** Check that `IsColliding()` returns `true` before calling this method to ensure the returned point is valid and up-to-date.

### GetExcludeParentBody

```csharp
public bool GetExcludeParentBody()
```

引擎名：`get_exclude_parent_body`

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

Returns whether any object is intersecting with the ray's vector (considering the vector length).

### IsEnabled

```csharp
public bool IsEnabled()
```

引擎名：`is_enabled`

### IsHitFromInsideEnabled

```csharp
public bool IsHitFromInsideEnabled()
```

引擎名：`is_hit_from_inside_enabled`

### RemoveException

```csharp
public void RemoveException(CollisionObject2D node)
```

引擎名：`remove_exception`

Removes a collision exception so the ray can report collisions with the specified `node`.

### RemoveExceptionRid

```csharp
public void RemoveExceptionRid(Rid rid)
```

引擎名：`remove_exception_rid`

Removes a collision exception so the ray can report collisions with the specified [`Rid`](Rid.md).

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

### SetHitFromInside

```csharp
public void SetHitFromInside(bool enable)
```

引擎名：`set_hit_from_inside`

### SetTargetPosition

```csharp
public void SetTargetPosition(Vector2 localPoint)
```

引擎名：`set_target_position`
