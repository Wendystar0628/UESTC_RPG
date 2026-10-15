<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RayCast3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A raycast represents a ray from its origin to its `TargetPosition` that finds the closest object along its path, if it intersects any.

`RayCast3D` can ignore some objects by adding them to an exception list, by making its detection reporting ignore [`Area3D`](Area3D.md)s (`CollideWithAreas`) or [`PhysicsBody3D`](PhysicsBody3D.md)s (`CollideWithBodies`), or by configuring physics layers.

`RayCast3D` calculates intersection every physics frame, and it holds the result until the next physics frame. For an immediate raycast, or if you want to configure a `RayCast3D` multiple times within the same physics frame, use `ForceRaycastUpdate()`.

To sweep over a region of 3D space, you can approximate the region with multiple `RayCast3D`s or use [`ShapeCast3D`](ShapeCast3D.md).

## 构造函数

### RayCast3D

```csharp
public RayCast3D()
```

## 属性

### CollideWithAreas

```csharp
public bool CollideWithAreas { get; set; }
```

引擎名：`collide_with_areas`

If `true`, collisions with [`Area3D`](Area3D.md)s will be reported.

### CollideWithBodies

```csharp
public bool CollideWithBodies { get; set; }
```

引擎名：`collide_with_bodies`

If `true`, collisions with [`PhysicsBody3D`](PhysicsBody3D.md)s will be reported.

### CollisionMask

```csharp
public uint CollisionMask { get; set; }
```

引擎名：`collision_mask`

The ray's collision mask. Only objects in at least one collision layer enabled in the mask will be detected. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### DebugShapeCustomColor

```csharp
public Color DebugShapeCustomColor { get; set; }
```

引擎名：`debug_shape_custom_color`

The custom color to use to draw the shape in the editor and at run-time if **Visible Collision Shapes** is enabled in the **Debug** menu. This color will be highlighted at run-time if the `RayCast3D` is colliding with something.

If set to `Color(0.0, 0.0, 0.0)` (by default), the color set in `ProjectSettings.debug/shapes/collision/shape_color` is used.

### DebugShapeThickness

```csharp
public int DebugShapeThickness { get; set; }
```

引擎名：`debug_shape_thickness`

If set to `1`, a line is used as the debug shape. Otherwise, a truncated pyramid is drawn to represent the `RayCast3D`. Requires **Visible Collision Shapes** to be enabled in the **Debug** menu for the debug shape to be visible at run-time.

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

If `true`, this raycast will not report collisions with its parent node. This property only has an effect if the parent node is a [`CollisionObject3D`](CollisionObject3D.md). See also [`Node.GetParent`](Node.md) and `AddException()`.

### HitBackFaces

```csharp
public bool HitBackFaces { get; set; }
```

引擎名：`hit_back_faces`

If `true`, the ray will hit back faces with concave polygon shapes with back face enabled or heightmap shapes.

### HitFromInside

```csharp
public bool HitFromInside { get; set; }
```

引擎名：`hit_from_inside`

If `true`, the ray will detect a hit when starting inside shapes. In this case the collision normal will be `Vector3(0, 0, 0)`. Does not affect shapes with no volume like concave polygon or heightmap.

### TargetPosition

```csharp
public Vector3 TargetPosition { get; set; }
```

引擎名：`target_position`

The ray's destination point, relative to this raycast's [`Node3D.Position`](Node3D.md).

## 方法

### AddException

```csharp
public void AddException(CollisionObject3D node)
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

**Note:** This object is not guaranteed to be a [`CollisionObject3D`](CollisionObject3D.md). For example, if the ray intersects a [`CsgShape3D`](CsgShape3D.md) or a [`GridMap`](GridMap.md), the method will return a [`CsgShape3D`](CsgShape3D.md) or [`GridMap`](GridMap.md) instance.

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

To get the intersected shape node, for a [`CollisionObject3D`](CollisionObject3D.md) target, use:

```csharp
var target = (CollisionObject3D)GetCollider(); // A CollisionObject3D.
var shapeId = GetColliderShape(); // The shape index in the collider.
var ownerId = target.ShapeFindOwner(shapeId); // The owner ID in the collider.
var shape = target.ShapeOwnerGetOwner(ownerId);

```

### GetCollisionFaceIndex

```csharp
public int GetCollisionFaceIndex()
```

引擎名：`get_collision_face_index`

Returns the collision object's face index at the collision point, or `-1` if the shape intersecting the ray is not a [`ConcavePolygonShape3D`](ConcavePolygonShape3D.md).

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
public Vector3 GetCollisionNormal()
```

引擎名：`get_collision_normal`

Returns the normal of the intersecting object's shape at the collision point, or `Vector3(0, 0, 0)` if the ray starts inside the shape and `HitFromInside` is `true`.

**Note:** Check that `IsColliding()` returns `true` before calling this method to ensure the returned normal is valid and up-to-date.

### GetCollisionPoint

```csharp
public Vector3 GetCollisionPoint()
```

引擎名：`get_collision_point`

Returns the collision point at which the ray intersects the closest object, in the global coordinate system. If `HitFromInside` is `true` and the ray starts inside of a collision shape, this function will return the origin point of the ray.

**Note:** Check that `IsColliding()` returns `true` before calling this method to ensure the returned point is valid and up-to-date.

### GetDebugShapeCustomColor

```csharp
public Color GetDebugShapeCustomColor()
```

引擎名：`get_debug_shape_custom_color`

### GetDebugShapeThickness

```csharp
public int GetDebugShapeThickness()
```

引擎名：`get_debug_shape_thickness`

### GetExcludeParentBody

```csharp
public bool GetExcludeParentBody()
```

引擎名：`get_exclude_parent_body`

### GetTargetPosition

```csharp
public Vector3 GetTargetPosition()
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

### IsHitBackFacesEnabled

```csharp
public bool IsHitBackFacesEnabled()
```

引擎名：`is_hit_back_faces_enabled`

### IsHitFromInsideEnabled

```csharp
public bool IsHitFromInsideEnabled()
```

引擎名：`is_hit_from_inside_enabled`

### RemoveException

```csharp
public void RemoveException(CollisionObject3D node)
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

### SetDebugShapeCustomColor

```csharp
public void SetDebugShapeCustomColor(Color debugShapeCustomColor)
```

引擎名：`set_debug_shape_custom_color`

### SetDebugShapeThickness

```csharp
public void SetDebugShapeThickness(int debugShapeThickness)
```

引擎名：`set_debug_shape_thickness`

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

### SetHitBackFaces

```csharp
public void SetHitBackFaces(bool enable)
```

引擎名：`set_hit_back_faces`

### SetHitFromInside

```csharp
public void SetHitFromInside(bool enable)
```

引擎名：`set_hit_from_inside`

### SetTargetPosition

```csharp
public void SetTargetPosition(Vector3 localPoint)
```

引擎名：`set_target_position`
